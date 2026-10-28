using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// Applies the local examples catalog to an OpenAPI document. The document is plain JSON:
/// the merger navigates it with <see cref="JsonNode"/>, so there is no reflection and no
/// dependency on the OpenAPI object model.
/// </summary>
public static partial class OpenApiExamplesMerger
{
    /// <summary>
    /// Adds request body examples and parameter examples to <paramref name="document"/>.
    /// Returns the list of catalog entries that could not be applied.
    /// </summary>
    public static IReadOnlyList<string> Merge(JsonObject document, JsonObject catalog, string basePath)
    {
        var problems = new List<string>();

        if (document["paths"] is not JsonObject paths)
        {
            problems.Add("The OpenAPI document has no 'paths' object.");
            return problems;
        }

        if (catalog["operations"] is not JsonObject operations)
        {
            problems.Add("The examples catalog has no 'operations' object.");
            return problems;
        }

        var pathIndex = BuildPathIndex(paths, problems);

        foreach (var (operationKey, operationSpecNode) in operations)
        {
            if (operationSpecNode is not JsonObject operationSpec)
            {
                problems.Add($"'{operationKey}': the operation specification must be a JSON object.");
                continue;
            }

            if (!TryParseOperationKey(operationKey, out var method, out var path))
            {
                problems.Add($"'{operationKey}': expected 'METHOD /path'.");
                continue;
            }

            if (!pathIndex.TryGetValue(NormalizePath(path), out var documentPath))
            {
                problems.Add($"'{operationKey}': no OpenAPI path matches '{path}'.");
                continue;
            }

            if (paths[documentPath] is not JsonObject pathItem
                || pathItem[method] is not JsonObject operation)
            {
                problems.Add($"'{operationKey}': the OpenAPI document has no '{method}' operation.");
                continue;
            }

            ApplyRequestBody(document, operation, operationSpec, basePath, operationKey, problems);
            ApplyParameters(document, pathItem, operation, operationSpec, operationKey, problems);
        }

        ApplyServers(document);
        ApplyInfo(document);
        ApplyBearerSecurity(document);

        return problems;
    }

    private static Dictionary<string, string> BuildPathIndex(JsonObject paths, List<string> problems)
    {
        var index = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var (path, _) in paths)
        {
            var normalized = NormalizePath(path);
            if (!index.TryAdd(normalized, path))
                problems.Add($"The OpenAPI document contains more than one path that normalizes to '{normalized}'.");
        }

        return index;
    }

    private static bool TryParseOperationKey(string key, out string method, out string path)
    {
        var separator = key.IndexOf(' ');
        if (separator <= 0 || separator == key.Length - 1)
        {
            method = string.Empty;
            path = string.Empty;
            return false;
        }

        method = key[..separator].ToLowerInvariant();
        path = key[(separator + 1)..].Trim();
        return method.Length > 0 && path.Length > 0;
    }

    private static void ApplyRequestBody(
        JsonObject document,
        JsonObject operation,
        JsonObject operationSpec,
        string basePath,
        string operationKey,
        List<string> problems)
    {
        if (operationSpec["requestBody"] is null)
            return;

        if (operationSpec["requestBody"] is not JsonObject requestBodySpec)
        {
            problems.Add($"'{operationKey}': 'requestBody' must be a JSON object.");
            return;
        }

        var requestBody = ResolveReference(document, operation["requestBody"]);
        if (requestBody?["content"] is not JsonObject content)
        {
            problems.Add($"'{operationKey}': the operation has no request body content.");
            return;
        }

        if (requestBodySpec["examples"] is not JsonObject examplesSpec)
        {
            problems.Add($"'{operationKey}': 'requestBody.examples' must be a JSON object.");
            return;
        }

        var requestedContentType = ReadString(requestBodySpec["contentType"]);
        var contentKey = FindContentKey(content, requestedContentType);
        if (contentKey is null)
        {
            problems.Add($"'{operationKey}': no request body content type matches '{requestedContentType ?? "application/json"}'.");
            return;
        }

        if (content[contentKey] is not JsonObject mediaType)
        {
            problems.Add($"'{operationKey}': the request body content '{contentKey}' is not a JSON object.");
            return;
        }

        var examples = mediaType["examples"] as JsonObject ?? new JsonObject();
        mediaType["examples"] = examples;

        foreach (var (exampleName, exampleSpecNode) in examplesSpec)
        {
            if (exampleSpecNode is not JsonObject exampleSpec)
            {
                problems.Add($"'{operationKey}' example '{exampleName}': the specification must be a JSON object.");
                continue;
            }

            var value = LoadValue(exampleSpec, basePath, operationKey, exampleName, problems);
            if (value is null)
                continue;

            var example = new JsonObject
            {
                ["value"] = value,
            };

            if (ReadString(exampleSpec["summary"]) is { Length: > 0 } summary)
                example["summary"] = summary;

            if (ReadString(exampleSpec["description"]) is { Length: > 0 } description)
                example["description"] = description;

            examples[exampleName] = example;
        }
    }

    private static void ApplyParameters(
        JsonObject document,
        JsonObject pathItem,
        JsonObject operation,
        JsonObject operationSpec,
        string operationKey,
        List<string> problems)
    {
        if (operationSpec["parameters"] is null)
            return;

        if (operationSpec["parameters"] is not JsonArray parametersSpec)
        {
            problems.Add($"'{operationKey}': 'parameters' must be a JSON array.");
            return;
        }

        var parameters = CollectParameters(document, pathItem, operation);

        foreach (var parameterSpecNode in parametersSpec)
        {
            if (parameterSpecNode is not JsonObject parameterSpec)
            {
                problems.Add($"'{operationKey}': each parameter specification must be a JSON object.");
                continue;
            }

            var name = ReadString(parameterSpec["name"]);
            var location = ReadString(parameterSpec["in"]);
            var value = parameterSpec["value"];

            if (string.IsNullOrWhiteSpace(name) || value is null)
            {
                problems.Add($"'{operationKey}': a parameter needs 'name' and 'value'.");
                continue;
            }

            var matches = parameters
                .Where(parameter => string.Equals(ReadString(parameter["name"]), name, StringComparison.Ordinal)
                    && (string.IsNullOrWhiteSpace(location)
                        || string.Equals(ReadString(parameter["in"]), location, StringComparison.Ordinal)))
                .ToList();

            if (matches.Count == 0)
            {
                var where = string.IsNullOrWhiteSpace(location) ? string.Empty : $" in '{location}'";
                problems.Add($"'{operationKey}': the OpenAPI document has no parameter '{name}'{where}.");
                continue;
            }

            if (matches.Count > 1)
            {
                problems.Add($"'{operationKey}': parameter '{name}' is ambiguous; set 'in'.");
                continue;
            }

            matches[0]["example"] = value.DeepClone();
        }
    }

    private static List<JsonObject> CollectParameters(JsonObject document, JsonObject pathItem, JsonObject operation)
    {
        var parameters = new List<JsonObject>();
        AddParameters(document, pathItem["parameters"], parameters);
        AddParameters(document, operation["parameters"], parameters);
        return parameters;
    }

    private static void AddParameters(JsonObject document, JsonNode? node, List<JsonObject> parameters)
    {
        if (node is not JsonArray array)
            return;

        foreach (var item in array)
        {
            var resolved = ResolveReference(document, item);
            if (resolved is null)
                continue;

            // An operation-level parameter overrides a path-level one with the same name and location.
            var name = ReadString(resolved["name"]);
            var location = ReadString(resolved["in"]);
            parameters.RemoveAll(existing =>
                string.Equals(ReadString(existing["name"]), name, StringComparison.Ordinal)
                && string.Equals(ReadString(existing["in"]), location, StringComparison.Ordinal));

            parameters.Add(resolved);
        }
    }

    private static JsonObject? ResolveReference(JsonObject document, JsonNode? node)
    {
        if (node is not JsonObject obj)
            return null;

        var reference = ReadString(obj["$ref"]);
        if (reference is null)
            return obj;

        if (!reference.StartsWith("#/", StringComparison.Ordinal))
            return null;

        JsonNode? current = document;
        foreach (var segment in reference[2..].Split('/'))
        {
            if (current is not JsonObject currentObject)
                return null;

            current = currentObject[segment.Replace("~1", "/").Replace("~0", "~")];
        }

        return current as JsonObject;
    }

    private static JsonNode? LoadValue(
        JsonObject exampleSpec,
        string basePath,
        string operationKey,
        string exampleName,
        List<string> problems)
    {
        var body = ReadString(exampleSpec["body"]);
        var inline = exampleSpec["value"];

        if (body is not null && inline is not null)
        {
            problems.Add($"'{operationKey}' example '{exampleName}': use either 'body' or 'value', not both.");
            return null;
        }

        if (inline is not null)
            return inline.DeepClone();

        if (body is null)
        {
            problems.Add($"'{operationKey}' example '{exampleName}': missing 'body' or 'value'.");
            return null;
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(basePath));
        var fullPath = Path.GetFullPath(Path.Combine(root, body));
        var relative = Path.GetRelativePath(root, fullPath);

        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            problems.Add($"'{operationKey}' example '{exampleName}': the body path '{body}' escapes '{basePath}'.");
            return null;
        }

        if (!File.Exists(fullPath))
        {
            problems.Add($"'{operationKey}' example '{exampleName}': body file '{body}' does not exist.");
            return null;
        }

        try
        {
            var text = File.ReadAllText(fullPath);
            return JsonNode.Parse(text) ?? throw new JsonException("The body file is empty.");
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            problems.Add($"'{operationKey}' example '{exampleName}': could not read '{body}': {exception.Message}");
            return null;
        }
    }

    private static string? FindContentKey(JsonObject content, string? requestedContentType)
    {
        var target = string.IsNullOrWhiteSpace(requestedContentType) ? "application/json" : requestedContentType;

        foreach (var (key, _) in content)
        {
            if (string.Equals(key, target, StringComparison.OrdinalIgnoreCase)
                || key.StartsWith(target + ";", StringComparison.OrdinalIgnoreCase))
            {
                return key;
            }
        }

        return content.Count == 1 ? content.First().Key : null;
    }

    private static void ApplyServers(JsonObject document)
    {
        // "Try it out" must call this sidecar (which proxies /api), not the API directly.
        var servers = new JsonArray();
        JsonNode server = new JsonObject { ["url"] = "/" };
        servers.Add(server);
        document["servers"] = servers;
    }

    private static void ApplyInfo(JsonObject document)
    {
        if (document["info"] is not JsonObject info)
        {
            info = new JsonObject();
            document["info"] = info;
        }

        if (ReadString(info["title"]) is { Length: > 0 } title
            && !title.Contains("local examples", StringComparison.OrdinalIgnoreCase))
        {
            info["title"] = $"{title} (local examples)";
        }

        const string note =
            "Local examples loaded from `local/openapi`. Authorize with a token from "
            + "`dotnet local/tools/devtoken.cs get --profile <profile>`. Nothing is persisted: "
            + "the seed is rebuilt on every `aspire start`.";

        info["description"] = ReadString(info["description"]) is { Length: > 0 } description
            ? $"{description}\n\n{note}"
            : note;
    }

    private static void ApplyBearerSecurity(JsonObject document)
    {
        var components = document["components"] as JsonObject ?? new JsonObject();
        document["components"] = components;

        var securitySchemes = components["securitySchemes"] as JsonObject ?? new JsonObject();
        components["securitySchemes"] = securitySchemes;

        if (!securitySchemes.ContainsKey("Bearer"))
        {
            securitySchemes["Bearer"] = new JsonObject
            {
                ["type"] = "http",
                ["scheme"] = "bearer",
                ["bearerFormat"] = "JWT",
            };
        }

        if (!document.ContainsKey("security"))
        {
            var security = new JsonArray();
            JsonNode requirement = new JsonObject { ["Bearer"] = new JsonArray() };
            security.Add(requirement);
            document["security"] = security;
        }
    }

    private static string? ReadString(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static string NormalizePath(string path)
    {
        var normalized = RouteConstraintRegex().Replace(path, "{$1}");
        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }

    [GeneratedRegex(@"\{([^}:]+):[^}]+\}", RegexOptions.CultureInvariant)]
    private static partial Regex RouteConstraintRegex();
}
