using System.Security.Claims;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Application.Abstractions;

namespace CleanWebApiTemplate.Api.Common;

/// <summary>Reads the current user from the ASP.NET request and exposes only what Application needs.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor contextAccessor) : ICurrentUser
{
    public string Email => contextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value
        ?? contextAccessor.HttpContext?.User.FindFirst("email")?.Value
        ?? throw new InvalidOperationException("The authenticated principal has no email claim.");

    public bool IsAdmin => contextAccessor.HttpContext?.User.IsInRole(Constants.ADMIN_POLICY) ?? false;

    public string? OwnerScope => IsAdmin ? null : Email;
}
