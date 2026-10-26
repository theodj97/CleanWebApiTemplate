using CleanWebApiTemplate.Domain.Configuration;
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
            failures.Add("ValidIssuers must contain at least one entry.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("Audience is required.");

        return failures.Count != 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
