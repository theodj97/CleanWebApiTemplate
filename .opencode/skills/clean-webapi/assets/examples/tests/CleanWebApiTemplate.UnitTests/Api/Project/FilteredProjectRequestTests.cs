using CleanWebApiTemplate.Api.Project.Filter;
using CleanWebApiTemplate.Application.Common.Identity;

namespace CleanWebApiTemplate.UnitTests.Api.Project;

/// <summary>
/// The owner scope is decided by the API from the authenticated identity, never by the
/// request body when the caller is not an administrator.
/// </summary>
public class FilteredProjectRequestTests
{
    [Fact]
    public void ToInput_AsNonAdmin_ForcesTheAuthenticatedUserEmail()
    {
        // Arrange
        var request = new FilteredProjectRequest
        {
            Names = ["a name"],
            Owners = ["other@example.com"],
            PageNumber = 1,
            PageSize = 10,
        };

        // Act
        var input = request.ToInput(new OwnerScope("current@example.com", IsAdmin: false));

        // Assert
        Assert.Equal("current@example.com", Assert.Single(input.Owners!));
        Assert.Equal(new[] { "a name" }, input.Names);
        Assert.Equal((byte)1, input.PageNumber);
        Assert.Equal((byte)10, input.PageSize);
    }

    [Fact]
    public void ToInput_AsAdmin_UsesEveryOwnerFromTheRequest()
    {
        // Arrange
        var request = new FilteredProjectRequest
        {
            Owners = ["first@example.com", "second@example.com"],
        };

        // Act
        var input = request.ToInput(new OwnerScope("admin@example.com", IsAdmin: true));

        // Assert
        Assert.Equal(new[] { "first@example.com", "second@example.com" }, input.Owners);
    }

    [Fact]
    public void ToInput_AsAdminWithoutOwners_DoesNotRestrictTheOwner()
    {
        // Act
        var input = new FilteredProjectRequest().ToInput(new OwnerScope("admin@example.com", IsAdmin: true));

        // Assert
        Assert.Null(input.Owners);
    }
}
