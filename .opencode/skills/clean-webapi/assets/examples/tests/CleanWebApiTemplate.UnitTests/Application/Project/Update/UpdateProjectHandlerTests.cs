using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Application.Project.Update;
using CleanWebApiTemplate.Domain.Project;
using NSubstitute;

namespace CleanWebApiTemplate.UnitTests.Application.Project.Update;

/// <summary>
/// Update handler coverage with a substituted repository: conflicts and missing rows are results,
/// never controlled exceptions.
/// </summary>
public class UpdateProjectHandlerTests
{
    private const string Owner = "user@example.com";

    private readonly IProjectRepository _repository = Substitute.For<IProjectRepository>();
    private readonly UpdateProjectHandler _handler;
    private readonly ProjectEntity _project;

    public UpdateProjectHandlerTests()
    {
        _project = new ProjectEntity
        {
            Id = Ulid.NewUlid(),
            Name = "Original",
            Description = "original description",
            CreatedBy = Owner,
            UpdatedBy = Owner
        };

        _repository.GetByIdAsync(_project.Id, Owner, Arg.Any<CancellationToken>()).Returns(_project);
        _repository.NameExistsAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Ulid?>(), Arg.Any<CancellationToken>())
                   .Returns(false);
        _handler = new UpdateProjectHandler(
            _repository,
            new ValidatorSet<UpdateProjectInput>([new UpdateProjectInputValidator()]));
    }

    [Fact]
    public async Task HandleAsync_WithUnknownId_ReturnsNotFound()
    {
        // Arrange
        var missingId = Ulid.NewUlid();
        _repository.GetByIdAsync(missingId, Owner, Arg.Any<CancellationToken>())
                   .Returns((ProjectEntity?)null);

        // Act
        var result = await _handler.HandleAsync(Input(missingId), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WithNameAlreadyUsedByAnotherProject_ReturnsConflictWithoutUpdating()
    {
        // Arrange
        _repository.NameExistsAsync("Taken", Owner, _project.Id, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.HandleAsync(
            Input(_project.Id, name: "Taken"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenRepositoryReportsDuplicate_PropagatesConflict()
    {
        // Arrange: the pre-check passed but a concurrent writer took the name first.
        _repository.UpdateAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Failure(new ConflictError("duplicate")));

        // Act
        var result = await _handler.HandleAsync(
            Input(_project.Id, name: "Taken"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<ConflictError>(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WhenRowDisappearsBeforeUpdate_ReturnsNotFound()
    {
        // Arrange
        _repository.UpdateAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Success(false));

        // Act
        var result = await _handler.HandleAsync(
            Input(_project.Id, description: "changed"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsFailure);
        Assert.IsType<NotFoundError>(result.Error);
    }

    [Fact]
    public async Task HandleAsync_WithValidChanges_ReturnsUpdatedDto()
    {
        // Arrange
        _repository.UpdateAsync(Arg.Any<ProjectEntity>(), Arg.Any<CancellationToken>())
                   .Returns(Result<bool>.Success(true));

        // Act
        var result = await _handler.HandleAsync(
            Input(_project.Id, name: "  Renamed  ", description: "changed"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Renamed", result.Value!.Name);
        Assert.Equal("changed", result.Value.Description);
        Assert.Equal(Owner, result.Value.UpdatedBy);
    }

    private UpdateProjectInput Input(Ulid id, string? name = null, string? description = null) => new()
    {
        Id = id.ToString(),
        Name = name,
        Description = description,
        LastWriter = Owner,
        VisibleOwner = Owner
    };
}
