using System.Security.Claims;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Api.Models.Interfaces;

namespace CleanWebApiTemplate.Api.Common;

public abstract class BaseApiRouter : IGroupMap
{
    private readonly IHttpContextAccessor contextAccessor;
    public string UserName => contextAccessor.HttpContext!.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
    public string UserEmail => contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value
        ?? contextAccessor.HttpContext?.User.FindFirst("email")?.Value
        ?? throw new InvalidOperationException("The authenticated principal has no email claim.");
    public string Role => contextAccessor.HttpContext!.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
    public bool IsAdmin => contextAccessor.HttpContext!.User.IsInRole(Constants.ADMIN_POLICY);

    /// <summary>
    /// Owner email that non-administrators are restricted to. Administrators get
    /// <see langword="null"/> because they may access any TODO.
    /// </summary>
    public string? OwnerScope => IsAdmin ? null : UserEmail;
    public required string RouteName { get; init; }
    private const string ROUTE_TERMINATION_NAME = "Routes";

    public BaseApiRouter(IHttpContextAccessor contextAccessor)
    {
        this.contextAccessor = contextAccessor;
        var className = GetType().Name;

        RouteName = className.EndsWith(ROUTE_TERMINATION_NAME)
            ? className[..^ROUTE_TERMINATION_NAME.Length]
            : throw new ArgumentException($"Route name doesn't match syntax finishing in {ROUTE_TERMINATION_NAME}");
    }

    public abstract void MapGroup(IEndpointRouteBuilder app);
}
