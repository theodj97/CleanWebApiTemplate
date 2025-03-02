using MediatR;
using System.Security.Claims;

namespace CleanWebApiTemplate.Host.Common;

public class BaseApiRouter
{
    private readonly IHttpContextAccessor contextAccessor;
    public string UserName => contextAccessor.HttpContext!.User.Claims.First(x => x.Type == ClaimTypes.Name).Value;
    public string Role => contextAccessor.HttpContext!.User.Claims.First(x => x.Type == ClaimTypes.Role).Value;
    public string RouteName { get; private set; }
    private const string ROUTE_TERMINATION_NAME = "Routes";
    protected IMediator Mediator => contextAccessor.HttpContext!.RequestServices.GetRequiredService<IMediator>()
        ?? throw new Exception("Unable finding IMediator service");

    public BaseApiRouter(IHttpContextAccessor contextAccessor)
    {
        this.contextAccessor = contextAccessor;

        var className = GetType().Name;

        RouteName = className.EndsWith(ROUTE_TERMINATION_NAME)
            ? className[..^ROUTE_TERMINATION_NAME.Length]
            : throw new ArgumentException($"Route name doesn't match syntax finishing in {ROUTE_TERMINATION_NAME}");
    }
}
