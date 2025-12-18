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
using Microsoft.EntityFrameworkCore.Diagnostics;
using MySqlConnector;
using System.Text;

namespace CleanWebApiTemplate.Testing;

public class TestServerFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MariaDbContainer MariaDbServerContainer;
    private static string MariaDbServerCnnString = string.Empty;
    private readonly TaskCompletionSource<bool> DBSetupCompletionSource = new();
    public HttpClient HttpClient { get; private set; } = null!;
    private IServiceScopeFactory ServiceScopeFactory { get; set; } = null!;
    private const string DataBaseName = "TodoDB";
    private readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private string? PathToTestAppSettings = null;

    public TestServerFixture()
    {
        MariaDbServerContainer = new MariaDbBuilder().WithImage("mariadb:10.10").Build();
        MariaDbServerContainer.Started += (sender, args) =>
        {
            if (sender is not MariaDbContainer mariaDbContainer)
                throw new Exception("Sender is not an MariaDbContainer.");

            InitDatabase(mariaDbContainer.GetConnectionString()).Wait();
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
            ServiceScopeFactory = serviceProvider.GetService<IServiceScopeFactory>()
            ?? throw new Exception("ServiceScopeFactory not found.");
        });
    }

    public async Task InitializeAsync()
    {
        await MariaDbServerContainer.StartAsync();
        await DBSetupCompletionSource.Task;

        HttpClient = Server.CreateClient();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        MariaDbServerContainer.DisposeAsync().AsTask();
        if (string.IsNullOrEmpty(PathToTestAppSettings) is false && !string.IsNullOrEmpty(PathToTestAppSettings) && File.Exists(PathToTestAppSettings))
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
        return Task.CompletedTask;
    }

    private void CreateJsonTestFile()
    {
        var appSettings = new AppSettings()
        {
            ConnectionStrings = new() { MariaDb = MariaDbServerCnnString },
            CorsAllow = ["*"],
            ValidIssuers = ["localhost"]
        };

        string path = AppContext.BaseDirectory;
        var appSettingsJson = JsonSerializer.Serialize(appSettings, JsonOpts);

        PathToTestAppSettings = Path.Combine(path, $"appsettings.{Constants.TEST_ENVIRONMENT}.json");
        if (File.Exists(PathToTestAppSettings)) File.Delete(PathToTestAppSettings);
        File.WriteAllText(PathToTestAppSettings, appSettingsJson);
    }

    private async Task InitDatabase(string mariaDbConnectionStr)
    {
        MariaDbServerCnnString = mariaDbConnectionStr.Replace("Database=master", $"Database={DataBaseName}");

        var optionsBuilder = new DbContextOptionsBuilder<MariaDbContext>();
        MariaDbServerVersion serverVersion = new(new Version(10, 10, 0));
        optionsBuilder.UseMySql(MariaDbServerCnnString, serverVersion)
                      .EnableDetailedErrors()
                      .EnableSensitiveDataLogging();

        using var context = new MariaDbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();

        DBSetupCompletionSource.SetResult(true);
    }

    internal static async Task ResetDatabaseAsync()
    {
        using MySqlConnection connection = new(MariaDbServerCnnString);
        await connection.OpenAsync();

        using (MySqlCommand disableFkCommand = new("SET FOREIGN_KEY_CHECKS = 0;", connection))
            await disableFkCommand.ExecuteNonQueryAsync();

        var tableNames = new List<string>();
        const string getTablesSql = "SELECT table_name FROM information_schema.tables WHERE table_schema = DATABASE() AND table_type = 'BASE TABLE';";

        using (MySqlCommand getTablesCommand = new(getTablesSql, connection))
        using (var reader = await getTablesCommand.ExecuteReaderAsync())
            while (await reader.ReadAsync())
                tableNames.Add(reader.GetString(0));

        if (tableNames.Count > 0)
        {
            StringBuilder truncateScript = new();
            foreach (var table in tableNames)
                truncateScript.Append($"TRUNCATE TABLE `{table}`; ");


            using MySqlCommand truncateCommand = new(truncateScript.ToString(), connection);
            await truncateCommand.ExecuteNonQueryAsync();
        }

        using MySqlCommand enableFkCommand = new("SET FOREIGN_KEY_CHECKS = 1;", connection);
        await enableFkCommand.ExecuteNonQueryAsync();
    }

    public async Task ExecuteDbContextAsync(Func<MariaDbContext, Task> function) =>
        await ExecuteScopeAsync(sp => function(sp.GetService<MariaDbContext>() ?? throw new InvalidOperationException("No DbContext was provided")));


    private async Task ExecuteScopeAsync(Func<IServiceProvider, Task> function)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        await function(scope.ServiceProvider);
    }
}
