using System.Text.Json;
using System.Text.Json.Nodes;

namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// Local users shown in the Swagger dropdown. The emails must match the seed's CreatedBy values
/// so that the user/operator/admin filters and ownership checks return meaningful rows.
/// </summary>
internal sealed class LocalUsers
{
    private LocalUsers(IReadOnlyDictionary<string, LocalUser> users) => Users = users;

    public IReadOnlyDictionary<string, LocalUser> Users { get; }

    public static LocalUsers Load(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"The local users file '{path}' does not exist.");

        JsonObject root;
        try
        {
            root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
                ?? throw new InvalidOperationException($"'{path}' is not a JSON object.");
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            throw new InvalidOperationException($"'{path}' is not valid JSON: {exception.Message}");
        }

        if (root["users"] is not JsonObject usersNode)
            throw new InvalidOperationException($"'{path}' has no 'users' object.");

        var users = new Dictionary<string, LocalUser>(StringComparer.Ordinal);
        foreach (var (profile, userNode) in usersNode)
        {
            if (userNode is not JsonObject user)
                throw new InvalidOperationException($"'{path}': user '{profile}' must be a JSON object.");

            var email = JsonText.Read(user["email"]);
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException($"'{path}': user '{profile}' needs an 'email'.");

            var name = JsonText.Read(user["name"]) ?? profile;
            var subject = JsonText.Read(user["subject"]) ?? profile;
            var roles = JsonText.ReadStrings(user["roles"]);

            users.Add(profile, new LocalUser(profile, name, email, roles, subject));
        }

        if (users.Count == 0)
            throw new InvalidOperationException($"'{path}' has no users.");

        return new LocalUsers(users);
    }
}
