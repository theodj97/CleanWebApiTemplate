using System.Text.Json;
using System.Text.Json.Serialization;
using CleanWebApiTemplate.Api.Todo.Create;
using CleanWebApiTemplate.Api.Todo.Filter;
using CleanWebApiTemplate.Api.Todo.Get;
using CleanWebApiTemplate.Api.Todo.Update;

namespace CleanWebApiTemplate.Api.Todo;

/// <summary>
/// Source-generated JSON metadata for the Todo contracts. Required for Native AOT, where the reflection-based
/// serializer is unavailable: every type the feature reads or writes is listed here.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(CreateTodoRequest))]
[JsonSerializable(typeof(UpdateTodoRequest))]
[JsonSerializable(typeof(FilteredTodoRequest))]
[JsonSerializable(typeof(GetTodoTitlesRequest))]
[JsonSerializable(typeof(TodoResponse))]
[JsonSerializable(typeof(TodoTitleResponse))]
[JsonSerializable(typeof(IEnumerable<TodoResponse>))]
[JsonSerializable(typeof(IEnumerable<TodoTitleResponse>))]
internal sealed partial class TodoJsonSerializerContext : JsonSerializerContext;
