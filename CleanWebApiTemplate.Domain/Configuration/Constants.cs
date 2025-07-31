namespace CleanWebApiTemplate.Domain.Configuration;

public static class Constants
{
    // Envs
    public const string DEV_ENVIRONMNET = "DEVELOPMENT";
    public const string INTEGRATION_ENVIRONMENT = "INTEGRATION";
    public const string STAGING_ENVIRONMENT = "STAGING";
    public const string PRODUCTION_ENVIRONMENT = "PRODUCTION";
    public const string TEST_ENVIRONMNET = "TEST";

    // Envs variables
    public const string API_KEY = "ApiKey";
    public const string DEFAULT_CORS_POLICY_NAME = "DefaultCorsPolicy";

    // User authorization policies.
    public const string ADMIN_POLICY = "Admin";
    public const string OPERATOR_POLICY = "Operator";
    public const string USER_POLICY = "User";
    public const string EXTERNAL_POLICY = "External";

    public static readonly string[] AuthorizationPolicies =
    [
        ADMIN_POLICY,
        OPERATOR_POLICY,
        USER_POLICY,
        EXTERNAL_POLICY
    ];

    // Authentication Schemes.
    public const string AUTHENTICATION_SCHEME = "JWTBearer";
    public static readonly string[] AuthenticationPolicies =
    [
        AUTHENTICATION_SCHEME
    ];
}
