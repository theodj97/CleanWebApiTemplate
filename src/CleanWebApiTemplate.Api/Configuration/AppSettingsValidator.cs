using Microsoft.Extensions.Options;

namespace CleanWebApiTemplate.Api.Configuration;

public class AppSettingsValidator : IValidateOptions<AppSettings>
{
    public ValidateOptionsResult Validate(string? name, AppSettings options)
    {
        var failures = new List<string>();

        if (options.ConnectionStrings is null)
            failures.Add("ConnectionStrings section is required.");
        else if (string.IsNullOrWhiteSpace(options.ConnectionStrings.Postgres))
            failures.Add("ConnectionStrings.Postgres is required.");

        if (options.ValidIssuers is null || options.ValidIssuers.Length == 0)
        {
            failures.Add("ValidIssuers must contain at least one entry.");
        }
        else
        {
            foreach (string issuer in options.ValidIssuers)
            {
                if (string.IsNullOrWhiteSpace(issuer))
                {
                    failures.Add("ValidIssuers cannot contain empty entries.");
                    continue;
                }

                if (Uri.TryCreate(issuer, UriKind.Absolute, out Uri? uri) is false ||
                    (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != "urn"))
                {
                    failures.Add($"ValidIssuer '{issuer}' must be an absolute HTTPS or URN URI.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("Audience is required.");

        return failures.Count != 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
