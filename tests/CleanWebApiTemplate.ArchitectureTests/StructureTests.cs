using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Abstractions;
using CleanWebApiTemplate.Application.Todo.Queries;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Repositories.Todo;

namespace CleanWebApiTemplate.ArchitectureTests;

/// <summary>Location and naming conventions that keep the repository pattern honest.</summary>
public class StructureTests
{
    [Fact]
    public void RepositoryPortAndQueryContract_ShouldLiveInApplication()
    {
        Assert.Equal("CleanWebApiTemplate.Application.Todo.Abstractions", typeof(ITodoRepository).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Todo.Queries", typeof(TodoFilter).Namespace);
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
