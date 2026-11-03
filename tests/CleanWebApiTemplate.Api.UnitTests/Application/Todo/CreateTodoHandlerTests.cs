using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Handlers.Create;
using CleanWebApiTemplate.Domain.Models.Entities;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Todo;

/// <summary>
/// Create handler coverage with a substituted repository: title uniqueness is a business rule
/// reported as a conflict, and repository failures are propagated instead of thrown.
/// </summary>
public class CreateTodoHandlerTests
{
    private const string Owner = "user@example.com";

    private readonly ITodoRepository _repository = Substitute.For<ITodoRepository>();
    private readonly CreateTodoHandler _handler;

    public CreateTodoHandlerTests()
    {
        _repository.TitleExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(false);
        _handler = new CreateTodoHandler(_repository, []);
    }

    [Fact]
    public async Task HandleAsync_WithTitleAlreadyUsedByOwner_ReturnsConflictWithoutInserting()
    {
        // Arrange
        _repository.TitleExistsAsync("Taken", Owner, Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(true);

        // Act
        var result = await _handler.HandleAsync(
            new CreateTodoInput { Title = "Taken", CreatedBy = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        await _repository.DidNotReceive().InsertAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithTitleUsedByAnotherOwner_Inserts()
    {
        // Arrange
        _repository.TitleExistsAsync("Taken", "other@example.com", Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(true);
        _repository.InsertAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(call => Result<TodoEntity>.Success(call.Arg<TodoEntity>()));

        // Act
        var result = await _handler.HandleAsync(
            new CreateTodoInput { Title = "Taken", CreatedBy = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        await _repository.Received(1).TitleExistsAsync("Taken", Owner, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryReportsDuplicate_PropagatesConflict()
    {
        // Arrange: the pre-check passed but a concurrent writer inserted the same title first.
        _repository.InsertAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<TodoEntity>.Failure(new ConflictError("duplicate")));

        // Act
        var result = await _handler.HandleAsync(
            new CreateTodoInput { Title = "Taken", CreatedBy = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WithUniqueTitle_ReturnsCreatedDto()
    {
        // Arrange
        _repository.InsertAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                   .Returns(call => Result<TodoEntity>.Success(call.Arg<TodoEntity>()));

        // Act
        var result = await _handler.HandleAsync(
            new CreateTodoInput { Title = "  Unique  ", Description = "details", CreatedBy = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Unique", result.Value!.Title);
        Assert.Equal("details", result.Value.Description);
        Assert.Equal(Owner, result.Value.CreatedBy);
        await _repository.Received(1).TitleExistsAsync("Unique", Owner, null, Arg.Any<CancellationToken>());
    }
}
