#!/usr/bin/env dotnet
// devtoken: locally signed JWTs for local development. No network, no identity provider, no login endpoint.
//
//   dotnet local/tools/devtoken.cs init [--rotate]                 create the local signing key (once per machine)
//   dotnet local/tools/devtoken.cs list                            list the profiles defined in devtoken.json
//   dotnet local/tools/devtoken.cs get --profile <name> [--lifetime <minutes>]
//   dotnet local/tools/devtoken.cs dotenv <file> [--lifetime <minutes>]   upsert DEVTOKEN_<PROFILE> for every profile
//                                                                          into a git-ignored .env (e.g. Bruno's)
//
// Settings (issuer, audiences, claim names, profiles) come from devtoken.json next to this file. Keys live in the
// AppHost user secrets, outside the repository; the AppHost passes the validation material to the applications.
// The token is the only thing written to stdout; diagnostics go to stderr and never include a token or a key.
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;

const string SigningKeyParameter = "Parameters:local-jwt-signing-key"; // HS256 secret (text)
const string PublicKeyParameter = "Parameters:local-jwt-public-key";   // RS256 public key (PEM), read by the AppHost
const string PrivateKeySecret = "LocalAuth:PrivateKeyPem";             // RS256 private key (PEM), read only by devtoken

try
{
    return args switch
    {
        ["init", .. var rest] => Init(rest.Contains("--rotate")),
        ["list"] => List(),
        ["get", .. var rest] => Get(Option(rest, "--profile") ?? throw new DevTokenException("Missing --profile <name>."), Option(rest, "--lifetime")),
        ["dotenv", var file, .. var rest] when !file.StartsWith("--") => DotEnv(file, Option(rest, "--lifetime")),
        _ => Usage(),
    };
}
catch (DevTokenException ex)
{
    Console.Error.WriteLine($"devtoken: {ex.Message}");
    return 1;
}

static int Usage()
{
    Console.Error.WriteLine("Usage: dotnet local/tools/devtoken.cs init [--rotate] | list | get --profile <name> [--lifetime <minutes>] | dotenv <file> [--lifetime <minutes>]");
    return 2;
}

static int Init(bool rotate)
{
    var settings = Settings.Load();
    var secrets = UserSecrets.Load(settings.UserSecretsPath);
    var existing = settings.Algorithm == "HS256" ? SigningKeyParameter : PrivateKeySecret;

    if (secrets[existing] is not null && !rotate)
    {
        Console.Error.WriteLine($"Local {settings.Algorithm} key already present in {settings.UserSecretsPath}. Use --rotate to replace it.");
        return 0;
    }

    if (settings.Algorithm == "HS256")
    {
        // 512 random bits as text: apps that read the key as UTF-8 text (Jwt:Key) and the AppHost (base64 of those
        // bytes for config-bound JwtBearer) derive the same key bytes from it.
        secrets[SigningKeyParameter] = Base64Url(RandomNumberGenerator.GetBytes(64));
    }
    else
    {
        using var rsa = RSA.Create(2048);
        secrets[PrivateKeySecret] = rsa.ExportPkcs8PrivateKeyPem();
        secrets[PublicKeyParameter] = rsa.ExportSubjectPublicKeyInfoPem();
    }

    UserSecrets.Save(settings.UserSecretsPath, secrets);
    Console.Error.WriteLine($"Local {settings.Algorithm} key {(rotate ? "rotated" : "created")} in {settings.UserSecretsPath}. Restart the AppHost to use it.");
    return 0;
}

static int List()
{
    var settings = Settings.Load();
    foreach (var (name, profile) in settings.Profiles.OrderBy(p => p.Key, StringComparer.Ordinal))
    {
        var roles = profile.Roles.Count > 0 ? string.Join(",", profile.Roles) : "-";
        var scopes = profile.Scopes.Count > 0 ? string.Join(",", profile.Scopes) : "-";
        Console.WriteLine($"{name,-24} api={profile.Api,-16} roles={roles,-24} scopes={scopes}");
    }

    return 0;
}

static int Get(string profileName, string? lifetimeOption)
{
    var settings = Settings.Load();
    var lifetime = Lifetime(settings, lifetimeOption);
    Console.WriteLine(CreateToken(settings, profileName, lifetime));
    Console.Error.WriteLine($"devtoken: '{profileName}' for {settings.Profiles[profileName].Api} (aud {settings.Apis[settings.Profiles[profileName].Api].Audience}), expires {DateTimeOffset.UtcNow.AddMinutes(lifetime):u}.");
    return 0;
}

static int DotEnv(string file, string? lifetimeOption)
{
    // Upserts one DEVTOKEN_<PROFILE> line per profile and keeps every other line (other secrets, comments) as it is.
    var settings = Settings.Load();
    var lifetime = Lifetime(settings, lifetimeOption);
    var tokens = settings.Profiles.Keys.Order(StringComparer.Ordinal)
        .ToDictionary(name => DotEnvKey(name), name => CreateToken(settings, name, lifetime));
    var lines = File.Exists(file) ? File.ReadAllLines(file).ToList() : [];
    foreach (var (key, token) in tokens)
    {
        var index = lines.FindIndex(line => line.StartsWith(key + "=", StringComparison.Ordinal));
        if (index >= 0)
        {
            lines[index] = $"{key}={token}";
        }
        else
        {
            lines.Add($"{key}={token}");
        }
    }

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);
    File.WriteAllLines(file, lines);
    if (!OperatingSystem.IsWindows())
    {
        File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    Console.Error.WriteLine($"devtoken: {tokens.Count} tokens ({string.Join(", ", tokens.Keys)}) written to {file}, expire {DateTimeOffset.UtcNow.AddMinutes(lifetime):u}. Keep that file git-ignored.");
    return 0;
}

static string DotEnvKey(string profileName) =>
    "DEVTOKEN_" + new string([.. profileName.ToUpperInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : '_')]);

static int Lifetime(Settings settings, string? lifetimeOption) =>
    lifetimeOption is null ? settings.DefaultLifetimeMinutes
        : int.TryParse(lifetimeOption, out var minutes) && minutes is > 0 and <= 1440 ? minutes
        : throw new DevTokenException("--lifetime must be between 1 and 1440 minutes.");

static string CreateToken(Settings settings, string profileName, int lifetime)
{
    if (!settings.Profiles.TryGetValue(profileName, out var profile))
    {
        throw new DevTokenException($"Unknown profile '{profileName}'. Available: {string.Join(", ", settings.Profiles.Keys.Order())}.");
    }

    if (!settings.Apis.TryGetValue(profile.Api, out var api))
    {
        throw new DevTokenException($"Profile '{profileName}' targets unknown api '{profile.Api}'.");
    }

    var now = DateTimeOffset.UtcNow;
    var payload = new JsonObject
    {
        ["iss"] = settings.Issuer,
        ["aud"] = api.Audience,
        ["sub"] = profile.Subject,
        ["iat"] = now.ToUnixTimeSeconds(),
        ["nbf"] = now.ToUnixTimeSeconds(),
        ["exp"] = now.AddMinutes(lifetime).ToUnixTimeSeconds(),
        ["jti"] = Guid.NewGuid().ToString("N"),
    };

    if (profile.Name is not null)
    {
        payload["name"] = profile.Name;
    }

    if (profile.Roles.Count > 0)
    {
        payload[api.RoleClaim] = new JsonArray([.. profile.Roles.Select(r => (JsonNode?)JsonValue.Create(r))]);
    }

    if (profile.Scopes.Count > 0)
    {
        payload[api.ScopeClaim ?? throw new DevTokenException($"Api '{profile.Api}' has no ScopeClaim but profile '{profileName}' has scopes.")] =
            string.Join(' ', profile.Scopes);
    }

    foreach (var (type, value) in profile.Claims)
    {
        payload[type] = value;
    }

    var secrets = UserSecrets.Load(settings.UserSecretsPath);
    var header = new JsonObject { ["alg"] = settings.Algorithm, ["typ"] = "JWT" };
    var signingInput = $"{Base64Url(Encoding.UTF8.GetBytes(header.ToJsonString()))}.{Base64Url(Encoding.UTF8.GetBytes(payload.ToJsonString()))}";
    var signature = settings.Algorithm == "HS256"
        ? HMACSHA256.HashData(Encoding.UTF8.GetBytes(RequiredSecret(secrets, SigningKeyParameter)), Encoding.UTF8.GetBytes(signingInput))
        : SignRsa(RequiredSecret(secrets, PrivateKeySecret), signingInput);

    return $"{signingInput}.{Base64Url(signature)}";
}

static byte[] SignRsa(string privateKeyPem, string signingInput)
{
    using var rsa = RSA.Create();
    rsa.ImportFromPem(privateKeyPem);
    return rsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
}

static string RequiredSecret(JsonObject secrets, string key) =>
    secrets[key]?.GetValue<string>() is { Length: > 0 } value
        ? value
        : throw new DevTokenException("No local signing key yet. Run `dotnet local/tools/devtoken.cs init` first.");

static string? Option(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

internal sealed class DevTokenException(string message) : Exception(message);

internal sealed record ApiSettings(string Audience, string RoleClaim, string? ScopeClaim);

internal sealed record ProfileSettings(
    string Api, string Subject, string? Name, IReadOnlyList<string> Roles, IReadOnlyList<string> Scopes,
    IReadOnlyDictionary<string, string> Claims);

internal sealed record Settings(
    string Algorithm, string Issuer, int DefaultLifetimeMinutes, string UserSecretsPath,
    IReadOnlyDictionary<string, ApiSettings> Apis, IReadOnlyDictionary<string, ProfileSettings> Profiles)
{
    public static Settings Load()
    {
        var directory = ToolDirectory();
        var path = Path.Combine(directory, "devtoken.json");
        var root = (File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) : null)?["LocalAuth"]
            ?? throw new DevTokenException($"{path} is missing or has no LocalAuth section.");

        var algorithm = Text(root, "Algorithm") ?? "HS256";
        if (algorithm is not ("HS256" or "RS256"))
        {
            throw new DevTokenException($"Unsupported Algorithm '{algorithm}' (HS256 or RS256).");
        }

        var appHostProject = Path.GetFullPath(Path.Combine(directory, Text(root, "AppHostProject") ?? throw new DevTokenException("Missing AppHostProject.")));
        var userSecretsId = File.Exists(appHostProject)
            ? XDocument.Load(appHostProject).Descendants("UserSecretsId").FirstOrDefault()?.Value
            : throw new DevTokenException($"AppHost project not found: {appHostProject}.");

        var apis = (root["Apis"] as JsonObject ?? []).ToDictionary(
            api => api.Key,
            api => new ApiSettings(
                Text(api.Value, "Audience") ?? throw new DevTokenException($"Api '{api.Key}' has no Audience."),
                Text(api.Value, "RoleClaim") ?? "role",
                Text(api.Value, "ScopeClaim")));

        var profiles = (root["Profiles"] as JsonObject ?? []).ToDictionary(
            profile => profile.Key,
            profile => new ProfileSettings(
                Text(profile.Value, "Api") ?? throw new DevTokenException($"Profile '{profile.Key}' has no Api."),
                Text(profile.Value, "Subject") ?? throw new DevTokenException($"Profile '{profile.Key}' has no Subject."),
                Text(profile.Value, "Name"),
                Strings(profile.Value?["Roles"]),
                Strings(profile.Value?["Scopes"]),
                (profile.Value?["Claims"] as JsonObject ?? []).ToDictionary(c => c.Key, c => c.Value?.GetValue<string>() ?? string.Empty)));

        return new(
            algorithm,
            Text(root, "Issuer") ?? throw new DevTokenException("Missing Issuer."),
            root["DefaultLifetimeMinutes"]?.GetValue<int>() ?? 60,
            UserSecrets.PathFor(userSecretsId ?? throw new DevTokenException($"{appHostProject} has no <UserSecretsId>.")),
            apis,
            profiles);
    }

    private static string? Text(JsonNode? node, string name) => node?[name]?.GetValue<string>();

    private static string[] Strings(JsonNode? node) => node is JsonArray array ? [.. array.Select(n => n!.GetValue<string>())] : [];

    // The directory of this source file, wherever the command is run from.
    private static string ToolDirectory([CallerFilePath] string sourceFile = "") => Path.GetDirectoryName(sourceFile)!;
}

/// <summary>The AppHost's .NET user secrets file (same location `dotnet user-secrets` and `aspire secret` use).</summary>
internal static class UserSecrets
{
    public static string PathFor(string userSecretsId)
    {
        var root = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetEnvironmentVariable("APPDATA") ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets")
            : Path.Combine(Environment.GetEnvironmentVariable("HOME") ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets");
        return Path.Combine(root, userSecretsId, "secrets.json");
    }

    public static JsonObject Load(string path) =>
        File.Exists(path) ? JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? [] : [];

    public static void Save(string path, JsonObject secrets)
    {
        var directory = Path.GetDirectoryName(path)!;
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, secrets.ToJsonString());
            return;
        }

        // Readable by the current user only.
        Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        File.WriteAllText(path, secrets.ToJsonString());
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
