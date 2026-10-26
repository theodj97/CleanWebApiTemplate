using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Api.UnitTests.Domain.ResultModel;

/// <summary>Default titles and the explicit title overload of every error type.</summary>
public class ErrorTests
{
    [Fact]
    public void BadRequestError_WithDescription_UsesDefaultTitle()
    {
        // Act
        var error = new BadRequestError("invalid input");

        // Assert
        Assert.Equal("Bad Request", error.Title);
        Assert.Equal("invalid input", error.Description);
    }

    [Fact]
    public void BadRequestError_WithTitleAndDescription_UsesProvidedTitle()
    {
        // Act
        var error = new BadRequestError("Custom", "invalid input");

        // Assert
        Assert.Equal("Custom", error.Title);
        Assert.Equal("invalid input", error.Description);
    }

    [Fact]
    public void DomainError_WithDescription_UsesDefaultTitle()
    {
        // Act
        var error = new DomainError("rule violated");

        // Assert
        Assert.Equal("Domain Error", error.Title);
        Assert.Equal("rule violated", error.Description);
    }

    [Fact]
    public void DomainError_WithTitleAndDescription_UsesProvidedTitle()
    {
        // Act
        var error = new DomainError("Custom", "rule violated");

        // Assert
        Assert.Equal("Custom", error.Title);
        Assert.Equal("rule violated", error.Description);
    }

    [Fact]
    public void NotFoundError_WithDescription_UsesDefaultTitle()
    {
        // Act
        var error = new NotFoundError("missing");

        // Assert
        Assert.Equal("Not Found", error.Title);
        Assert.Equal("missing", error.Description);
    }

    [Fact]
    public void NotFoundError_WithTitleAndDescription_UsesProvidedTitle()
    {
        // Act
        var error = new NotFoundError("Custom", "missing");

        // Assert
        Assert.Equal("Custom", error.Title);
        Assert.Equal("missing", error.Description);
    }

    [Fact]
    public void ConflictError_WithDescription_UsesDefaultTitle()
    {
        // Act
        var error = new ConflictError("duplicate");

        // Assert
        Assert.Equal("Conflict", error.Title);
        Assert.Equal("duplicate", error.Description);
    }

    [Fact]
    public void ConflictError_WithTitleAndDescription_UsesProvidedTitle()
    {
        // Act
        var error = new ConflictError("Custom", "duplicate");

        // Assert
        Assert.Equal("Custom", error.Title);
        Assert.Equal("duplicate", error.Description);
    }

    [Fact]
    public void UnauthorizedError_HasNoTitleOrDescription()
    {
        // Act
        var error = new UnauthorizedError();

        // Assert
        Assert.Null(error.Title);
        Assert.Null(error.Description);
    }

    [Fact]
    public void ForbiddenError_HasNoTitleOrDescription()
    {
        // Act
        var error = new ForbiddenError();

        // Assert
        Assert.Null(error.Title);
        Assert.Null(error.Description);
    }
}
