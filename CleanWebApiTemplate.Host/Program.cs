using CleanWebApiTemplate.Application;
using CleanWebApiTemplate.Domain.Configuration;
using CleanWebApiTemplate.Host;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateSlimBuilder(args);

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? throw new Exception("No environment variable was setted!");
builder.Configuration.SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

if (environment is Constants.DEV_ENVIRONMNET)
    builder.Configuration.AddUserSecrets<Program>();
else
    builder.Configuration.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);

AppSettings appSettings = new();
builder.Configuration.Bind(appSettings);
if (environment is not Constants.DEV_ENVIRONMNET)
    appSettings.ConnectionStrings = new()
    {
        SqlServer = builder.Configuration[Constants.SQLSERVER_CNNSTRING]
        ?? throw new Exception("No connection string recieved!")
    };
if (appSettings.ConnectionStrings is null
    || string.IsNullOrEmpty(appSettings.ConnectionStrings.SqlServer))
    throw new Exception("ConnectionString should have a value!");



// Adding DI Services.
builder.Services.AddHostServices(builder.Configuration,
                                 environment,
                                 appSettings.CorwsAllow,
                                 appSettings.ValidIssuers,
                                 appSettings.ConnectionStrings.SqlServer);
builder.Services.AddApplicationServices();


var app = builder.Build();

app.ConfigureHostApplication(environment);

if (environment is not Constants.PRODUCTION_ENVIRONMENT)
{
    app.MapOpenApi();

    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint("/openapi/v1.json", "OpenAPI v1");
    });
}

app.MapHealthChecks("/health/api", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("api")
});

app.MapHealthChecks("/health/db", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database")
});

app.Run();

public partial class Program { }