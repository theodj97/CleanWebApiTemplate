using CleanWebApiTemplate.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using CleanWebApiTemplate.Infrastructure.Context;
using CleanWebApiTemplate.Testing.Configuration;
using Microsoft.AspNetCore.Authentication;
using CleanWebApiTemplate.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Testcontainers.MariaDb;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Text;

namespace CleanWebApiTemplate.Testing;

public class TestServerFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MariaDbContainer mariaDbServerContainer;
    private static string mariaDbServerConnectionString = string.Empty;
    private readonly TaskCompletionSource<bool> dbSetupCompletionSource = new();
    public HttpClient HttpClient { get; private set; } = null!;
    private IServiceScopeFactory serviceScopeFactory = null!;
    private const string DatabaseName = "TodoDB";
    private readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private string? pathToTestAppSettings;

    public TestServerFixture()
    {
        mariaDbServerContainer = new MariaDbBuilder().WithImage("mariadb:10.10").Build();
        mariaDbServerContainer.Started += async (sender, _) =>
        {
            if (sender is not MariaDbContainer mariaDbContainer)
                throw new InvalidOperationException("Sender is not a MariaDbContainer.");

            await InitDatabase(mariaDbContainer.GetConnectionString());
        };
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Constants.TEST_ENVIRONMENT);
        builder.UseEnvironment(Constants.TEST_ENVIRONMENT);
        CreateJsonTestFile();
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var authDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAuthenticationService));
            if (authDescriptor is not null) services.Remove(authDescriptor);
            services.AddAuthentication(TestAuthHandler.SchemeName).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);

            var serviceProvider = services.BuildServiceProvider();
            serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        });
    }

    public async Task InitializeAsync()
    {
        await mariaDbServerContainer.StartAsync();
        await dbSetupCompletionSource.Task;

        HttpClient = CreateClient();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await mariaDbServerContainer.DisposeAsync();

        if (!string.IsNullOrEmpty(pathToTestAppSettings) && File.Exists(pathToTestAppSettings))
        {
            try
            {
                File.Delete(pathToTestAppSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting {pathToTestAppSettings}: {ex.Message}");
            }
        }
    }

    private void CreateJsonTestFile()
    {
        var appSettings = new AppSettings
        {
            ConnectionStrings = new() { MariaDb = mariaDbServerConnectionString },
            CorsAllow = ["*"],
            ValidIssuers = ["localhost"]
        };

        var appSettingsJson = JsonSerializer.Serialize(appSettings, jsonOptions);

        pathToTestAppSettings = Path.Combine(AppContext.BaseDirectory, $"appsettings.{Constants.TEST_ENVIRONMENT}.json");
        if (File.Exists(pathToTestAppSettings))
            File.Delete(pathToTestAppSettings);

        File.WriteAllText(pathToTestAppSettings, appSettingsJson);
    }

    private async Task InitDatabase(string mariaDbConnectionStr)
    {
        try
        {
            mariaDbServerConnectionString = mariaDbConnectionStr.Replace("Database=master", $"Database={DatabaseName}");

            var options = new DbContextOptionsBuilder<MariaDbContext>()
                .UseMySql(mariaDbServerConnectionString, new MariaDbServerVersion(new Version(10, 10, 0)))
                .EnableDetailedErrors()
                .EnableSensitiveDataLogging()
                .Options;

            await using var context = new MariaDbContext(options);

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
                await context.Database.MigrateAsync();
            else
                await context.Database.EnsureCreatedAsync();

            dbSetupCompletionSource.SetResult(true);
        }
        catch (Exception ex)
        {
            dbSetupCompletionSource.SetException(ex);
            throw;
        }
    }

    internal static async Task ResetDatabaseAsync()
    {
        await using var connection = new MySqlConnection(mariaDbServerConnectionString);
        await connection.OpenAsync();

        await using (var disableFkCommand = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection))
            await disableFkCommand.ExecuteNonQueryAsync();

        var tableNames = new List<string>();
        const string getTablesSql = "SELECT table_name FROM information_schema.tables WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE';";

        await using (var getTablesCommand = new MySqlCommand(getTablesSql, connection))
        await using (var reader = await getTablesCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                tableNames.Add(reader.GetString(0));
        }

        if (tableNames.Count > 0)
        {
            var truncateScript = new StringBuilder();
            foreach (var table in tableNames)
                truncateScript.Append($"TRUNCATE TABLE `{table}`; ");

            await using var truncateCommand = new MySqlCommand(truncateScript.ToString(), connection);
            await truncateCommand.ExecuteNonQueryAsync();
        }

        await using var enableFkCommand = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection);
        await enableFkCommand.ExecuteNonQueryAsync();
    }

    public async Task ExecuteDbContextAsync(Func<MariaDbContext, Task> function) =>
        await ExecuteScopeAsync(sp => function(sp.GetRequiredService<MariaDbContext>()));

    private async Task ExecuteScopeAsync(Func<IServiceProvider, Task> function)
    {
        using var scope = serviceScopeFactory.CreateScope();
        await function(scope.ServiceProvider);
    }
}
