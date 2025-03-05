using CleanWebApiTemplate.Application.Handlers.Todo.GetById;
using CleanWebApiTemplate.Host.Common;
using CleanWebApiTemplate.Host.Helpers;
using CleanWebApiTemplate.Host.Routes.Todo.Create;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Host.Routes.Todo.Update;
using Microsoft.AspNetCore.Mvc;

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
        });

        group.MapGet("/", async ([AsParameters] FilteredTodoRequest request, CancellationToken cancellationToken) =>
        {
            var query = TodoMappers.FromRequestToQuery(request);
            var result = await Mediator.Send(query, cancellationToken);
            return result.ToResult();
        });

        group.MapPost("/", async ([FromBody] CreateTodoRequest request, CancellationToken cancellationToken) =>
        {
            var command = TodoMappers.FromRequestToCommand(request, UserEmail);
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResult();
        });

        group.MapPut("/{id}", async (string id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken) =>
        {
            var command = TodoMappers.FromRequestToUpdateCommand(request, id, UserEmail);
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResult();
        });
    }
}
