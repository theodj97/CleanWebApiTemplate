using CleanWebApiTemplate.Domain.Models.Dtos.Todo;
using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Host.Extensions;
using CleanWebApiTemplate.Host.Models.Responses.Todo;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace CleanWebApiTemplate.Testing.UnitTests.Extensions;

public class ApiResultExtensionsTests
{
    [Fact]
    public void ToResponse_WithBadRequestError_Should_ReturnBadRequest()
    {
        // Arrange
        var error = new BadRequestError("Test", "Test error");
        var result = Result<string>.Failure(error);

        // Act
        var response = result.ToResponse();
        var httpResult = (IStatusCodeHttpResult)response;

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, httpResult.StatusCode);
    }

    [Fact]
    public void ToResponse_WithDomainError_Should_ReturnBadRequest()
    {
        // Arrange
        var error = new DomainError("Test", "Domain error occurred");
        var result = Result<string>.Failure(error);

        // Act
        var response = result.ToResponse();
        var httpResult = (IStatusCodeHttpResult)response;

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, httpResult.StatusCode);
    }

    [Fact]
    public void ToResponse_WithNotFoundError_Should_ReturnNotFound()
    {
        // Arrange
        var error = new NotFoundError("Test", "Not found");
        var result = Result<string>.Failure(error);

        // Act
        var response = result.ToResponse();
        var httpResult = (IStatusCodeHttpResult)response;

        // Assert
        Assert.Equal((int)HttpStatusCode.NotFound, httpResult.StatusCode);
    }

    [Fact]
    public void ToResponse_WithConflictError_Should_ReturnConflict()
    {
        // Arrange
        var error = new ConflictError("Test", "Conflict occurred");
        var result = Result<string>.Failure(error);

        // Act
        var response = result.ToResponse();
        var httpResult = (IStatusCodeHttpResult)response;

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, httpResult.StatusCode);
    }

    [Fact]
    public void ToResponse_WithUnauthorizedError_Should_ReturnUnauthorized()
    {
        // Arrange
        var error = new UnauthorizedError();
        var result = Result<string>.Failure(error);

        // Act
        var response = result.ToResponse();

        // Assert - UnauthorizedHttpResult doesn't implement IStatusCodeHttpResult
        Assert.Contains("UnauthorizedHttpResult", response.GetType().Name);
    }

    [Fact]
    public void ToResponse_WithForbiddenError_Should_ReturnForbidden()
    {
        // Arrange
        var error = new ForbiddenError();
        var result = Result<string>.Failure(error);

        // Act
        var response = result.ToResponse();

        // Assert - ForbidHttpResult doesn't implement IStatusCodeHttpResult
        Assert.Contains("ForbidHttpResult", response.GetType().Name);
    }

    [Fact]
    public void ToResponse_WithNoContent_Should_ReturnNoContent()
    {
        // Arrange
        var result = Result<string>.Success(null);

        // Act
        var response = result.ToResponse();
        var httpResult = (IStatusCodeHttpResult)response;

        // Assert
        Assert.Equal((int)HttpStatusCode.NoContent, httpResult.StatusCode);
    }

    [Fact]
    public void ToResponse_WithSuccessValue_Should_ReturnOk()
    {
        // Arrange
        var result = Result<string>.Success("test value");

        // Act
        var response = result.ToResponse();
        var httpResult = (IStatusCodeHttpResult)response;

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, httpResult.StatusCode);
    }

    [Fact]
    public void ToResponse_WithCreated_Should_ReturnCreated()
    {
        // Arrange
        var result = Result<string>.Created("new value");

        // Act
        var response = result.ToResponse();
        var httpResult = (IStatusCodeHttpResult)response;

        // Assert
        Assert.Equal((int)HttpStatusCode.Created, httpResult.StatusCode);
    }

    [Fact]
    public void ToResponse_WithNonEmptyEnumerable_Should_ReturnOk()
    {
        // Arrange
        var result = Result<IEnumerable<TodoDto>>.Success([new TodoDto { Id = Ulid.NewUlid(), Title = "Test" }]);

        // Act
        var response = result.ToResponse<TodoDto, TodoResponse>();
        var httpResult = (IStatusCodeHttpResult)response;

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, httpResult.StatusCode);
    }
}
