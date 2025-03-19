using CleanWebApiTemplate.Application.Handlers.Todo.Create;
using CleanWebApiTemplate.Application.Handlers.Todo.Filtered;
using CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;
using CleanWebApiTemplate.Application.Handlers.Todo.Update;
using CleanWebApiTemplate.Host.Routes.Todo.Create;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Host.Routes.Todo.Get;
using CleanWebApiTemplate.Host.Routes.Todo.Update;

namespace CleanWebApiTemplate.Host.Routes.Todo;

public static class TodoMappers
{
    public static FilteredTodoQuery FromRequestToQuery(FilteredTodoRequest request)
    {
        return new FilteredTodoQuery()
        {
            Status = request.Status,
            Title = request.Title,
            CreatedBy = request.CreatedBy
        };
    }

    public static GetTodoTitleQuery FromRequestToQuery(GetTodoTitlesRequest request)
    {
        return new GetTodoTitleQuery()
        {
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public static CreateTodoCommand FromRequestToCommand(CreateTodoRequest request,
                                                         string userNameRequester)
    {
        return new CreateTodoCommand()
        {
            Title = request.Title,
            Description = request.Description,
            CreatedBy = userNameRequester
        };
    }

    public static UpdateTodoCommand FromRequestToUpdateCommand(UpdateTodoRequest request,
                                                               string id,
                                                               string userNameRequester)
    {
        return new UpdateTodoCommand()
        {
            Id = id,
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            UpdatedBy = userNameRequester
        };
    }
}
