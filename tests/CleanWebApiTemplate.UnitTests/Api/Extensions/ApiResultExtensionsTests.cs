using CleanWebApiTemplate.Api.Common.Responses;
using CleanWebApiTemplate.Api.Extensions;
using CleanWebApiTemplate.Application.Common.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CleanWebApiTemplate.UnitTests.Api.Extensions;

/// <summary>Every domain error becomes the right HTTP status, and every success shape the right status/body.</summary>
public class ApiResultExtensionsTests
{
    [Fact]
    public async Task ToResponse_WithSuccess_ReturnsOkWithBody()
    {
        // Arrange
        var context = CreateContext();
        var result = Result<TestDto>.Success(new TestDto { Name = "value" });

        // Act
        var response = await ExecuteAsync(result.ToResponse<TestDto, TestResponse>(context), context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.Response.StatusCode);
        Assert.Contains("\"value\"", await ReadBodyAsync(response));
    }

    [Fact]
    public async Task ToCreatedResponse_WithValue_ReturnsCreatedAndLocation()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var response = await ExecuteAsync(
            Result<TestDto>.Success(new TestDto { Name = "created" })
                           .ToCreatedResponse<TestDto, TestResponse>(context, _ => "/api/test/1"),
            context);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, response.Response.StatusCode);
        Assert.Equal("/api/test/1", response.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task ToResponse_WithMissingValue_ReturnsInternalServerError()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var response = await ExecuteAsync(
            Result<TestDto>.Success(null!).ToResponse<TestDto, TestResponse>(context), context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, response.Response.StatusCode);
        Assert.Contains("no value", await ReadBodyAsync(response));
    }

    [Fact]
    public async Task ToResponse_WithCollectionSuccess_ReturnsOkWithEveryItem()
    {
        // Arrange
        var context = CreateContext();
        var result = Result<IEnumerable<TestDto>>.Success([new TestDto { Name = "one" }, new TestDto { Name = "two" }]);

        // Act
        var response = await ExecuteAsync(result.ToResponse<TestDto, TestResponse>(context), context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.Response.StatusCode);
        var body = await ReadBodyAsync(response);
        Assert.Contains("\"one\"", body);
        Assert.Contains("\"two\"", body);
    }

    [Fact]
    public async Task ToResponse_WithEmptyCollection_ReturnsOkWithEmptyArray()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var response = await ExecuteAsync(
            Result<IEnumerable<TestDto>>.Success([]).ToResponse<TestDto, TestResponse>(context),
            context);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, response.Response.StatusCode);
        Assert.Equal("[]", await ReadBodyAsync(response));
    }

    [Fact]
    public async Task ToNoContentResponse_WithSuccess_ReturnsNoContent()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var response = await ExecuteAsync(Result<bool>.Success(true).ToNoContentResponse(context), context);

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, response.Response.StatusCode);
        Assert.Empty(await ReadBodyAsync(response));
    }

    [Fact]
    public async Task ToNoContentResponse_WithFailure_ReturnsProblemDetails()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var response = await ExecuteAsync(
            Result<bool>.Failure(new NotFoundError("missing")).ToNoContentResponse(context), context);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, response.Response.StatusCode);
        Assert.Contains("missing", await ReadBodyAsync(response));
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, typeof(BadRequestError))]
    [InlineData(StatusCodes.Status400BadRequest, typeof(DomainError))]
    [InlineData(StatusCodes.Status404NotFound, typeof(NotFoundError))]
    [InlineData(StatusCodes.Status409Conflict, typeof(ConflictError))]
    public async Task ToResponse_WithProblemError_ReturnsTheMatchingProblemDetails(int expectedStatus, Type errorType)
    {
        // Arrange
        var context = CreateContext();
        Error error = errorType == typeof(BadRequestError) ? new BadRequestError("failure")
                     : errorType == typeof(DomainError) ? new DomainError("failure")
                     : errorType == typeof(NotFoundError) ? new NotFoundError("failure")
                     : new ConflictError("failure");

        // Act
        var response = await ExecuteAsync(
            Result<TestDto>.Failure(error).ToResponse<TestDto, TestResponse>(context), context);

        // Assert
        Assert.Equal(expectedStatus, response.Response.StatusCode);
        Assert.Equal("application/problem+json", response.Response.ContentType?.Split(';')[0]);
        Assert.Contains("failure", await ReadBodyAsync(response));
    }

    [Fact]
    public async Task ToResponse_WithUnauthorizedError_ReturnsUnauthorizedProblem()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var response = await ExecuteAsync(
            Result<TestDto>.Failure(new UnauthorizedError()).ToResponse<TestDto, TestResponse>(context),
            context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, response.Response.StatusCode);
        Assert.Equal("application/problem+json", response.Response.ContentType?.Split(';')[0]);
    }

    [Fact]
    public async Task ToResponse_WithForbiddenError_ReturnsForbiddenProblem()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var response = await ExecuteAsync(
            Result<TestDto>.Failure(new ForbiddenError()).ToResponse<TestDto, TestResponse>(context),
            context);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, response.Response.StatusCode);
        Assert.Equal("application/problem+json", response.Response.ContentType?.Split(';')[0]);
    }

    [Fact]
    public void ToResponse_WithUnhandledErrorType_Throws()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Result<TestDto>.Failure(new UnknownError()).ToResponse<TestDto, TestResponse>(context));
    }

    [Fact]
    public async Task ToResponse_WithProblemError_AddsTheTraceId()
    {
        // Arrange
        var context = CreateContext();
        context.TraceIdentifier = "trace-123";

        // Act
        var response = await ExecuteAsync(
            Result<TestDto>.Failure(new ConflictError("failure")).ToResponse<TestDto, TestResponse>(context),
            context);

        // Assert
        Assert.Contains("\"traceId\":\"trace-123\"", await ReadBodyAsync(response));
    }

    private static HttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.RequestServices = new ServiceCollection()
                                   .AddLogging()
                                   .AddProblemDetails()
                                   .BuildServiceProvider();
        return context;
    }

    private static async Task<HttpContext> ExecuteAsync(IResult result, HttpContext context)
    {
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
        public required string Name { get; init; }
    }

    private sealed record TestResponse : IBaseResponse<TestDto, TestResponse>
    {
        public required string Name { get; init; }

        public static TestResponse ToResponseModel(TestDto dto) => new() { Name = dto.Name };
    }

    private sealed record UnknownError : Error;
}
