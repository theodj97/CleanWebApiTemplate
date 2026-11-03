namespace CleanWebApiTemplate.Application.Abstractions;

/// <summary>The authenticated caller as the application sees it, without ASP.NET types.</summary>
public interface ICurrentUser
{
    string Email { get; }
    bool IsAdmin { get; }
    string? OwnerScope { get; }
}
