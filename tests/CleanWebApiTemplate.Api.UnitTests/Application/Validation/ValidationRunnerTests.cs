using CleanWebApiTemplate.Application.Abstractions.Repositories;
using CleanWebApiTemplate.Application.Results;
using CleanWebApiTemplate.Application.Todo.Handlers.Create;
using CleanWebApiTemplate.Domain.Models.Entities;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Validation;

/// <summary>A handler with no registered validators must run its action instead of failing.</summary>
public class ValidationRunnerTests
{
    [Fact]
    public async Task HandleAsync_WithoutValidators_RunsTheAction()
    {
        // Arrange
        var repository = Substitute.For<ITodoRepository>();
        repository.InsertAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                  .Returns(call => Result<TodoEntity>.Success(call.Arg<TodoEntity>()));
        var handler = new CreateTodoHandler(repository, []);

        // Act
        var result = await handler.HandleAsync(
            new CreateTodoInput { Title = "No validators", CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).InsertAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>());
    }
}
