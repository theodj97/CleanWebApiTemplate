using CleanWebApiTemplate.Application.Extensions;
using CleanWebApiTemplate.Application.Handlers.Todo.Create;
using CleanWebApiTemplate.Application.Handlers.Todo.Delete;
using CleanWebApiTemplate.Application.Handlers.Todo.Filtered;
using CleanWebApiTemplate.Application.Handlers.Todo.GetById;
using CleanWebApiTemplate.Application.Handlers.Todo.GetTitles;
using CleanWebApiTemplate.Application.Handlers.Todo.Update;
using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Validators
        services.AddTransient<IValidator<CreateTodoCommand>, CreateTodoCommandValidator>();
        services.AddTransient<IValidator<UpdateTodoCommand>, UpdateTodoCommandValidator>();
        services.AddTransient<IValidator<DeleteTodoCommand>, DeleteTodoCommandValidator>();
        services.AddTransient<IValidator<GetTodoByIdQuery>, GetTodoByIdQueryValidator>();
        services.AddTransient<IValidator<GetTodoTitleQuery>, GetTodoTitleQueryValidator>();
        services.AddTransient<IValidator<FilteredTodoQuery>, FilteredTodoQueryValidator>();

        // Commands
        services.AddCommandHandler<CreateTodoCommand, Result<TodoDto?>, CreateTodoCommandHandler>();
        services.AddCommandHandler<UpdateTodoCommand, Result<TodoDto?>, UpdateTodoCommandHandler>();
        services.AddCommandHandler<DeleteTodoCommand, Result<bool>, DeleteTodoCommandHandler>();

        // Queries
        services.AddQueryHandler<GetTodoByIdQuery, Result<TodoDto?>, GetTodoByIdQueryHandler>();
        services.AddQueryHandler<GetTodoTitleQuery, Result<IEnumerable<TodoDto?>>, GetTodoTitleQueryHandler>();
        services.AddQueryHandler<FilteredTodoQuery, Result<IEnumerable<TodoDto?>>, FilteredTodoQueryHandler>();

        return services;
    }
}
