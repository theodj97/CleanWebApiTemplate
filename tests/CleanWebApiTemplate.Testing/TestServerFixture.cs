using CleanWebApiTemplate.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using CleanWebApiTemplate.Infrastructure.Data;
using CleanWebApiTemplate.Testing.Configuration;
using Microsoft.AspNetCore.Authentication;
using CleanWebApiTemplate.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using CleanWebApiTemplate.Infrastructure.Repositories.Interfaces;

namespace CleanWebApiTemplate.Testing;

public class TestServerFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DataBaseName = "Todo";
    private static readonly string DbFilePath = Path.Combine(Path.GetTempPath(), $"{DataBaseName}_{Guid.NewGuid():N}.db");
    private static readonly string SqliteCnnString = $"Data Source={DbFilePath}";
    public HttpClient HttpClient { get; private set; } = null!;
    private IServiceScopeFactory ServiceScopeFactory { get; set; } = null!;
    private readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private string? PathToTestAppSettings = null;

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
            ServiceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>()
            ?? throw new Exception("ServiceScopeFactory not found.");
        });
    }

    public async ValueTask InitializeAsync()
    {
        await InitDatabase(SqliteCnnString);

        HttpClient = Server.CreateClient();
    }

    public override async ValueTask DisposeAsync()
    {
        if (string.IsNullOrEmpty(PathToTestAppSettings) is false &&
            !string.IsNullOrEmpty(PathToTestAppSettings) &&
            File.Exists(PathToTestAppSettings))
        {
            try
            {
                File.Delete(PathToTestAppSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting {PathToTestAppSettings}", ex);
            }
        }

        try
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(DbFilePath)) File.Delete(DbFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting {DbFilePath}", ex);
        }

        GC.SuppressFinalize(this);
        await base.DisposeAsync();
    }

    private void CreateJsonTestFile()
    {
        var appSettings = new AppSettings()
        {
            ConnectionStrings = new() { Sqlite = SqliteCnnString },
            CorsAllow = ["*"],
            ValidIssuers = ["localhost"]
        };

        string path = AppContext.BaseDirectory;
        var appSettingsJson = JsonSerializer.Serialize(appSettings, JsonOpts);

        PathToTestAppSettings = Path.Combine(path, $"appsettings.{Constants.TEST_ENVIRONMENT}.json");
        if (File.Exists(PathToTestAppSettings)) File.Delete(PathToTestAppSettings);
        File.WriteAllText(PathToTestAppSettings, appSettingsJson);
    }

    private static async Task InitDatabase(string sqliteConnectionStr) =>
        await SqliteDatabaseInitializer.InitializeAsync(sqliteConnectionStr);

    internal static async Task ResetDatabaseAsync()
    {
        using SqliteConnection connection = new(SqliteCnnString);
        await connection.OpenAsync();

        // Disable foreign key constraints
        using SqliteCommand disableFkCommand = new("PRAGMA foreign_keys = OFF;", connection);
        await disableFkCommand.ExecuteNonQueryAsync();

        // Get all table names
        List<string> tables = [];
        using (SqliteCommand getTablesCommand = new(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%' AND name <> '__EFMigrationsHistory';",
            connection))
        using (var reader = await getTablesCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                tables.Add(reader.GetString(0));
        }

        // Drop all tables data
        foreach (var table in tables)
        {
            using SqliteCommand dropTablesDataCommand = new($"DELETE FROM \"{table}\";", connection);
            await dropTablesDataCommand.ExecuteNonQueryAsync();
        }

        // Re-enable foreign key constraints
        using SqliteCommand enableFkCommand = new("PRAGMA foreign_keys = ON;", connection);
        await enableFkCommand.ExecuteNonQueryAsync();
    }

    public async Task ExecuteRepositoryAsync(Func<ITodoRepository, Task> function) =>
        await ExecuteScopeAsync(sp => function(sp.GetService<ITodoRepository>() ?? throw new InvalidOperationException("No ITodoRepository was provided")));


    private async Task ExecuteScopeAsync(Func<IServiceProvider, Task> function)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        await function(scope.ServiceProvider);
    }
}
