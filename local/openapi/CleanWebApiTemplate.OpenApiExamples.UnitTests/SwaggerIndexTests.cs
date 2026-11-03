namespace CleanWebApiTemplate.OpenApiExamples.UnitTests;

public sealed class SwaggerIndexTests
{
    [Fact]
    public void Create_EmbedsUsersInTemplate()
    {
        using var temp = new TestDirectory();
        temp.Write("swagger/index.html", "<html><script>const localUsers = /*__LOCAL_USERS__*/;</script></html>");
        temp.Write("users.json", """
        {
          "users": {
            "user": { "name": "local.user", "email": "local.user@example.com", "roles": ["User"] },
            "no-role": { "email": "local.norole@example.com", "roles": [] }
          }
        }
        """);

        var users = LocalUsers.Load(Path.Combine(temp.Root, "users.json"));
        using var stream = SwaggerIndex.Create(temp.Root, users);
        using var reader = new StreamReader(stream);
        var html = reader.ReadToEnd();

        Assert.DoesNotContain("/*__LOCAL_USERS__*/", html, StringComparison.Ordinal);
        Assert.Contains("\"profile\":\"no-role\"", html, StringComparison.Ordinal);
        Assert.Contains("\"profile\":\"user\"", html, StringComparison.Ordinal);
        Assert.Contains("local.user@example.com", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_RejectsMissingTemplate()
    {
        using var temp = new TestDirectory();
        temp.Write("users.json", """{ "users": { "user": { "email": "local.user@example.com" } } }""");
        var users = LocalUsers.Load(Path.Combine(temp.Root, "users.json"));

        Assert.Throws<InvalidOperationException>(() => SwaggerIndex.Create(temp.Root, users));
    }
}
