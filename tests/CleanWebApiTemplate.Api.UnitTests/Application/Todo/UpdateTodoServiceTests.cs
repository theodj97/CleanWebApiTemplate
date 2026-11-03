using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Abstractions;
using CleanWebApiTemplate.Application.Todo.Update;
using CleanWebApiTemplate.Domain.Models.Entities;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Todo;

/// <summary>
/// Update service coverage with a substituted repository: conflicts and missing rows are results,
/// never controlled exceptions.
/// </summary>
public class UpdateTodoServiceTests
{
    private const string Owner = "user@example.com";

    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly UpdateTodoService _service;
    private readonly TodoEntity _todo;

    public UpdateTodoServiceTests()
    {
        _todo = new TodoEntity
        {
            Id = Ulid.NewUlid(),
            Title = "Original",
            Description = "original description",
            CreatedBy = Owner,
            UpdatedBy = Owner
        };

        _repository.GetByIdAsync(_todo.Id, Owner, Arg.Any<CancellationToken>()).Returns(_todo);
        _repository.TitleExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(false);
        _service = new UpdateTodoService(_repository, []);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var missingId = Ulid.NewUlid();
        _repository.GetByIdAsync(missingId, Owner, Arg.Any<CancellationToken>())
                   .Returns((TodoEntity?)null);

        // Act
        var result = await _service.UpdateAsync(Input(missingId), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithTitleAlreadyUsedByAnotherTodo_ReturnsConflictWithoutUpdating()
    {
        // Arrange
        _repository.TitleExistsAsync("Taken", Owner, _todo.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _service.UpdateAsync(
            Input(_todo.Id, title: "Taken"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenRepositoryReportsDuplicate_PropagatesConflict()
    {
        // Arrange: the pre-check passed but a concurrent writer took the title first.
        _repository.UpdateAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Failure(new ConflictError("duplicate")));

        // Act
        var result = await _service.UpdateAsync(
            Input(_todo.Id, title: "Taken"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WhenRowDisappearsBeforeUpdate_ReturnsNotFound()
    {
        // Arrange
        _repository.UpdateAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Success(false));

        // Act
        var result = await _service.UpdateAsync(
            Input(_todo.Id, description: "changed"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task UpdateAsync_WithValidChanges_ReturnsUpdatedDto()
    {
        // Arrange
        _repository.UpdateAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Success(true));

        // Act
        var result = await _service.UpdateAsync(
            Input(_todo.Id, title: "  Renamed  ", description: "changed"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Renamed", result.Value!.Title);
        Assert.Equal("changed", result.Value.Description);
        Assert.Equal(Owner, result.Value.UpdatedBy);
    }

    private UpdateTodoInput Input(Ulid id, string? title = null, string? description = null) => new()
    {
        Id = id.ToString(),
        Title = title,
        Description = description,
        UpdatedBy = Owner,
        CreatedBy = Owner
    };
}
