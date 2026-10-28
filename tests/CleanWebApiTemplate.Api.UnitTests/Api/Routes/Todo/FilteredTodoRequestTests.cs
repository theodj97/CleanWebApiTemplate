using CleanWebApiTemplate.Api.Routes.Todo.Filter;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Routes.Todo;

/// <summary>
/// The owner scope is decided by the API from the authenticated identity, never by the
/// request body when the caller is not an administrator.
/// </summary>
public class FilteredTodoRequestTests
{
    [Fact]
    public void ToInput_AsNonAdmin_ForcesTheAuthenticatedUserEmail()
    {
        // Arrange
        var request = new FilteredTodoRequest
        {
            Title = ["a title"],
            CreatedBy = ["other@example.com"],
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var input = request.ToInput(ownerScope: "current@example.com");

        // Assert
        Assert.Equal("current@example.com", Assert.Single(input.CreatedBy!));
        Assert.Equal(new[] { "a title" }, input.Title);
        Assert.Equal((byte)1, input.PageNumber);
        Assert.Equal((byte)10, input.PageSize);
    }

    [Fact]
    public void ToInput_AsAdmin_UsesEveryCreatedByFromTheRequest()
    {
        // Arrange
        var request = new FilteredTodoRequest
        {
            CreatedBy = ["first@example.com", "second@example.com"],
        };

        // Act
        var input = request.ToInput(ownerScope: null);

        // Assert
        Assert.Equal(new[] { "first@example.com", "second@example.com" }, input.CreatedBy);
    }

    [Fact]
    public void ToInput_AsAdminWithoutCreatedBy_DoesNotRestrictTheOwner()
    {
        // Act
        var input = new FilteredTodoRequest().ToInput(ownerScope: null);

        // Assert
        Assert.Null(input.CreatedBy);
    }
}
