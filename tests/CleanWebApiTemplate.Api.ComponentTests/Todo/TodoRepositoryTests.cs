using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Tests.Shared.Attributes;
using CleanWebApiTemplate.Tests.Shared.Data;

namespace CleanWebApiTemplate.Api.ComponentTests.Todo;

/// <summary>
/// Repository behavior against the real PostgreSQL container: the unique index violation is
/// translated into a failed result, never into a controlled exception.
/// </summary>
public class TodoRepositoryTests(ComponentTestFixture fixture) : ComponentTestBase(fixture)
{
    [Fact]
    [ResetDatabase]
    public async Task Insert_WithDuplicateTitleForOwner_ReturnsConflictResult()
    {
        // Arrange
        var existing = await Fixture.Todos.AddAsync(new TodoBuilder().Build());
        var duplicate = new TodoBuilder()
            .CreatedBy(existing.CreatedBy)
            .WithTitle(existing.Title)
            .Build();

        // Act
        var result = await Fixture.Todos.TryAddAsync(duplicate);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        Assert.Null(result.Value);
    }
}
