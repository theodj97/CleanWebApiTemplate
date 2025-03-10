using CleanWebApiTemplate.Application.Handlers.Todo.Delete;
using CleanWebApiTemplate.Application.Handlers.Todo.GetById;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Host.Common;
using CleanWebApiTemplate.Host.Helpers;
using CleanWebApiTemplate.Host.Routes.Todo.Create;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Host.Routes.Todo.Update;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CleanWebApiTemplate.Host.Routes.Todo;

public class TodoRoutes(IHttpContextAccessor httpContextAccessor) : BaseApiRouter(httpContextAccessor), IGroupMap
{
    public void MapGroup(IEndpointRouteBuilder app)
    {
        var group = RouteGroupFactory.CreateAuthorizedGroup(app, RouteName);

        group.MapGet("/{id}", async (string id, CancellationToken cancellationToken) =>
        {
            GetTodoByIdQuery query = new() { Id = id };
            var result = await Mediator.Send(query, cancellationToken);
            return result.ToResult();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK);

        group.MapGet("/", async ([AsParameters] FilteredTodoRequest request, CancellationToken cancellationToken) =>
        {
            var query = TodoMappers.FromRequestToQuery(request);
            var result = await Mediator.Send(query, cancellationToken);
            return result.ToResult();
        }).Produces<IEnumerable<TodoResponse>>((int)HttpStatusCode.OK);

        group.MapPost("/", async ([FromBody] CreateTodoRequest request, CancellationToken cancellationToken) =>
        {
            var command = TodoMappers.FromRequestToCommand(request, UserEmail);
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResult();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK);

        group.MapPut("/{id}", async (string id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken) =>
        {
            var command = TodoMappers.FromRequestToUpdateCommand(request, id, UserEmail);
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResult();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK);

        group.MapDelete("/{id}", async (string id, CancellationToken cancellationToken) =>
        {
            var command = new DeleteTodoCommand() { Id = id };
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResult();
        }).Produces<bool>((int)HttpStatusCode.OK);
    }
}
