using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Testing.UnitTests.Domain;

public class ErrorTests
{
    [Fact]
    public void BadRequestError_WithTwoArgs_Should_SetTitleAndDescription()
    {
        // Arrange & Act
        var error = new BadRequestError("Custom Title", "Custom Description");

        // Assert
        Assert.Equal("Custom Title", error.Title);
        Assert.Equal("Custom Description", error.Description);
    }

    [Fact]
    public void BadRequestError_WithOneArg_Should_SetDefaultTitle()
    {
        // Arrange & Act
        var error = new BadRequestError("Custom Description");

        // Assert
        Assert.Equal("Bad Request", error.Title);
        Assert.Equal("Custom Description", error.Description);
    }

    [Fact]
    public void DomainError_WithTwoArgs_Should_SetTitleAndDescription()
    {
        // Arrange & Act
        var error = new DomainError("Custom Title", "Custom Description");

        // Assert
        Assert.Equal("Custom Title", error.Title);
        Assert.Equal("Custom Description", error.Description);
    }

    [Fact]
    public void DomainError_WithOneArg_Should_SetDefaultTitle()
    {
        // Arrange & Act
        var error = new DomainError("Custom Description");

        // Assert
        Assert.Equal("Domain Error", error.Title);
        Assert.Equal("Custom Description", error.Description);
    }

    [Fact]
    public void NotFoundError_WithTwoArgs_Should_SetTitleAndDescription()
    {
        // Arrange & Act
        var error = new NotFoundError("Custom Title", "Custom Description");

        // Assert
        Assert.Equal("Custom Title", error.Title);
        Assert.Equal("Custom Description", error.Description);
    }

    [Fact]
    public void NotFoundError_WithOneArg_Should_SetDefaultTitle()
    {
        // Arrange & Act
        var error = new NotFoundError("Custom Description");

        // Assert
        Assert.Equal("Not Found", error.Title);
        Assert.Equal("Custom Description", error.Description);
    }

    [Fact]
    public void ConflictError_WithTwoArgs_Should_SetTitleAndDescription()
    {
        // Arrange & Act
        var error = new ConflictError("Custom Title", "Custom Description");

        // Assert
        Assert.Equal("Custom Title", error.Title);
        Assert.Equal("Custom Description", error.Description);
    }

    [Fact]
    public void ConflictError_WithOneArg_Should_SetDefaultTitle()
    {
        // Arrange & Act
        var error = new ConflictError("Custom Description");

        // Assert
        Assert.Equal("Conflict", error.Title);
        Assert.Equal("Custom Description", error.Description);
    }

    [Fact]
    public void UnauthorizedError_Should_HaveNullTitleAndDescription()
    {
        // Arrange & Act
        var error = new UnauthorizedError();

        // Assert
        Assert.Null(error.Title);
        Assert.Null(error.Description);
    }

    [Fact]
    public void ForbiddenError_Should_HaveNullTitleAndDescription()
    {
        // Arrange & Act
        var error = new ForbiddenError();

        // Assert
        Assert.Null(error.Title);
        Assert.Null(error.Description);
    }
}
