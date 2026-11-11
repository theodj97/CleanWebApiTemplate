using CleanWebApiTemplate.Api;
using CleanWebApiTemplate.Api.Common.Abstractions;
using CleanWebApiTemplate.Api.Todo;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Domain.Common;
using CleanWebApiTemplate.Domain.Todo;
using CleanWebApiTemplate.Infrastructure.Todo;

namespace CleanWebApiTemplate.ArchitectureTests;

/// <summary>
/// Location and naming conventions that keep the application boundaries honest. The rules are written
/// against the folder convention, not against one feature, so adding a feature never edits them.
/// </summary>
public class StructureTests
{
    private const string ApplicationPrefix = "CleanWebApiTemplate.Application.";
    private const string ApplicationCommon = "CleanWebApiTemplate.Application.Common.";
    private const string InfrastructurePrefix = "CleanWebApiTemplate.Infrastructure.";
    private const string InfrastructureCommon = "CleanWebApiTemplate.Infrastructure.Common.";

    [Fact]
    public void SharedApplicationContracts_ShouldLiveUnderApplicationCommon()
    {
        Assert.Equal("CleanWebApiTemplate.Application.Common.Handlers", typeof(IHandler<,>).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Common.Identity", typeof(ICurrentUser).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Common.Identity", typeof(OwnerScope).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Common.Results", typeof(Result<>).Namespace);
    }

    [Fact]
    public void ApplicationHandlers_ShouldBeSealedAndLiveUnderTheirFeature()
    {
        Type[] handlerTypes = [.. typeof(IHandler<,>).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetInterfaces()
                               .Any(contract => contract.IsGenericType
                                                && contract.GetGenericTypeDefinition() == typeof(IHandler<,>)))];

        Assert.NotEmpty(handlerTypes);
        Assert.All(handlerTypes, type =>
        {
            Assert.True(type.IsSealed, $"{type.FullName} must be sealed.");
            Assert.StartsWith(ApplicationPrefix, type.Namespace!, StringComparison.Ordinal);
            Assert.False(type.Namespace!.StartsWith(ApplicationCommon, StringComparison.Ordinal),
                         $"{type.FullName} is a feature handler and belongs in Application.<Feature>.");
            Assert.True(type.Namespace.Split('.').Length >= 4,
                        $"{type.FullName} must live in Application.<Feature>.<UseCase>.");
        });
    }

    [Fact]
    public void DomainHasNoResultOrHttpConcepts()
    {
        string[] forbidden = ["Result", "Error", "Request", "Response", "Controller"];
        Type[] domainTypes = [.. typeof(TodoEntity).Assembly.GetTypes()];

        Assert.DoesNotContain(domainTypes,
                              type => Flatten(type).Any(segment => forbidden.Contains(segment, StringComparer.Ordinal)));
        Assert.Equal("CleanWebApiTemplate.Domain.Todo", typeof(TodoEntity).Namespace);
        Assert.Equal("CleanWebApiTemplate.Domain.Todo", typeof(TodoConstraints).Namespace);
        Assert.Equal("CleanWebApiTemplate.Domain.Common", typeof(BaseEntity<>).Namespace);
    }

    [Fact]
    public void InfrastructureFeatureTypes_ShouldBeRepositoriesOrTheirSchemaHelpers()
    {
        Type[] types = [.. typeof(TodoRepository).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsNested: false })
            .Where(type => type.Namespace?.StartsWith(InfrastructurePrefix, StringComparison.Ordinal) == true)
            .Where(type => type.Namespace!.StartsWith(InfrastructureCommon, StringComparison.Ordinal) is false
                           && type.Namespace.StartsWith("CleanWebApiTemplate.Infrastructure.Secrets", StringComparison.Ordinal) is false)];

        Assert.NotEmpty(types);
        Assert.All(types, type => Assert.True(
            type.Name.EndsWith("Repository", StringComparison.Ordinal)
            || type.Name.EndsWith("Table", StringComparison.Ordinal)
            || type.Name.EndsWith("SortColumns", StringComparison.Ordinal),
            $"{type.FullName} is not a repository, a table map or a sort-column map."));
    }

    [Fact]
    public void FeaturePorts_ShouldLiveInTheFeatureTheyServe()
    {
        Assert.Equal("CleanWebApiTemplate.Application.Todo", typeof(ITodoRepository).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Todo", typeof(TodoDto).Namespace);
        Assert.Equal("CleanWebApiTemplate.Application.Todo", typeof(TodoFilter).Namespace);
        Assert.Equal("CleanWebApiTemplate.Infrastructure.Todo", typeof(TodoRepository).Namespace);
        Assert.Equal("CleanWebApiTemplate.Api.Todo", typeof(TodoRoutes).Namespace);
    }

    [Fact]
    public void ApiRoutes_ShouldLiveInTheirFeatureFolder()
    {
        Type[] routeTypes = [.. typeof(Program).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsSealed: true, IsNested: false })
            .Where(type => typeof(IGroupMap).IsAssignableFrom(type))];

        Assert.NotEmpty(routeTypes);
        Assert.All(routeTypes, type =>
        {
            Assert.StartsWith("CleanWebApiTemplate.Api.", type.Namespace!, StringComparison.Ordinal);
            Assert.False(type.Namespace!.StartsWith("CleanWebApiTemplate.Api.Common.", StringComparison.Ordinal),
                         $"{type.FullName} is a feature route group and belongs in Api.<Feature>.");
        });
    }

    [Fact]
    public void ApplicationAssembly_ShouldNotReferenceInfrastructure()
    {
        Assert.DoesNotContain(typeof(Result<>).Assembly.GetReferencedAssemblies(),
                              reference => reference.Name?.Contains("Infrastructure", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(typeof(ServiceDefaults.Extensions).Assembly.GetReferencedAssemblies(),
                              reference => reference.Name == "CleanWebApiTemplate.Infrastructure");
    }

    private static IEnumerable<string> Flatten(Type type) =>
        (type.FullName ?? type.Name).Split('.', '+');
}
