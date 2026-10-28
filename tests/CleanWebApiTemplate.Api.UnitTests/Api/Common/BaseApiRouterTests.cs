using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using CleanWebApiTemplate.Api.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CleanWebApiTemplate.Api.UnitTests.Api.Common;

/// <summary>Route naming and the identity values every router exposes to its handlers.</summary>
public class BaseApiRouterTests
{
    [Fact]
    public void Constructor_WithNameEndingInRoutes_StripsTheSuffix()
    {
        // Act
        var router = new TodoRoutes(new HttpContextAccessor());

        // Assert
        Assert.Equal("Todo", router.RouteName);
    }

    [Fact]
    public void Constructor_WithInvalidName_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new NotARouter(new HttpContextAccessor()));
    }

    [Fact]
    public void UserNameUserEmailAndRole_ReadTheCurrentPrincipalClaims()
    {
        // Arrange
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Name, "theo"),
                    new Claim(ClaimTypes.Email, "theo@example.com"),
                    new Claim(ClaimTypes.Role, "User"),
                ], authenticationType: "Test")),
            },
        };
        var router = new TodoRoutes(accessor);

        // Act
        var userName = router.UserName;
        var userEmail = router.UserEmail;
        var role = router.Role;
        var isAdmin = router.IsAdmin;
        var ownerScope = router.OwnerScope;

        // Assert
        Assert.Equal("theo", userName);
        Assert.Equal("theo@example.com", userEmail);
        Assert.Equal("User", role);
        Assert.False(isAdmin);
        Assert.Equal("theo@example.com", ownerScope);
    }

    [Fact]
    public void IsAdmin_WithAdminRole_ReturnsTrue()
    {
        // Arrange
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Email, "admin@example.com"),
                    new Claim(ClaimTypes.Role, "Admin"),
                ], authenticationType: "Test")),
            },
        };
        var router = new TodoRoutes(accessor);

        // Act & Assert
        Assert.True(router.IsAdmin);
        Assert.Null(router.OwnerScope);
    }

    // The base constructor sets RouteName; the compiler does not see that through [SetsRequiredMembers].
#pragma warning disable CS8618
    private sealed class TodoRoutes : BaseApiRouter
    {
        [SetsRequiredMembers]
        public TodoRoutes(IHttpContextAccessor accessor) : base(accessor)
        {
        }

        public override void MapGroup(IEndpointRouteBuilder app)
        {
        }
    }

    private sealed class NotARouter : BaseApiRouter
    {
        [SetsRequiredMembers]
        public NotARouter(IHttpContextAccessor accessor) : base(accessor)
        {
        }

        public override void MapGroup(IEndpointRouteBuilder app)
        {
        }
    }
#pragma warning restore CS8618
}
