using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using CleanWebApiTemplate.Api;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Repositories.Todo;
using CleanWebApiTemplate.ServiceDefaults;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace CleanWebApiTemplate.ArchitectureTests;

/// <summary>
/// Fitness functions for the dependency rule. They fail when a layer starts depending on an
/// outer layer, when Result moves back to Domain, or when the repositories folder becomes a
/// generic dumping ground again.
/// </summary>
public class DependencyRulesTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(TodoEntity).Assembly,
            typeof(Result<>).Assembly,
            typeof(TodoRepository).Assembly,
            typeof(Program).Assembly,
            typeof(Extensions).Assembly)
        .Build();

    [Fact]
    public void Domain_ShouldNotDependOnOuterLayers()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^CleanWebApiTemplate\.Domain(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(
                    @"^CleanWebApiTemplate\.(Application|Infrastructure|Api|ServiceDefaults)(\..*)?$"))
            .Because("Domain is the innermost layer");

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_ShouldNotDependOnInfrastructureOrApi()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^CleanWebApiTemplate\.Application(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(
                    @"^CleanWebApiTemplate\.(Infrastructure|Api|ServiceDefaults)(\..*)?$"))
            .Because("Application defines the ports; Infrastructure implements them");

        rule.Check(Architecture);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApiOrServiceDefaults()
    {
        IArchRule rule = Types().That().ResideInNamespaceMatching(@"^CleanWebApiTemplate\.Infrastructure(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(
                    @"^CleanWebApiTemplate\.(Api|ServiceDefaults)(\..*)?$"))
            .Because("Infrastructure is an adapter, not a composition root");

        rule.Check(Architecture);
    }

    [Fact]
    public void ApplicationResults_ShouldNotReferenceAspNetCore()
    {
        var references = typeof(Result<>).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
                              reference => reference.Name?.StartsWith("Microsoft.AspNetCore",
                                                                   StringComparison.Ordinal) == true);
    }

    [Fact]
    public void DomainAssembly_ShouldNotReferenceAspNetCore()
    {
        var references = typeof(TodoEntity).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
                              reference => reference.Name?.StartsWith("Microsoft.AspNetCore",
                                                                   StringComparison.Ordinal) == true);
    }

    [Fact]
    public void ApplicationAssembly_ShouldNotReferenceInfrastructure()
    {
        var references = typeof(Result<>).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(references,
                              reference => reference.Name == "CleanWebApiTemplate.Infrastructure");
    }
}
