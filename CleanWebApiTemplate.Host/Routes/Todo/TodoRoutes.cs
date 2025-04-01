using CleanWebApiTemplate.Application.Handlers.Todo.Delete;
using CleanWebApiTemplate.Application.Handlers.Todo.GetById;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Host.Common;
using CleanWebApiTemplate.Host.Helpers;
using CleanWebApiTemplate.Host.Routes.Todo.Create;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Host.Routes.Todo.Get;
using CleanWebApiTemplate.Host.Routes.Todo.Update;
using System.Net;

namespace CleanWebApiTemplate.Host.Routes.Todo;

public class TodoRoutes(IHttpContextAccessor httpContextAccessor) : BaseApiRouter(httpContextAccessor), IGroupMap
{
    public void MapGroup(IEndpointRouteBuilder app)
    {
        var userGroup = RouteGroupFactory.CreateAuthorizedGroup(app, RouteName, [Constants.USER_POLICY]);
        var operatorGroup = RouteGroupFactory.CreateAuthorizedGroup(app, RouteName, [Constants.OPERATOR_POLICY]);

        userGroup.MapGet("/{id}", async (string id, CancellationToken cancellationToken) =>
        {
            GetTodoByIdQuery query = new() { Id = id };
            var result = await Mediator.Send(query, cancellationToken);
            return result.ToResult();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK).Produces<ResultModel>((int)HttpStatusCode.NoContent).Produces<ResultModel>((int)HttpStatusCode.BadRequest);

        userGroup.MapGet("/", async ([AsParameters] FilteredTodoRequest request, CancellationToken cancellationToken) =>
        {
            var query = TodoMappers.FromRequestToQuery(request);
            var result = await Mediator.Send(query, cancellationToken);
            return result.ToResult();
        }).Produces<IEnumerable<TodoResponse>>((int)HttpStatusCode.OK);

        userGroup.MapGet("/titles", async ([AsParameters] GetTodoTitlesRequest request, CancellationToken cancellationToken) =>
        {
            var query = TodoMappers.FromRequestToQuery(request);
            var result = await Mediator.Send(query, cancellationToken);
            return result.ToResult();
        }).Produces<IEnumerable<TodoTitleResponse>>((int)HttpStatusCode.OK);

        userGroup.MapPost("/", async (CreateTodoRequest request, CancellationToken cancellationToken) =>
        {
            var command = TodoMappers.FromRequestToCommand(request, UserEmail);
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResult();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK);

        userGroup.MapPut("/{id}", async (string id, UpdateTodoRequest request, CancellationToken cancellationToken) =>
        {
            var command = TodoMappers.FromRequestToUpdateCommand(request, id, UserEmail);
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResult();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK);

        operatorGroup.MapDelete("/{id}", async (string id, CancellationToken cancellationToken) =>
        {
            var command = new DeleteTodoCommand() { Id = id };
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResult();
        }).Produces<bool>((int)HttpStatusCode.OK);
    }
}

