using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using {{SharedNamespace}}.Fixtures;
using {{SharedNamespace}}.Waiting;

namespace {{SharedNamespace}}.Messaging;

/// <summary>
/// Publishes and verifies Kafka messages against the real broker started by Aspire.
/// Isolation rules:
/// <list type="bullet">
/// <item>Every verification consumer uses a unique group id, so it never steals messages from the app's groups.</item>
/// <item><see cref="CaptureOffsetsAsync"/> runs before every test (TestBase → fixture): verification only sees
/// messages produced after that point, never an older test's message with a matching key or a restarted id.</item>
/// <item>Every wait has a timeout.</item>
/// </list>
/// ADAPT the (de)serialisation to the app's wire format: JSON options, envelope (MassTransit, CloudEvents), Avro or
/// Protobuf with Schema Registry (references/messaging.md).
/// </summary>
public sealed class KafkaTestHelper : IAsyncDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan MetadataTimeout = TimeSpan.FromSeconds(10);

    private readonly string _bootstrapServers;
    private readonly IAdminClient _admin;
    private readonly IConsumer<string?, string> _offsetReader;
    private readonly IProducer<string?, string> _producer;
    private Dictionary<TopicPartition, Offset> _offsetsAtTestStart = [];

    public KafkaTestHelper(string bootstrapServers)
    {
        _bootstrapServers = bootstrapServers;
        _admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
        _offsetReader = new ConsumerBuilder<string?, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = $"tests-offsets-{Guid.NewGuid():N}",
            EnableAutoCommit = false,
        }).Build();
        _producer = new ProducerBuilder<string?, string>(new ProducerConfig { BootstrapServers = bootstrapServers }).Build();
    }

    /// <summary>ADAPT: same naming policy/converters as the app's serializer.</summary>
    public JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web);

    /// <summary>Creates topics the app does not create itself. Idempotent.</summary>
    public async Task CreateTopicsAsync(IEnumerable<string> topics, int partitions = 1)
    {
        try
        {
            await _admin.CreateTopicsAsync(topics.Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = partitions,
                ReplicationFactor = 1,
            }));
        }
        catch (CreateTopicsException exception)
            when (exception.Results.All(r => r.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
        {
            // Already there from a previous step: nothing to do.
        }
    }

    /// <summary>Records the end offset of every partition of every topic.</summary>
    public Task CaptureOffsetsAsync() => Task.Run(() =>
    {
        var metadata = _admin.GetMetadata(MetadataTimeout);

        _offsetsAtTestStart = metadata.Topics
            .Where(topic => topic.Error.Code == ErrorCode.NoError && !topic.Topic.StartsWith("__", StringComparison.Ordinal))
            .SelectMany(topic => topic.Partitions.Select(partition => new TopicPartition(topic.Topic, partition.PartitionId)))
            .ToDictionary(partition => partition, partition => _offsetReader.QueryWatermarkOffsets(partition, MetadataTimeout).High);
    });

    /// <summary>
    /// First message on <paramref name="topic"/> produced during this test that matches <paramref name="predicate"/>,
    /// or null after the timeout (assert on the result in the test).
    /// </summary>
    public Task<T?> WaitForMessageAsync<T>(string topic, Func<T, bool> predicate, TimeSpan? timeout = null)
        where T : class
    {
        var cancellationToken = TestCancellation.Token;

        return Task.Run(() =>
        {
            var partitions = _admin.GetMetadata(topic, MetadataTimeout).Topics.Single().Partitions;
            if (partitions.Count == 0)
            {
                throw new InvalidOperationException($"Topic '{topic}' does not exist. Create it in the fixture if the app does not.");
            }

            using var consumer = new ConsumerBuilder<string?, string>(new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = $"tests-{Guid.NewGuid():N}",
                EnableAutoCommit = false,
            }).Build();

            // Topics created during the test did not exist at the snapshot: read them from the beginning.
            consumer.Assign(partitions.Select(partition =>
            {
                var topicPartition = new TopicPartition(topic, partition.PartitionId);
                var start = _offsetsAtTestStart.TryGetValue(topicPartition, out var offset) ? offset : Offset.Beginning;
                return new TopicPartitionOffset(topicPartition, start);
            }));

            var deadline = DateTime.UtcNow + (timeout ?? DefaultTimeout);
            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(250));
                if (result?.Message?.Value is not { } payload)
                {
                    continue;
                }

                if (TryDeserialize<T>(payload) is { } message && predicate(message))
                {
                    return message;
                }
            }

            return null;
        }, cancellationToken);
    }

    /// <summary>
    /// Publishes a message the way the real producers do, for tests where the application is the consumer.
    /// ADAPT: key and headers (message type, correlation id, content type) that the app's consumer relies on.
    /// </summary>
    public async Task PublishAsync<T>(string topic, T message, string? key = null, IReadOnlyDictionary<string, string>? headers = null)
    {
        var kafkaMessage = new Message<string?, string>
        {
            Key = key,
            Value = JsonSerializer.Serialize(message, SerializerOptions),
            Headers = new Headers(),
        };

        foreach (var (name, value) in headers ?? new Dictionary<string, string>())
        {
            kafkaMessage.Headers.Add(name, Encoding.UTF8.GetBytes(value));
        }

        await _producer.ProduceAsync(topic, kafkaMessage, TestCancellation.Token);
    }

    /// <summary>
    /// Waits until an application consumer group is Stable and owns partitions. Call it once before the first test:
    /// a message published before the app's consumer has its partitions can be skipped (auto.offset.reset=latest).
    /// </summary>
    public Task WaitForConsumerGroupAsync(string groupId, TimeSpan? timeout = null) =>
        Eventually.AssertAsync(async () =>
        {
            var description = (await _admin.DescribeConsumerGroupsAsync([groupId])).ConsumerGroupDescriptions.Single();
            if (description.State != ConsumerGroupState.Stable
                || description.Members.All(member => member.Assignment.TopicPartitions.Count == 0))
            {
                throw new InvalidOperationException($"Consumer group '{groupId}' is {description.State} without assignments.");
            }
        }, timeout ?? TimeSpan.FromSeconds(60));

    /// <summary>
    /// Waits until an application consumer group has committed everything published so far. Use it before resetting
    /// the database when the app consumes messages, so a slow message from the previous test cannot write into this
    /// test's data. Committed equals processed only when the app commits after processing (manual commit); with
    /// auto-commit it means "fetched" and the wait can add up to the commit interval (5 s by default) per test.
    /// </summary>
    public Task WaitUntilConsumedAsync(string groupId, TimeSpan? timeout = null) =>
        Eventually.AssertAsync(async () =>
        {
            var committed = await _admin.ListConsumerGroupOffsetsAsync([new ConsumerGroupTopicPartitions(groupId, null)]);
            foreach (var partition in committed.Single().Partitions)
            {
                var end = _offsetReader.QueryWatermarkOffsets(partition.TopicPartition, MetadataTimeout).High.Value;
                var position = partition.Offset == Offset.Unset ? 0 : partition.Offset.Value;
                if (position < end)
                {
                    throw new InvalidOperationException($"'{groupId}' has unprocessed messages on {partition.TopicPartition}.");
                }
            }
        }, timeout ?? TimeSpan.FromSeconds(30));

    private T? TryDeserialize<T>(string payload)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        }
        catch (JsonException)
        {
            return null; // another message type on the same topic
        }
    }

    public ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        _offsetReader.Dispose();
        _admin.Dispose();
        return ValueTask.CompletedTask;
    }
}
