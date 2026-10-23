using System.Net;
using CleanWebApiTemplate.Api.Common;
using CleanWebApiTemplate.Api.Common.Abstractions;
using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Api.Models.Responses.Todo;
using CleanWebApiTemplate.Api.Routes.Todo.Create;
using CleanWebApiTemplate.Api.Routes.Todo.Filter;
using CleanWebApiTemplate.Api.Routes.Todo.Get;
using CleanWebApiTemplate.Api.Routes.Todo.Update;
using CleanWebApiTemplate.Application.Todo.Create;
using CleanWebApiTemplate.Application.Todo.Delete;
using CleanWebApiTemplate.Application.Todo.Filtered;
using CleanWebApiTemplate.Application.Todo.GetById;
using CleanWebApiTemplate.Application.Todo.GetTitles;
using CleanWebApiTemplate.Application.Todo.Update;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Api.Routes.Todo;

public class TodoRoutes(IHttpContextAccessor httpContextAccessor) : BaseApiRouter(httpContextAccessor)
{
    public override void MapGroup(IEndpointRouteBuilder app)
    {
        var userGroup = app.CreateAuthorizedRouteGroupBuilder(RouteName, [Constants.USER_POLICY]);
        var operatorGroup = app.CreateAuthorizedRouteGroupBuilder(RouteName, [Constants.OPERATOR_POLICY]);

        userGroup.MapGet("/{id}", async (string id,
                                         IGetTodoByIdService service,
                                         CancellationToken cancellationToken) =>
        {
            GetTodoByIdInput input = new() { Id = id };
            Result<TodoDto?> result = await service.GetAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK);

        userGroup.MapPost("/filter", async (FilteredTodoRequest request,
                                            IFilteredTodoService service,
                                            CancellationToken cancellationToken) =>
        {
            var input = request.ToInput();
            var result = await service.FilterAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        }).Produces<IEnumerable<TodoResponse>>((int)HttpStatusCode.OK);

        userGroup.MapPost("/titles", async (GetTodoTitlesRequest request,
                                            IGetTodoTitlesService service,
                                            CancellationToken cancellationToken) =>
        {
            var input = request.ToInput();
            var result = await service.GetAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoTitleResponse>();
        }).Produces<IEnumerable<TodoTitleResponse>>((int)HttpStatusCode.OK);

        userGroup.MapPost("/", async (CreateTodoRequest request,
                                      ICreateTodoService service,
                                      CancellationToken cancellationToken) =>
        {
            var input = request.ToInput(UserEmail);
            var result = await service.CreateAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        }).Produces<TodoResponse>((int)HttpStatusCode.Created)
        .Produces<TodoResponse>((int)HttpStatusCode.Conflict);

        userGroup.MapPut("/{id}", async (string id,
                                         UpdateTodoRequest request,
                                         IUpdateTodoService service,
                                         CancellationToken cancellationToken) =>
        {
            var input = request.ToInput(id, UserEmail);
            var result = await service.UpdateAsync(input, cancellationToken);
            return result.ToResponse<TodoDto?, TodoResponse>();
        }).Produces<TodoResponse>((int)HttpStatusCode.OK);

        operatorGroup.MapDelete("/{id}", async (string id,
                                                IDeleteTodoService service,
                                                CancellationToken cancellationToken) =>
        {
            var input = new DeleteTodoInput() { Id = id };
            var result = await service.DeleteAsync(input, cancellationToken);
            return result.ToResponse();
        }).Produces<bool>((int)HttpStatusCode.OK);
    }
}
