using System.Net;
using System.Text.Json.Nodes;
using CleanWebApiTemplate.AppHost;
using CleanWebApiTemplate.OpenApiExamples;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = RequireAbsoluteUri("API_BASE_URL");
var examplesPath = RequireExistingDirectory("EXAMPLES_PATH");

var catalog = new ExamplesCatalog(examplesPath);
if (!File.Exists(catalog.CatalogPath))
    throw new InvalidOperationException($"The examples catalog '{catalog.CatalogPath}' does not exist.");

var localAuth = new LocalAuth(
    RequireNonEmpty(AppHostContract.LocalLogin.SigningKey),
    RequireNonEmpty(AppHostContract.LocalLogin.Issuer),
    RequireNonEmpty(AppHostContract.LocalLogin.Audience),
    RequireNonEmpty(AppHostContract.LocalLogin.RoleClaim),
    RequireLifetime(AppHostContract.LocalLogin.LifetimeMinutes));

var users = LocalUsers.Load(Path.Combine(catalog.BasePath, "users.json"));

builder.Services.AddSingleton(catalog);
builder.Services.AddSingleton(localAuth);
builder.Services.AddSingleton(users);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LocalTokenService>();

builder.Services.AddHttpClient<OpenApiDocumentService>(client =>
{
    client.BaseAddress = apiBaseUrl;
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AutomaticDecompression = DecompressionMethods.All,
});

builder.Services.AddHttpClient<ApiProxy>(client =>
{
    client.BaseAddress = apiBaseUrl;
    client.Timeout = TimeSpan.FromSeconds(30);
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false,
});

var app = builder.Build();

app.Logger.LogInformation(
    "Local OpenAPI examples: API {ApiBaseUrl}, catalog {CatalogPath}, users {UserCount}.",
    apiBaseUrl,
    catalog.CatalogPath,
    users.Users.Count);

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "CleanWebApiTemplate — local examples");
    options.IndexStream = () => SwaggerIndex.Create(catalog.BasePath, users);
});

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok());

app.MapGet("/openapi/v1.json", async (
    HttpContext context,
    OpenApiDocumentService service,
    CancellationToken cancellationToken) =>
{
    context.Response.Headers["Cache-Control"] = "no-store";

    try
    {
        var document = await service.GetDocumentAsync(cancellationToken);
        return Results.Text(document.ToJsonString(), "application/json");
    }
    catch (OpenApiExamplesException exception)
    {
        return Results.Text(
            OpenApiExamplesError.ToJson(exception.Problems),
            "application/json",
            statusCode: StatusCodes.Status500InternalServerError);
    }
    catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return Results.Text(
            OpenApiExamplesError.ToJson(["The API did not respond within 10 seconds."]),
            "application/json",
            statusCode: StatusCodes.Status504GatewayTimeout);
    }
    catch (HttpRequestException exception)
    {
        app.Logger.LogError(exception, "Could not read the OpenAPI document from {ApiBaseUrl}.", apiBaseUrl);
        return Results.Text(
            OpenApiExamplesError.ToJson([exception.Message]),
            "application/json",
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapPost("/local/auth/token/{profile}", (
    string profile,
    LocalUsers localUsers,
    LocalTokenService tokens) =>
{
    if (!localUsers.Users.TryGetValue(profile, out var user))
    {
        var profiles = new JsonArray();
        foreach (var name in localUsers.Users.Keys.Order(StringComparer.Ordinal))
        {
            JsonNode? value = JsonValue.Create(name);
            profiles.Add(value);
        }

        var error = new JsonObject
        {
            ["error"] = $"Unknown profile '{profile}'.",
            ["profiles"] = profiles,
        };

        return Results.Text(error.ToJsonString(), "application/json", statusCode: StatusCodes.Status404NotFound);
    }

    var token = tokens.Create(user);
    var response = new JsonObject
    {
        ["token"] = token.Token,
        ["tokenType"] = "Bearer",
        ["expiresAt"] = token.ExpiresAt.ToString("O"),
        ["expiresIn"] = token.ExpiresIn,
        ["profile"] = token.Profile,
        ["email"] = token.Email,
    };

    var roles = new JsonArray();
    foreach (var role in token.Roles)
    {
        JsonNode? value = JsonValue.Create(role);
        roles.Add(value);
    }

    response["roles"] = roles;
    return Results.Text(response.ToJsonString(), "application/json");
});

app.Map("/api/{**path}", async (HttpContext context) =>
{
    var proxy = context.RequestServices.GetRequiredService<ApiProxy>();
    await proxy.ForwardAsync(context);
});

app.Run();

static Uri RequireAbsoluteUri(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value)
        || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
    {
        throw new InvalidOperationException($"Environment variable '{name}' must be an absolute HTTP(S) URI.");
    }

    return uri;
}

static string RequireExistingDirectory(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value) || !Directory.Exists(value))
        throw new InvalidOperationException($"Environment variable '{name}' must be an existing directory.");

    return Path.GetFullPath(value);
}

static string RequireNonEmpty(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
        throw new InvalidOperationException($"Environment variable '{name}' is required.");

    return value;
}

static int RequireLifetime(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrWhiteSpace(value))
        return 60;

    return int.TryParse(value, out var minutes) && minutes is >= 1 and <= 1440
        ? minutes
        : throw new InvalidOperationException($"Environment variable '{name}' must be between 1 and 1440 minutes.");
}
