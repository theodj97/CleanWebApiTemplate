using System.Text;
using System.Text.Json.Nodes;

namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// Serves the custom Swagger UI index with the local users embedded, so the topbar dropdown can
/// request a token without an extra profiles endpoint.
/// </summary>
internal static class SwaggerIndex
{
    private const string UsersPlaceholder = "/*__LOCAL_USERS__*/";

    public static Stream Create(string basePath, LocalUsers users)
    {
        var templatePath = Path.Combine(basePath, "swagger", "index.html");
        if (!File.Exists(templatePath))
            throw new InvalidOperationException($"The Swagger index template '{templatePath}' does not exist.");

        var template = File.ReadAllText(templatePath);
        if (!template.Contains(UsersPlaceholder, StringComparison.Ordinal))
            throw new InvalidOperationException($"The Swagger index template '{templatePath}' has no '{UsersPlaceholder}' placeholder.");

        var html = template.Replace(UsersPlaceholder, BuildUsersJson(users), StringComparison.Ordinal);
        return new MemoryStream(Encoding.UTF8.GetBytes(html));
    }

    private static string BuildUsersJson(LocalUsers users)
    {
        var array = new JsonArray();
        foreach (var user in users.Users.Values.OrderBy(user => user.Profile, StringComparer.Ordinal))
        {
            var roles = new JsonArray();
            foreach (var role in user.Roles)
            {
                JsonNode? value = JsonValue.Create(role);
                roles.Add(value);
            }

            var item = new JsonObject
            {
                ["profile"] = user.Profile,
                ["name"] = user.Name,
                ["email"] = user.Email,
                ["roles"] = roles,
            };

            JsonNode? node = item;
            array.Add(node);
        }

        return array.ToJsonString();
    }
}
