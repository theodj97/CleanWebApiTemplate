using CleanWebApiTemplate.Application.Handlers.Todo.Delete;
using CleanWebApiTemplate.Application.Handlers.Todo.GetById;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Host.Common;
using CleanWebApiTemplate.Host.Helpers;
using CleanWebApiTemplate.Host.ResponseModels.Todo;
using CleanWebApiTemplate.Host.Routes.Todo.Create;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Host.Routes.Todo.Get;
using CleanWebApiTemplate.Host.Routes.Todo.Update;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CleanWebApiTemplate.Host.Routes.Todo;

public class TodoRoutes(IHttpContextAccessor httpContextAccessor) : BaseApiRouter(httpContextAccessor), IGroupMap
{
    public void MapGroup(IEndpointRouteBuilder app)
    {
        var userGroup = app.CreateAuthorizedGroup(RouteName, [Constants.USER_POLICY]);
        var operatorGroup = app.CreateAuthorizedGroup(RouteName, [Constants.OPERATOR_POLICY]);

        userGroup.MapGet("/{id}", async (string id, CancellationToken cancellationToken) =>
        {
            GetTodoByIdQuery query = new() { Id = id };
            Result<TodoDto?> result = await Mediator.Send(query, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
        .Produces<ProblemDetails>((int)HttpStatusCode.NoContent)
        .Produces<ProblemDetails>((int)HttpStatusCode.MethodNotAllowed);

        userGroup.MapPost("/filter", async ([FromBody] FilteredTodoRequest request, CancellationToken cancellationToken) =>
        {
            var query = request.ToQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        }).Produces<IEnumerable<TodoResponse>>((int)HttpStatusCode.OK)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
        .Produces<ProblemDetails>((int)HttpStatusCode.NoContent);

        userGroup.MapPost("/titles", async ([FromBody] GetTodoTitlesRequest request, CancellationToken cancellationToken) =>
        {
            var query = request.ToQuery();
            var result = await Mediator.Send(query, cancellationToken);
            return result.ToResponse<TodoDto?, TodoTitleResponse>();
        }).Produces<IEnumerable<TodoTitleResponse>>((int)HttpStatusCode.OK)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
        .Produces<ProblemDetails>((int)HttpStatusCode.NoContent);

        userGroup.MapPost("/", async (CreateTodoRequest request, CancellationToken cancellationToken) =>
        {
            var command = request.ToCommand(UserEmail);
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        }).Produces<TodoResponse>((int)HttpStatusCode.Created)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest);

        userGroup.MapPut("/{id}", async (string id, UpdateTodoRequest request, CancellationToken cancellationToken) =>
        {
            var command = request.ToCommand(id, UserEmail);
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest)
        .Produces<ProblemDetails>((int)HttpStatusCode.NotFound);

        operatorGroup.MapDelete("/{id}", async (string id, CancellationToken cancellationToken) =>
        {
            var command = new DeleteTodoCommand() { Id = id };
            var result = await Mediator.Send(command, cancellationToken);
            return result.ToResponse();
        }).Produces<bool>((int)HttpStatusCode.OK)
        .Produces<ProblemDetails>((int)HttpStatusCode.BadRequest);
    }
}

