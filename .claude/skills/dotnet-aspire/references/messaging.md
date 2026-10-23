# Messaging: publish, consume, verify

Template: `assets/tests/shared/Messaging/KafkaTestHelper.cs`. Brokers are declared once in the shared AppHost and are real
containers in every mode (Local, Component, Functional). Other brokers follow the same rules with the snippets below.
Brokers always run as real containers or emulators (external-services.md); never replace them with in-memory
transports in component or functional tests.

## Contents
- [Rules for every broker](#rules-for-every-broker)
- [Kafka](#kafka)
- [RabbitMQ](#rabbitmq)
- [Azure Service Bus (emulator)](#azure-service-bus-emulator)
- [Amazon SQS/SNS](#amazon-sqssns)
- [Messaging libraries](#messaging-libraries)
- [When the application consumes](#when-the-application-consumes)
- [Outbox and eventual publication](#outbox-and-eventual-publication)

## Rules for every broker

- **Own consumer identity.** Verification consumers use a unique group id / subscription / queue, so they never
  steal messages from the app's consumers and never resume where a previous run stopped.
- **Only messages produced after the Act.** Either record positions before the Act (Kafka offsets, captured by the
  fixture at the start of each test — take a new snapshot right before the Act when the Arrange section itself makes
  the app publish) or bind a fresh queue before the Act (RabbitMQ, Service Bus), and filter by key/correlation id.
  Never "read the topic and take the last message": a previous test's message, or an id restarted by a reset, will match.
- **Always a timeout**, returning null/default so the test asserts (`Assert.NotNull(published)`) and fails with a clear
  message instead of hanging.
- **Topology**: if the app does not create its topics/queues/exchanges on startup, the fixture creates them before the
  app starts, with the same names, partition counts and bindings as production.
- **Wire format**: deserialise exactly what the app produces. Find the serializer in discovery (System.Text.Json
  options, Newtonsoft, Avro/Protobuf with a schema registry, CloudEvents, library envelopes). When in doubt, run the
  scenario once, dump the raw payload and headers, and write the deserialisation from what you see.

## Kafka

AppHost: `builder.AddKafka("kafka")` (Aspire.Hosting.Kafka). Connection string = bootstrap servers.

`KafkaTestHelper` (exposed as `Fixture.Kafka`):

| Member | Use |
|---|---|
| `CreateTopicsAsync(topics, partitions)` | fixture startup, before the app starts, with `AppHostContract.Topics.CreatedByFixture`; idempotent |
| `CaptureOffsetsAsync()` | called by `PrepareForTestAsync` before every test: end offset of every partition of every topic; call it again in the test right before the Act if Arrange produced messages |
| `WaitForMessageAsync<T>(topic, predicate, timeout?)` | Assert: first message after the snapshot matching the predicate, null on timeout |
| `PublishAsync(topic, message, key, headers)` | Act, when the app is the consumer |
| `WaitForConsumerGroupAsync(groupId)` | fixture readiness: the app's consumer group is Stable with partitions, so the first test's message is not skipped (`auto.offset.reset=latest`) |
| `WaitUntilConsumedAsync(groupId)` | `PrepareForTestAsync` for consuming apps: the previous test's messages are processed before this test resets the database |

```csharp
// Assert
var published = await Fixture.Kafka.WaitForMessageAsync<OrderCreated>("orders", m => m.OrderId == order.Id);
Assert.NotNull(published);
Assert.Equal(100m, published.Amount);
```

Avro/Protobuf with Schema Registry: add a schema registry container to the AppHost
(`AddContainer("schema-registry", "confluentinc/cp-schema-registry", tag)` with the Kafka bootstrap servers, `WaitFor(kafka)`),
point the app to it, and change the helper's consumer/producer to use `Confluent.SchemaRegistry.Serdes` deserializers.

## RabbitMQ

AppHost: `builder.AddRabbitMQ("rabbitmq")` (Aspire.Hosting.RabbitMQ). Connection string = AMQP URI.

RabbitMQ does not retain messages for queues that do not exist yet, so the verification queue must be bound **before
the Act** (in Arrange) — a message published before the binding is lost to the test.

```csharp
// RabbitMQ.Client 7.x (async API)
public sealed class RabbitMqProbe : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly Channel<byte[]> _received = Channel.CreateUnbounded<byte[]>(); // System.Threading.Channels

    private RabbitMqProbe(IConnection connection, IChannel channel) => (_connection, _channel) = (connection, channel);

    public static async Task<RabbitMqProbe> BindAsync(string connectionString, string exchange, string routingKey = "#")
    {
        var connection = await new ConnectionFactory { Uri = new Uri(connectionString) }.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();
        var queue = await channel.QueueDeclareAsync(queue: "", durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync(queue.QueueName, exchange, routingKey);
        var probe = new RabbitMqProbe(connection, channel);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) => await probe._received.Writer.WriteAsync(delivery.Body.ToArray());
        await channel.BasicConsumeAsync(queue.QueueName, autoAck: true, consumer);
        return probe;
    }
    // WaitForMessageAsync<T>(predicate, timeout): read _received until a payload deserialises and matches, or time out.

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
```

Expose the broker's connection string from the fixture (e.g. a `RabbitMqConnectionString` property filled from
`App.GetConnectionStringAsync("rabbitmq")` at startup) and bind in Arrange:

```csharp
// Arrange
await using var orderEvents = await RabbitMqProbe.BindAsync(Fixture.RabbitMqConnectionString, exchange: "orders");
// Act
var response = await Client.PostAsJsonAsync("/orders", request, CancellationToken);
// Assert
var published = await orderEvents.WaitForMessageAsync<OrderCreated>(m => m.CustomerId == customer.Id);
Assert.NotNull(published);
```

Purge the app's own queues in `PrepareForTestAsync` (`QueuePurgeAsync`) when a failed test may have left messages.

## Azure Service Bus (emulator)

AppHost: `builder.AddAzureServiceBus("servicebus").RunAsEmulator()` and declare the entities in the AppHost
(`AddServiceBusQueue`, `AddServiceBusTopic(...).AddServiceBusSubscription(...)`): the emulator takes its entities from
configuration and does not support creating them at runtime through the administration client.

- Verify published messages through a **dedicated test subscription** on the topic (declared in the AppHost), never
  through the app's own subscription, which would steal the app's messages.
- Queues read by another service: receive from the queue in Assert with a `ServiceBusReceiver` and a timeout.
- Drain the test subscription in `PrepareForTestAsync` (receive until empty with a short wait), since there is no
  offset snapshot.
- Check the emulator's limits (number of entities, message size) against the app's topology.

## Amazon SQS/SNS

LocalStack (or an alternative, see external-services.md). Create queues/topics in the fixture; for SNS verification,
subscribe a fresh SQS queue to the topic before the Act and receive from it with long polling and a timeout.

## Messaging libraries

When the app uses MassTransit, Wolverine, CAP, NServiceBus or Rebus, still verify on the real broker, but learn the
library's conventions from the app's configuration and one real message:

- **MassTransit**: on RabbitMQ an exchange per message type (`Namespace:TypeName` by default), so bind the probe to that
  exchange; on Kafka (Rider) the topic configured in the rider. Messages are JSON envelopes
  (`{"messageId":…, "messageType":["urn:message:…"], "message":{…}}`) unless raw JSON is configured: deserialise the
  `message` property. Publishing to a MassTransit consumer needs the envelope and `Content-Type:
  application/vnd.masstransit+json` (or raw JSON if the consumer endpoint accepts it).
- **Wolverine / CAP / NServiceBus / Rebus**: read the transport routing (exchange/topic names, headers such as message
  type and correlation id) and replicate it in the probe and in `PublishAsync`.
- Library test harnesses that replace the transport with an in-memory one are not a substitute in component or
  functional tests: they bypass serialization, routing and broker configuration — exactly what these tests cover.

## When the application consumes

- Act publishes the message (with the same key, headers and format as the real producer); Assert waits for the
  effect: database state through a test repository, an outgoing message, a WireMock call — always with
  `Eventually.AssertAsync` or a `WaitFor...` helper, never with a fixed delay.
- Before the first test, wait until the app's consumer is ready (`WaitForConsumerGroupAsync` for Kafka; for RabbitMQ
  and Service Bus, the app's queue has a consumer).
- Before resetting the database, make sure the app has processed the previous test's messages
  (`WaitUntilConsumedAsync`), otherwise a late message writes into the next test's fresh data.
- Idempotency/deduplication tables (inbox, processed-message ids) are data: they are reset like any other table.

## Outbox and eventual publication

With a transactional outbox the HTTP response returns before the message reaches the broker: the helper's wait and
timeout already cover it. If the outbox publisher is a background service that interferes with other tests, keep it
running in tests that verify publication; disable it (component only) where it is irrelevant and only if it causes
flakiness, never to make a failing test pass.
