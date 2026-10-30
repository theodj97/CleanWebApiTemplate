using System.Text;
using CleanWebApiTemplate.Domain.Configuration;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>How the API validates JWTs. Local development/tests use a symmetric HS256 key;
/// deployed environments can delegate to an OIDC provider.</summary>
internal enum AuthenticationMode
{
    Symmetric,
    Oidc,
}

/// <summary>
/// Authentication settings resolved from configuration and validated at startup. Keeping this
/// separate from <see cref="AppSettings"/> makes the two modes explicit and prevents mixing an
/// OIDC authority with a symmetric signing key.
/// </summary>
internal sealed record AuthenticationSettings(
    AuthenticationMode Mode,
    string? Authority,
    string Audience,
    string[] ValidIssuers,
    string? ApiKey)
{
    public static AuthenticationSettings Load(IConfiguration configuration, AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(appSettings);

        string? authority = configuration[Constants.AUTHORITY];
        AuthenticationMode mode = ParseMode(configuration[Constants.AUTHENTICATION_MODE], authority);

        return mode switch
        {
            AuthenticationMode.Symmetric => CreateSymmetric(configuration, appSettings, authority),
            AuthenticationMode.Oidc => CreateOidc(appSettings, authority),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported authentication mode."),
        };
    }

    private static AuthenticationMode ParseMode(string? mode, string? authority)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            // Backwards-compatible inference: an authority means OIDC, no authority means the
            // local symmetric key. A missing mode is never ambiguous once the authority is read.
            return string.IsNullOrWhiteSpace(authority)
                ? AuthenticationMode.Symmetric
                : AuthenticationMode.Oidc;
        }

        return mode.Trim().ToLowerInvariant() switch
        {
            "symmetric" or "local" => AuthenticationMode.Symmetric,
            "oidc" => AuthenticationMode.Oidc,
            _ => throw new InvalidOperationException(
                $"'{Constants.AUTHENTICATION_MODE}' must be 'Symmetric' or 'Oidc'."),
        };
    }

    private static AuthenticationSettings CreateSymmetric(IConfiguration configuration,
                                                          AppSettings appSettings,
                                                          string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority) is false)
        {
            throw new InvalidOperationException(
                $"'{Constants.AUTHORITY}' must be empty when '{Constants.AUTHENTICATION_MODE}' is 'Symmetric'.");
        }

        string? apiKey = configuration[Constants.API_KEY];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                $"'{Constants.API_KEY}' is required in Symmetric mode.");
        }

        if (Encoding.UTF8.GetByteCount(apiKey) < 32)
        {
            throw new InvalidOperationException(
                $"'{Constants.API_KEY}' must be at least 32 UTF-8 bytes in Symmetric mode.");
        }

        return new AuthenticationSettings(AuthenticationMode.Symmetric,
                                          Authority: null,
                                          appSettings.Audience,
                                          appSettings.ValidIssuers,
                                          apiKey);
    }

    private static AuthenticationSettings CreateOidc(AppSettings appSettings, string? authority)
    {
        if (string.IsNullOrWhiteSpace(authority))
        {
            throw new InvalidOperationException(
                $"'{Constants.AUTHORITY}' is required in Oidc mode.");
        }

        if (Uri.TryCreate(authority, UriKind.Absolute, out Uri? uri) is false ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                $"'{Constants.AUTHORITY}' must be an absolute HTTPS URI in Oidc mode.");
        }

        // In OIDC mode the signing keys come from the provider metadata. A stray API_KEY is
        // intentionally ignored rather than mixed with the asymmetric keys.
        return new AuthenticationSettings(AuthenticationMode.Oidc,
                                          authority,
                                          appSettings.Audience,
                                          appSettings.ValidIssuers,
                                          ApiKey: null);
    }
}
