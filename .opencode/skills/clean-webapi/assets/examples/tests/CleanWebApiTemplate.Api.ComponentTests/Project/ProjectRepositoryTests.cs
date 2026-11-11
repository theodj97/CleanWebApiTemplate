using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Project;

/// <summary>
/// Repository behavior against the real PostgreSQL container: the unique index violation is
/// translated into a failed result, never into a controlled exception.
/// </summary>
public class ProjectRepositoryTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    public async Task Insert_WithDuplicateNameForOwner_ReturnsConflictResult()
    {
        // Arrange
        var existing = await Fixture.Projects.AddAsync(new ProjectBuilder().Build());
        var duplicate = new ProjectBuilder()
            .CreatedBy(existing.CreatedBy)
            .WithName(existing.Name)
            .Build();

        // Act
        var result = await Fixture.Projects.TryAddAsync(duplicate);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Null(result.Value);
    }
}
