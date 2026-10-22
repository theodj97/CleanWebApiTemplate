using CleanWebApiTemplate.Application.Todo.Create;
using CleanWebApiTemplate.Application.Todo.Delete;
using CleanWebApiTemplate.Application.Todo.Filtered;
using CleanWebApiTemplate.Application.Todo.GetById;
using CleanWebApiTemplate.Application.Todo.GetTitles;
using CleanWebApiTemplate.Application.Todo.Update;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Validators
        services.AddTransient<IValidator<CreateTodoInput>, CreateTodoInputValidator>();
        services.AddTransient<IValidator<UpdateTodoInput>, UpdateTodoInputValidator>();
        services.AddTransient<IValidator<DeleteTodoInput>, DeleteTodoInputValidator>();
        services.AddTransient<IValidator<GetTodoByIdInput>, GetTodoByIdInputValidator>();
        services.AddTransient<IValidator<GetTodoTitlesInput>, GetTodoTitlesInputValidator>();
        services.AddTransient<IValidator<FilteredTodoInput>, FilteredTodoInputValidator>();

        // Services
        services.AddTransient<ICreateTodoService, CreateTodoService>();
        services.AddTransient<IUpdateTodoService, UpdateTodoService>();
        services.AddTransient<IDeleteTodoService, DeleteTodoService>();
        services.AddTransient<IGetTodoByIdService, GetTodoByIdService>();
        services.AddTransient<IGetTodoTitlesService, GetTodoTitlesService>();
        services.AddTransient<IFilteredTodoService, FilteredTodoService>();

        return services;
    }
}
