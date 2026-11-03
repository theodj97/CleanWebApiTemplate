using System.Security.Claims;
using CleanWebApiTemplate.Api.Common;
using CleanWebApiTemplate.Api.Configuration;
using Microsoft.AspNetCore.Http;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Common;

/// <summary>The API adapter reads claims once and exposes only the values Application needs.</summary>
public class HttpContextCurrentUserTests
{
    [Fact]
    public void Email_WithMappedEmailClaim_ReturnsTheEmail()
    {
        // Arrange
        var currentUser = CreateCurrentUser(new Claim(ClaimTypes.Email, "user@example.com"));

        // Act & Assert
        Assert.Equal("user@example.com", currentUser.Email);
    }

    [Fact]
    public void Email_WithShortEmailClaim_ReturnsTheEmail()
    {
        // Arrange
        var currentUser = CreateCurrentUser(new Claim("email", "user@example.com"));

        // Act & Assert
        Assert.Equal("user@example.com", currentUser.Email);
    }

    [Fact]
    public void Email_WithoutEmailClaim_Throws()
    {
        // Arrange
        var currentUser = CreateCurrentUser(new Claim(ClaimTypes.Name, "user"));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => currentUser.Email);
    }

    [Fact]
    public void Owner_AsAdmin_HasNoVisibleOwner()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            new Claim(ClaimTypes.Email, "admin@example.com"),
            new Claim(ClaimTypes.Role, Constants.ADMIN_POLICY));

        // Act & Assert
        Assert.True(currentUser.Owner.IsAdmin);
        Assert.Null(currentUser.Owner.VisibleOwner);
    }

    [Fact]
    public void Owner_AsUser_IsPinnedToTheEmail()
    {
        // Arrange
        var currentUser = CreateCurrentUser(
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.Role, Constants.USER_POLICY));

        // Act & Assert
        Assert.False(currentUser.Owner.IsAdmin);
        Assert.Equal("user@example.com", currentUser.Owner.VisibleOwner);
    }

    private static HttpContextCurrentUser CreateCurrentUser(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };

        return new HttpContextCurrentUser(accessor);
    }
}
