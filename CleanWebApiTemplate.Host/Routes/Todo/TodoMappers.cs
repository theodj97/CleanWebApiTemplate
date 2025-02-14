using CleanWebApiTemplate.Application.Handlers.Todo.Create;
using CleanWebApiTemplate.Application.Handlers.Todo.Filtered;
using CleanWebApiTemplate.Host.Routes.Todo.Create;
using CleanWebApiTemplate.Host.Routes.Todo.Filter;

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
}
