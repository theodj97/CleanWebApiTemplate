namespace CleanWebApiTemplate.OpenApiExamples.UnitTests;

public sealed class LocalAuthTests
{
    private const string Key = "0123456789abcdef0123456789abcdef";

    [Fact]
    public void Constructor_AcceptsValidSettings()
    {
        var auth = new LocalAuth(Key, "https://localhost", "https://localhost:5001", "role", 60);

        Assert.Equal(Key, auth.SigningKey);
        Assert.Equal("https://localhost", auth.Issuer);
        Assert.Equal("https://localhost:5001", auth.Audience);
        Assert.Equal("role", auth.RoleClaim);
        Assert.Equal(60, auth.LifetimeMinutes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Constructor_RejectsShortKey(string key)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new LocalAuth(key, "https://localhost", "https://localhost:5001", "role", 60));
    }

    [Theory]
    [InlineData("", "https://localhost:5001", "role", 60)]
    [InlineData("https://localhost", "", "role", 60)]
    [InlineData("https://localhost", "https://localhost:5001", "", 60)]
    [InlineData("https://localhost", "https://localhost:5001", "role", 0)]
    [InlineData("https://localhost", "https://localhost:5001", "role", 1441)]
    public void Constructor_RejectsInvalidSettings(string issuer, string audience, string roleClaim, int lifetime)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new LocalAuth(Key, issuer, audience, roleClaim, lifetime));
    }
}
