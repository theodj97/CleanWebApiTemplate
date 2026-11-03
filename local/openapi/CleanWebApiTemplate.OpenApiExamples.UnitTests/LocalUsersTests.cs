namespace CleanWebApiTemplate.OpenApiExamples.UnitTests;

public sealed class LocalUsersTests
{
    [Fact]
    public void Load_ReadsProfilesAndRoles()
    {
        using var temp = new TestDirectory();
        temp.Write("users.json", """
        {
          "users": {
            "user": { "name": "local.user", "email": "local.user@example.com", "roles": ["User"], "subject": "sub-1" },
            "no-role": { "email": "local.norole@example.com" }
          }
        }
        """);

        var users = LocalUsers.Load(Path.Combine(temp.Root, "users.json"));

        Assert.Equal(2, users.Users.Count);

        var user = users.Users["user"];
        Assert.Equal("user", user.Profile);
        Assert.Equal("local.user", user.Name);
        Assert.Equal("local.user@example.com", user.Email);
        Assert.Equal(new[] { "User" }, user.Roles);
        Assert.Equal("sub-1", user.Subject);

        var noRole = users.Users["no-role"];
        Assert.Equal("no-role", noRole.Name);
        Assert.Empty(noRole.Roles);
        Assert.Equal("no-role", noRole.Subject);
    }

    [Fact]
    public void Load_RejectsMissingEmail()
    {
        using var temp = new TestDirectory();
        temp.Write("users.json", """{ "users": { "user": { "name": "local.user" } } }""");

        Assert.Throws<InvalidOperationException>(() =>
            LocalUsers.Load(Path.Combine(temp.Root, "users.json")));
    }

    [Fact]
    public void Load_RejectsMissingFile()
    {
        using var temp = new TestDirectory();

        Assert.Throws<InvalidOperationException>(() =>
            LocalUsers.Load(Path.Combine(temp.Root, "missing.json")));
    }
}
