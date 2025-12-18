using CleanWebApiTemplate.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace CleanWebApiTemplate.Host.Configuration;

public class AppSettingsValidator : IValidateOptions<AppSettings>
{
    public ValidateOptionsResult Validate(string? name, AppSettings options)
    {
        var failures = new List<string>();

        if (options.ConnectionStrings is null)
            failures.Add($"{nameof(options.ConnectionStrings)} section is required.");
        else
            if (string.IsNullOrWhiteSpace(options.ConnectionStrings.MariaDb))
                failures.Add($"{nameof(options.ConnectionStrings.MariaDb)} is required.");

        if (options.CorsAllow is null || options.CorsAllow.Length == 0)
            failures.Add($"{nameof(options.CorsAllow)} must contain at least one entry.");

        if (options.ValidIssuers is null || options.ValidIssuers.Length == 0)
            failures.Add($"{nameof(options.ValidIssuers)} must contain at least one entry.");

        return failures.Count != 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
