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
    public void EnvironmentAndConfigurationKeys_HaveTheExpectedValues()
    {
        Assert.Equal("Development", Constants.DEV_ENVIRONMNET);
        Assert.Equal("Integration", Constants.INTEGRATION_ENVIRONMENT);
        Assert.Equal("Staging", Constants.STAGING_ENVIRONMENT);
        Assert.Equal("Production", Constants.PRODUCTION_ENVIRONMENT);
        Assert.Equal("Test", Constants.TEST_ENVIRONMENT);
        Assert.Equal("API_KEY", Constants.API_KEY);
        Assert.Equal("Authentication:Authority", Constants.AUTHORITY);
        Assert.Equal("DefaultCorsPolicy", Constants.DEFAULT_CORS_POLICY_NAME);
        Assert.Equal("JWTBearer", Constants.AUTHENTICATION_SCHEME);
    }
}
