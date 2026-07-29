using CleanWebApiTemplate.Host.Models.Responses.Todo;
using CleanWebApiTemplate.Host.Routes.Todo.Create;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Host.Routes.Todo.Get;
using CleanWebApiTemplate.Host.Routes.Todo.Update;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CleanWebApiTemplate.Host.Configuration;

/// <summary>
/// Source-generated JSON serializer metadata for the API contracts.
/// Required for Native AOT, where the reflection-based serializer is unavailable.
/// Add each request/response type here.
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
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(ProblemDetails))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
