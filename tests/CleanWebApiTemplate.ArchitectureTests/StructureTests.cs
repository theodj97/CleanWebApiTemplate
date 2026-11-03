using CleanWebApiTemplate.Application.Abstractions.Handlers;
using CleanWebApiTemplate.Application.Abstractions.Identity;
using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Handlers.Queries;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Repositories.Todo;

namespace CleanWebApiTemplate.ArchitectureTests;

/// <summary>Location and naming conventions that keep the application boundaries honest.</summary>
public class StructureTests
{
    [Fact]
    public void ApplicationPortsAndQueryContract_ShouldLiveUnderExpectedNamespaces()
    {
        Assert.Equal("CleanWebApiTemplate.Application.Abstractions.Repositories", typeof(ITodoRepository).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Abstractions.Identity", typeof(ICurrentUser).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Abstractions.Handlers", typeof(IHandler<,>).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Todo.Handlers.Queries", typeof(TodoFilter).Namespace);
    }

    [Fact]
    public void ApplicationHandlers_ShouldBeSealedAndLiveUnderTheirFeature()
    {
        var handlerTypes = typeof(IHandler<,>).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces()
                               .Any(@interface => @interface.IsGenericType
                                                  && @interface.GetGenericTypeDefinition() == typeof(IHandler<,>)))
            .ToArray();

        Assert.NotEmpty(handlerTypes);
        Assert.All(handlerTypes, type =>
        {
            Assert.True(type.IsSealed, $"{type.FullName} must be sealed.");
            Assert.StartsWith("CleanWebApiTemplate.Application.Todo.Handlers.",
                              type.Namespace!,
                              StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Result_ShouldLiveInApplicationResults()
    {
        Assert.Equal("CleanWebApiTemplate.Application.Results", typeof(Result<>).Namespace);
        Assert.DoesNotContain(typeof(TodoEntity).Assembly.GetTypes(),
                              type => type.Namespace?.StartsWith("CleanWebApiTemplate.Domain.ResultModel",
                                                                 StringComparison.Ordinal) == true);
    }

    [Fact]
    public void InfrastructureRepositories_ShouldContainOnlyRepositoriesAndTheirHelpers()
    {
        var types = typeof(TodoRepository).Assembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("CleanWebApiTemplate.Infrastructure.Repositories",
                                                      StringComparison.Ordinal) == true)
            .Where(type => type.IsClass && !type.IsNested)
            .ToArray();

        Assert.NotEmpty(types);
        Assert.All(types, type => Assert.True(
            type.Name.EndsWith("Repository", StringComparison.Ordinal)
            || type.Namespace!.StartsWith("CleanWebApiTemplate.Infrastructure.Repositories.Todo.Helpers",
                                          StringComparison.Ordinal),
            $"{type.FullName} is not a repository or a repository helper."));
    }
}
