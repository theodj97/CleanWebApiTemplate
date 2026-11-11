using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Application.Project.Create;
using CleanWebApiTemplate.Domain.Project;
using NSubstitute;

namespace CleanWebApiTemplate.UnitTests.Application.Project.Create;

/// <summary>
/// Create handler coverage with a substituted repository: name uniqueness is a business rule
/// reported as a conflict, and repository failures are propagated instead of thrown.
/// </summary>
public class CreateProjectHandlerTests
{
    private const string Owner = "user@example.com";

    private readonly IProjectRepository _repository = Substitute.For<IProjectRepository>();
    private readonly CreateProjectHandler _handler;

    public CreateProjectHandlerTests()
    {
        _repository.NameExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(false);
        _handler = new CreateProjectHandler(
            _repository,
            new ValidatorSet<CreateProjectInput>([new CreateProjectInputValidator()]));
    }

    [Fact]
    public async Task HandleAsync_WithNameAlreadyUsedByOwner_ReturnsConflictWithoutInserting()
    {
        // Arrange
        _repository.NameExistsAsync("Taken", Owner, Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(true);

        // Act
        var result = await _handler.HandleAsync(
            new CreateProjectInput { Name = "Taken", Owner = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        await _repository.DidNotReceive().InsertAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNameUsedByAnotherOwner_Inserts()
    {
        // Arrange
        _repository.NameExistsAsync("Taken", "other@example.com", Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(true);
        _repository.InsertAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>())
                   .Returns(call => Result<ProjectEntity>.Success(call.Arg<ProjectEntity>()));

        // Act
        var result = await _handler.HandleAsync(
            new CreateProjectInput { Name = "Taken", Owner = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        await _repository.Received(1).NameExistsAsync("Taken", Owner, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryReportsDuplicate_PropagatesConflict()
    {
        // Arrange: the pre-check passed but a concurrent writer inserted the same name first.
        _repository.InsertAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<ProjectEntity>.Failure(new ConflictError("duplicate")));

        // Act
        var result = await _handler.HandleAsync(
            new CreateProjectInput { Name = "Taken", Owner = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WithUniqueName_ReturnsCreatedDto()
    {
        // Arrange
        _repository.InsertAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>())
                   .Returns(call => Result<ProjectEntity>.Success(call.Arg<ProjectEntity>()));

        // Act
        var result = await _handler.HandleAsync(
            new CreateProjectInput { Name = "  Unique  ", Description = "details", Owner = Owner },
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Unique", result.Value!.Name);
        Assert.Equal("details", result.Value.Description);
        Assert.Equal(Owner, result.Value.CreatedBy);
        await _repository.Received(1).NameExistsAsync("Unique", Owner, null, Arg.Any<CancellationToken>());
    }
}
