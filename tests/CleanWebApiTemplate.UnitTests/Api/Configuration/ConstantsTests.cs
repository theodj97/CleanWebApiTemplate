using CleanWebApiTemplate.Api.Configuration;

namespace CleanWebApiTemplate.UnitTests.Api.Configuration;

/// <summary>The policy array and the configuration keys are the single source every host reads.</summary>
public class ConstantsTests
{
    [Fact]
    public void AuthorizationPolicies_ListsEveryPolicyOnce()
    {
        Assert.Equal(
            [Constants.AdminPolicy, Constants.OperatorPolicy, Constants.UserPolicy, Constants.ExternalPolicy],
            Constants.AuthorizationPolicies);
    }

    [Fact]
    public void AuthorizationPolicies_HaveNoDuplicates()
    {
        Assert.Equal(Constants.AuthorizationPolicies.Length,
                     Constants.AuthorizationPolicies.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ConfigurationKeys_HaveTheExpectedValues()
    {
        Assert.Equal("API_KEY", Constants.ApiKey);
        Assert.Equal("Authentication:Mode", Constants.AuthenticationMode);
        Assert.Equal("Authentication:Authority", Constants.Authority);
        Assert.Equal("Audience", Constants.Audience);
        Assert.Equal("Cors:AllowedOrigins", Constants.CorsAllowedOrigins);
        Assert.Equal("Cors:AllowCredentials", Constants.CorsAllowCredentials);
        Assert.Equal("OpenApi:Enabled", Constants.OpenApiEnabled);
        Assert.Equal("DefaultCorsPolicy", Constants.DefaultCorsPolicyName);
    }

    [Fact]
    public void PolicyNames_MatchTheRolesTheTokensCarry()
    {
        Assert.Equal("Admin", Constants.AdminPolicy);
        Assert.Equal("Operator", Constants.OperatorPolicy);
        Assert.Equal("User", Constants.UserPolicy);
        Assert.Equal("External", Constants.ExternalPolicy);
    }
}
