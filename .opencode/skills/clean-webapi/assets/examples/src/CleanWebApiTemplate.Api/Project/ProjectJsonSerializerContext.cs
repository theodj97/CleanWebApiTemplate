using System.Text.Json;
using System.Text.Json.Serialization;
using CleanWebApiTemplate.Api.Project.Create;
using CleanWebApiTemplate.Api.Project.Filter;
using CleanWebApiTemplate.Api.Project.Get;
using CleanWebApiTemplate.Api.Project.Update;

namespace CleanWebApiTemplate.Api.Project;

/// <summary>
/// Source-generated JSON metadata for the Project contracts. Required for Native AOT, where the reflection-based
/// serializer is unavailable: every type the feature reads or writes is listed here.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(CreateProjectRequest))]
[JsonSerializable(typeof(UpdateProjectRequest))]
[JsonSerializable(typeof(FilteredProjectRequest))]
[JsonSerializable(typeof(GetProjectNamesRequest))]
[JsonSerializable(typeof(ProjectResponse))]
[JsonSerializable(typeof(ProjectNameResponse))]
[JsonSerializable(typeof(IEnumerable<ProjectResponse>))]
[JsonSerializable(typeof(IEnumerable<ProjectNameResponse>))]
internal sealed partial class ProjectJsonSerializerContext : JsonSerializerContext;
