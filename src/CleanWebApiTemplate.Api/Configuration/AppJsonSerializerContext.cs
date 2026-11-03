using System.Text.Json;
using System.Text.Json.Serialization;
using CleanWebApiTemplate.Api.Models.Responses.Todo;
using CleanWebApiTemplate.Api.Routes.Todo.Create;
using CleanWebApiTemplate.Api.Routes.Todo.Filter;
using CleanWebApiTemplate.Api.Routes.Todo.Get;
using CleanWebApiTemplate.Api.Routes.Todo.Update;
using Microsoft.AspNetCore.Mvc;

namespace CleanWebApiTemplate.Api.Configuration;

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
