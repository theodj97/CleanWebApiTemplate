using CleanWebApiTemplate.Application.Common.Results;

namespace CleanWebApiTemplate.UnitTests.Application.Common.Results;

/// <summary>Invariants of <see cref="Result{T}"/>: one success shape and one failure shape, never a mix.</summary>
public class ResultTests
{
    [Fact]
    public void Success_WithValue_ReturnsSuccessfulResultWithValue()
    {
        // Arrange
        var value = new object();

        // Act
        var result = Result<object>.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Same(value, result.Value);
    }

    [Fact]
    public void Failure_WithError_ReturnsFailedResultWithError()
    {
        // Arrange
        var error = new NotFoundError("missing");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(error, result.Error);
        Assert.Null(result.Value);
    }

    [Fact]
    public void Failure_WithNullError_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }

    [Fact]
    public void Success_WithValue_ExposesNoError()
    {
        // Arrange
        var error = new ConflictError("boom");

        // Act
        var success = Result<string>.Success("value");
        var failure = Result<string>.Failure(error);

        // Assert
        Assert.NotSame(error, success.Error);
        Assert.Same(error, failure.Error);
    }
}
