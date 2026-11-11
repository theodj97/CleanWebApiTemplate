using CleanWebApiTemplate.Infrastructure.Secrets;

namespace CleanWebApiTemplate.OpenBaoInitializer;

/// <summary>
/// Local-only one-shot job: writes the application's secrets into OpenBao on every AppHost start. The server runs in
/// dev mode (in memory), so the store is empty when this job starts and everything it writes disappears with the
/// container. Exits 0 once the secrets are written, so the application can start; non-zero leaves it waiting.
/// </summary>
public static class Program
{
    /// <summary>The API's signing key, as the API reads it (<c>Api.Configuration.Constants.ApiKey</c>).</summary>
    private const string ApiKey = "API_KEY";

    /// <summary>The connection string key, as the API reads it (<c>Api.Configuration.ConnectionStringsSection</c>).</summary>
    private const string PostgresConnectionString = "ConnectionStrings:Postgres";

    /// <summary>How the AppHost passes the connection string to this job (configuration key with <c>__</c>).</summary>
    private const string PostgresConnectionStringEnvironmentVariable = "ConnectionStrings__Postgres";

    public static async Task<int> Main()
    {
        var address = Environment.GetEnvironmentVariable("OpenBao__Address");
        var token = Environment.GetEnvironmentVariable("OpenBao__Token");
        var mount = Environment.GetEnvironmentVariable("OpenBao__Mount");
        var secretPath = Environment.GetEnvironmentVariable("OpenBao__Path");
        var apiKey = Environment.GetEnvironmentVariable(ApiKey);
        var postgres = Environment.GetEnvironmentVariable(PostgresConnectionStringEnvironmentVariable);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(address)) missing.Add("OpenBao__Address");
        if (string.IsNullOrWhiteSpace(token)) missing.Add("OpenBao__Token");
        if (string.IsNullOrWhiteSpace(secretPath)) missing.Add("OpenBao__Path");
        if (string.IsNullOrWhiteSpace(apiKey)) missing.Add(ApiKey);
        if (string.IsNullOrWhiteSpace(postgres)) missing.Add(PostgresConnectionStringEnvironmentVariable);

        if (missing.Count is not 0)
        {
            Console.Error.WriteLine($"Missing configuration: {string.Join(", ", missing)}.");
            return 1;
        }

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var client = new OpenBaoClient(http, new OpenBaoOptions
            {
                Address = address!,
                Token = token!,
                MountPath = string.IsNullOrWhiteSpace(mount) ? OpenBaoConfigurationKeys.DefaultMount : mount,
            });

            await client.WriteAsync(secretPath!, new Dictionary<string, string>
            {
                [ApiKey] = apiKey!,
                [PostgresConnectionString] = postgres!,
            });

            Console.WriteLine($"OpenBao ready: {ApiKey} and {PostgresConnectionString} written to '{secretPath}'.");
            return 0;
        }
        catch (Exception exception) when (exception is OpenBaoException or HttpRequestException or TaskCanceledException)
        {
            Console.Error.WriteLine($"OpenBao initialization failed: {exception.Message}");
            return 1;
        }
    }
}
