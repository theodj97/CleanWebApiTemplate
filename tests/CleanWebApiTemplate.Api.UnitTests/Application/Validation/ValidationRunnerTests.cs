using CleanWebApiTemplate.Application.Todo.Create;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;
using NSubstitute;

namespace CleanWebApiTemplate.Api.UnitTests.Application.Validation;

/// <summary>A service with no registered validators must run its action instead of failing.</summary>
public class ValidationRunnerTests
{
    [Fact]
    public async Task CreateAsync_WithoutValidators_RunsTheAction()
    {
        // Arrange
        var repository = Substitute.For<ITodoRepository>();
        repository.InsertAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>())
                  .Returns(call => call.Arg<TodoEntity>());
        var service = new CreateTodoService(repository, []);

        // Act
        var result = await service.CreateAsync(
            new CreateTodoInput { Title = "No validators", CreatedBy = "user@example.com" },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        await repository.Received(1).InsertAsync(Arg.Any<TodoEntity>(), Arg.Any<CancellationToken>());
    }
}
