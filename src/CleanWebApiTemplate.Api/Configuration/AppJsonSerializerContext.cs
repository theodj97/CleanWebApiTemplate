using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>
/// Source-generated JSON metadata shared by every feature: the ProblemDetails body. Each feature contributes its
/// own <see cref="JsonSerializerContext"/>, chained in front of this one.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(ProblemDetails))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;
