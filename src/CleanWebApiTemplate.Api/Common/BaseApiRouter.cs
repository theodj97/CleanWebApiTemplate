using System.Security.Claims;
using CleanWebApiTemplate.Api.Models.Interfaces;

namespace CleanWebApiTemplate.Api.Common;

public abstract class BaseApiRouter : IGroupMap
{
    private readonly IHttpContextAccessor contextAccessor;
    public string UserName => contextAccessor.HttpContext!.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
    public string UserEmail => contextAccessor.HttpContext!.User.Claims.First(x => x.Type == ClaimTypes.Email).Value;
    public string Role => contextAccessor.HttpContext!.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
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
