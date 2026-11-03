using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Handlers.Update;
using CleanWebApiTemplate.Application.Validation;
using CleanWebApiTemplate.Domain.Models.Entities;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Todo;

/// <summary>
/// Update handler coverage with a substituted repository: conflicts and missing rows are results,
/// never controlled exceptions.
/// </summary>
public class UpdateTodoHandlerTests
{
    private const string Owner = "user@example.com";

    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly UpdateTodoHandler _handler;
    private readonly TodoEntity _todo;

    public UpdateTodoHandlerTests()
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
        _handler = new UpdateTodoHandler(
            _repository,
            new ValidatorSet<UpdateTodoInput>([new UpdateTodoInputValidator()]));
    }

    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var missingId = Ulid.NewUlid();
        _repository.GetByIdAsync(missingId, Owner, Arg.Any<CancellationToken>())
                   .Returns((TodoEntity?)null);

        // Act
        var result = await _handler.HandleAsync(Input(missingId), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WithTitleAlreadyUsedByAnotherTodo_ReturnsConflictWithoutUpdating()
    {
        // Arrange
        _repository.TitleExistsAsync("Taken", Owner, _todo.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.HandleAsync(
            Input(_todo.Id, title: "Taken"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryReportsDuplicate_PropagatesConflict()
    {
        // Arrange: the pre-check passed but a concurrent writer took the title first.
        _repository.UpdateAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Failure(new ConflictError("duplicate")));

        // Act
        var result = await _handler.HandleAsync(
            Input(_todo.Id, title: "Taken"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenRowDisappearsBeforeUpdate_ReturnsNotFound()
    {
        // Arrange
        _repository.UpdateAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Success(false));

        // Act
        var result = await _handler.HandleAsync(
            Input(_todo.Id, description: "changed"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WithValidChanges_ReturnsUpdatedDto()
    {
        // Arrange
        _repository.UpdateAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Success(true));

        // Act
        var result = await _handler.HandleAsync(
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
