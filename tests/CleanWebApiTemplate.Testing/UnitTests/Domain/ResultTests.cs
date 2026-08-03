using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Testing.UnitTests.Domain;

public class ResultTests
{
    [Fact]
    public void Success_WithValue_Should_ReturnSuccessResult()
    {
        // Arrange & Act
        var result = Result<string>.Success("test");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("test", result.Value);
        Assert.Null(result.Error);
        Assert.False(result.IsCreated);
        Assert.False(result.IsNoContent);
    }

    [Fact]
    public void Success_WithNullValue_Should_SetIsNoContent()
    {
        // Arrange & Act
        var result = Result<string>.Success(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsNoContent);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Success_WithNonEmptyEnumerable_Should_NotSetIsNoContent()
    {
        // Arrange & Act
        var result = Result<IEnumerable<string>>.Success(["item1"]);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsNoContent);
    }

    [Fact]
    public void Failure_WithError_Should_ReturnFailureResult()
    {
        // Arrange
        var error = new BadRequestError("Test error");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Created_WithValue_Should_ReturnCreatedResult()
    {
        // Arrange & Act
        var result = Result<string>.Created("test");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsCreated);
        Assert.False(result.IsNoContent);
        Assert.Equal("test", result.Value);
    }
}
