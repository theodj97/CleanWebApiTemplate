using CleanWebApiTemplate.Domain.ResultModel;

namespace CleanWebApiTemplate.Api.UnitTests.Domain.ResultModel;

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
        Assert.False(result.IsNoContent);
        Assert.False(result.IsCreated);
        Assert.Null(result.Error);
        Assert.Same(value, result.Value);
    }

    [Fact]
    public void Success_WithNullValue_ReturnsNoContent()
    {
        // Act
        var result = Result<object?>.Success(null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsNoContent);
    }

    [Fact]
    public void Success_WithEmptyCollection_ReturnsNoContent()
    {
        // Act
        var result = Result<object>.Success(Array.Empty<object>());

        // Assert
        Assert.True(result.IsNoContent);
    }

    [Fact]
    public void Success_WithNonEmptyCollection_IsNotNoContent()
    {
        // Act
        var result = Result<object>.Success(new object[] { new() });

        // Assert
        Assert.False(result.IsNoContent);
    }

    [Fact]
    public void Success_WithEmptyGenericCollection_ReturnsNoContent()
    {
        // Act
        var result = Result<IEnumerable<object>>.Success(Array.Empty<object>());

        // Assert
        Assert.True(result.IsNoContent);
    }

    [Fact]
    public void Success_WithEmptyString_IsNotNoContent()
    {
        // Act
        var result = Result<string>.Success(string.Empty);

        // Assert
        Assert.False(result.IsNoContent);
    }

    [Fact]
    public void Created_WithValue_ReturnsCreatedResult()
    {
        // Act
        var result = Result<string>.Created("created");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.IsCreated);
        Assert.False(result.IsNoContent);
        Assert.Equal("created", result.Value);
    }

    [Fact]
    public void Created_WithNullValue_ThrowsBecauseCreatedAndNoContentAreExclusive()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Result<string?>.Created(null!));
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
    public void Constructor_WithSuccessAndError_Throws()
    {
        // Arrange
        var error = new BadRequestError("boom");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new ExposedResult<string>("value", isSuccess: true, error));
    }

    [Fact]
    public void Constructor_WithFailureAndNoError_Throws()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new ExposedResult<string>(null, isSuccess: false, error: null));
    }

    /// <summary>Exposes the protected constructor so the invariants can be exercised directly.</summary>
    private sealed class ExposedResult<T>(T? value, bool isSuccess, Error? error) : Result<T>(value, isSuccess, error);
}
