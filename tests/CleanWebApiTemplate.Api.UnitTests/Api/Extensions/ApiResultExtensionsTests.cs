using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Api.Models.Responses;
using CleanWebApiTemplate.Application.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Extensions;

/// <summary>Every domain error becomes the right HTTP status, and every success shape the right status/body.</summary>
public class ApiResultExtensionsTests
{
    [Fact]
    public async Task ToResponse_WithSuccess_ReturnsOkWithBody()
    {
        // Arrange
        var result = Result<TestDto>.Success(new TestDto { Name = "value" });

        // Act
        var response = await ExecuteAsync(result.ToResponse<TestDto, TestResponse>());

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.Response.StatusCode);
        Assert.Contains("\"value\"", await ReadBodyAsync(response));
    }

    [Fact]
    public async Task ToCreatedResponse_WithValue_ReturnsCreatedAndLocation()
    {
        // Act
        var response = await ExecuteAsync(
            Result<TestDto>.Success(new TestDto { Name = "created" })
                           .ToCreatedResponse<TestDto, TestResponse>(_ => "/api/test/1"));

        // Assert
        Assert.Equal(StatusCodes.Status201Created, response.Response.StatusCode);
        Assert.Equal("/api/test/1", response.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task ToResponse_WithNoContentResult_ReturnsNoContent()
    {
        // Act
        var response = await ExecuteAsync(Result<TestDto>.Success(null).ToResponse<TestDto, TestResponse>());

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, response.Response.StatusCode);
    }

    [Fact]
    public async Task ToResponse_WithCollectionSuccess_ReturnsOkWithEveryItem()
    {
        // Arrange
        var result = Result<IEnumerable<TestDto>>.Success([new TestDto { Name = "one" }, new TestDto { Name = "two" }]);

        // Act
        var response = await ExecuteAsync(result.ToResponse<TestDto, TestResponse>());

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.Response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Contains("\"one\"", body);
        Assert.Contains("\"two\"", body);
    }

    [Fact]
    public async Task ToResponse_WithEmptyCollection_ReturnsNoContent()
    {
        // Act
        var response = await ExecuteAsync(Result<IEnumerable<TestDto>>.Success([])
                                                          .ToResponse<TestDto, TestResponse>());

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, response.Response.StatusCode);
    }

    [Fact]
    public async Task ToResponse_WithGenericValue_ReturnsOk()
    {
        // Act
        var response = await ExecuteAsync(Result<bool>.Success(true).ToResponse());

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.Response.StatusCode);
        Assert.Equal("true", await ReadBodyAsync(response));
    }

    [Fact]
    public async Task ToResponse_WithEmptyString_ReturnsOk()
    {
        // Act
        var response = await ExecuteAsync(Result<string>.Success(string.Empty).ToResponse());

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.Response.StatusCode);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, typeof(BadRequestError))]
    [InlineData(StatusCodes.Status400BadRequest, typeof(DomainError))]
    [InlineData(StatusCodes.Status404NotFound, typeof(NotFoundError))]
    [InlineData(StatusCodes.Status409Conflict, typeof(ConflictError))]
    public async Task ToResponse_WithProblemError_ReturnsTheMatchingProblemDetails(int expectedStatus, Type errorType)
    {
        // Arrange
        Error error = errorType == typeof(BadRequestError) ? new BadRequestError("failure")
                     : errorType == typeof(DomainError) ? new DomainError("failure")
                     : errorType == typeof(NotFoundError) ? new NotFoundError("failure")
                     : new ConflictError("failure");

        // Act
        var response = await ExecuteAsync(Result<TestDto>.Failure(error).ToResponse<TestDto, TestResponse>());

        // Assert
        Assert.Equal(expectedStatus, response.Response.StatusCode);
        Assert.Contains("failure", await ReadBodyAsync(response));
    }

    [Fact]
    public async Task ToResponse_WithUnauthorizedError_ReturnsUnauthorized()
    {
        // Act
        var response = await ExecuteAsync(Result<TestDto>.Failure(new UnauthorizedError())
                                                          .ToResponse<TestDto, TestResponse>());

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, response.Response.StatusCode);
    }

    [Fact]
    public void ToResponse_WithForbiddenError_ReturnsForbidResult()
    {
        // Act
        var result = Result<TestDto>.Failure(new ForbiddenError()).ToResponse<TestDto, TestResponse>();

        // Assert
        Assert.Equal("ForbidHttpResult", result.GetType().Name);
    }

    [Fact]
    public void ToResponse_WithUnhandledErrorType_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Result<TestDto>.Failure(new UnknownError()).ToResponse<TestDto, TestResponse>());
    }

    private static async Task<HttpContext> ExecuteAsync(IResult result)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestServices = new ServiceCollection()
                                   .AddLogging()
                                   .AddProblemDetails()
                                   .BuildServiceProvider();

        await result.ExecuteAsync(context);
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
    }

    private sealed record TestDto
    {
        public string? Name { get; init; }
    }

    private sealed record TestResponse : IBaseResponse<TestDto, TestResponse>
    {
        public required string Name { get; init; }

        public static TestResponse? ToResponseModel(TestDto? dto) =>
            dto is null ? null : new() { Name = dto.Name ?? string.Empty };
    }

    private sealed record UnknownError : Error;
}
