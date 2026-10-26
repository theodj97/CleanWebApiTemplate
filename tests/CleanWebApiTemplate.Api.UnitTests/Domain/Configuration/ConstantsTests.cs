using CleanWebApiTemplate.Domain.Configuration;

namespace CleanWebApiTemplate.Api.UnitTests.Domain.Configuration;

/// <summary>The policy arrays are the single list of authorization policies every host must register.</summary>
public class ConstantsTests
{
    [Fact]
    public void AuthorizationPolicies_ListsEveryPolicyOnce()
    {
        Assert.Equal(
            [Constants.ADMIN_POLICY, Constants.OPERATOR_POLICY, Constants.USER_POLICY, Constants.EXTERNAL_POLICY],
            Constants.AuthorizationPolicies);
    }

    [Fact]
    public void AuthenticationPolicies_ListsTheJwtBearerScheme()
    {
        Assert.Equal([Constants.AUTHENTICATION_SCHEME], Constants.AuthenticationPolicies);
    }

    [Fact]
    public void ConfigurationKeys_HaveTheExpectedValues()
    {
        Assert.Equal("API_KEY", Constants.API_KEY);
        Assert.Equal("Authentication:Authority", Constants.AUTHORITY);
        Assert.Equal("Audience", Constants.AUDIENCE);
        Assert.Equal("Cors:AllowedOrigins", Constants.CORS_ALLOWED_ORIGINS);
        Assert.Equal("Cors:AllowCredentials", Constants.CORS_ALLOW_CREDENTIALS);
        Assert.Equal("OpenApi:Enabled", Constants.OPENAPI_ENABLED);
        Assert.Equal("DefaultCorsPolicy", Constants.DEFAULT_CORS_POLICY_NAME);
        Assert.Equal("JWTBearer", Constants.AUTHENTICATION_SCHEME);
    }
}
