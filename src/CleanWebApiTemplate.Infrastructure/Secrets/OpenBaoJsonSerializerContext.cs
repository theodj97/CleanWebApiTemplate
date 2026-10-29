using System.Text.Json;
using System.Text.Json.Serialization;

namespace CleanWebApiTemplate.Infrastructure.Secrets;

/// <summary>
/// Source-generated JSON metadata for the OpenBao HTTP API. Required for Native AOT: the client never falls back to
/// the reflection-based serializer. Dictionary keys (the secret names) are preserved exactly as stored.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OpenBaoReadResponse))]
[JsonSerializable(typeof(OpenBaoWriteRequest))]
[JsonSerializable(typeof(OpenBaoErrorResponse))]
internal partial class OpenBaoJsonSerializerContext : JsonSerializerContext;

internal sealed class OpenBaoReadResponse
{
    public OpenBaoKvData? Data { get; set; }
}

internal sealed class OpenBaoKvData
{
    public Dictionary<string, string>? Data { get; set; }
}

internal sealed class OpenBaoWriteRequest
{
    public Dictionary<string, string> Data { get; set; } = [];
}

internal sealed class OpenBaoErrorResponse
{
    public List<string>? Errors { get; set; }
}
