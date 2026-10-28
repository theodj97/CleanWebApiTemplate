using System.Net;
using CleanWebApiTemplate.OpenApiExamples;

var builder = WebApplication.CreateBuilder(args);

var apiBaseUrl = RequireAbsoluteUri("API_BASE_URL");
var examplesPath = RequireExistingDirectory("EXAMPLES_PATH");

var catalog = new ExamplesCatalog(examplesPath);
if (!File.Exists(catalog.CatalogPath))
    throw new InvalidOperationException($"The examples catalog '{catalog.CatalogPath}' does not exist.");

builder.Services.AddSingleton(catalog);

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
    "Local OpenAPI examples: API {ApiBaseUrl}, catalog {CatalogPath}.",
    apiBaseUrl,
    catalog.CatalogPath);

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "CleanWebApiTemplate — local examples");
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
