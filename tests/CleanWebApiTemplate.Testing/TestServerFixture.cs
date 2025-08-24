using CleanWebApiTemplate.Host;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using CleanWebApiTemplate.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CleanWebApiTemplate.Testing.Configuration;
using Microsoft.AspNetCore.Authentication;
using CleanWebApiTemplate.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace CleanWebApiTemplate.Testing;

public class TestServerFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer SqlServerContainer;
    private static string SqlServerCnnString = string.Empty;
    private readonly TaskCompletionSource<bool> DBSetupCompletionSource = new();
    public HttpClient HttpClient { get; private set; } = null!;
    private IServiceScopeFactory ServiceScopeFactory { get; set; } = null!;
    private const string DataBaseName = "Todo";
    private readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private string? PathToTestAppSettings = null;

    public TestServerFixture()
    {
        SqlServerContainer = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:latest").Build();
        SqlServerContainer.Started += (sender, args) =>
        {
            if (sender is not MsSqlContainer sqlContainer)
                throw new Exception("Sender is not an MsSqlContainer.");

            InitDatabase(sqlContainer.GetConnectionString()).Wait();
        };
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", Constants.TEST_ENVIRONMNET);
        builder.UseEnvironment(Constants.TEST_ENVIRONMNET);
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
        await SqlServerContainer.StartAsync();
        await DBSetupCompletionSource.Task;

        HttpClient = Server.CreateClient();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        SqlServerContainer.DisposeAsync().AsTask();
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
            ConnectionStrings = new() { SqlServer = SqlServerCnnString },
            CorsAllow = ["*"],
            ValidIssuers = ["localhost"]
        };

        string path = AppContext.BaseDirectory;
        var appSettingsJson = JsonSerializer.Serialize(appSettings, JsonOpts);

        PathToTestAppSettings = Path.Combine(path, $"appsettings.{Constants.TEST_ENVIRONMNET}.json");
        if (File.Exists(PathToTestAppSettings)) File.Delete(PathToTestAppSettings);
        File.WriteAllText(PathToTestAppSettings, appSettingsJson);
    }

    private async Task InitDatabase(string sqlConnectionStr)
    {
        SqlServerCnnString = sqlConnectionStr.Replace("Database=master", $"Database={DataBaseName}");

        var optionsBuilder = new DbContextOptionsBuilder<SqlDbContext>();
        optionsBuilder.UseSqlServer(SqlServerCnnString);
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

        using var context = new SqlDbContext(optionsBuilder.Options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();

        DBSetupCompletionSource.SetResult(true);
    }

    internal static async Task ResetDatabaseAsync()
    {
        using SqlConnection connection = new(SqlServerCnnString);
        await connection.OpenAsync();

        // Disable all constraints
        using SqlCommand removeCnstaitCommand = new(
            "EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'",
            connection);
        await removeCnstaitCommand.ExecuteNonQueryAsync();

        // Drop all tables data
        using SqlCommand dropTablesDataCommand = new(
            "EXEC sp_msforeachtable 'TRUNCATE TABLE ?'",
            connection);
        await dropTablesDataCommand.ExecuteNonQueryAsync();

        // Re-enable constraints
        using SqlCommand reEnableCnstaitCommand = new(
            "EXEC sp_msforeachtable 'ALTER TABLE ? CHECK CONSTRAINT ALL'",
            connection);
        await reEnableCnstaitCommand.ExecuteNonQueryAsync();
    }

    public async Task ExecuteDbContextAsync(Func<SqlDbContext, Task> function) =>
        await ExecuteScopeAsync(sp => function(sp.GetService<SqlDbContext>() ?? throw new InvalidOperationException("No DbContext was provided")));


    private async Task ExecuteScopeAsync(Func<IServiceProvider, Task> function)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        await function(scope.ServiceProvider);
    }
}
