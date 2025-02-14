namespace CleanWebApiTemplate.Domain.Configuration;

public record Constants
{
    // Envs
    public const string DEV_ENVIRONMNET = "DEVELOPMENT";
    public const string INTEGRATION_ENVIRONMENT = "INTEGRATION";
    public const string STAGING_ENVIRONMENT = "STAGING";
    public const string PRODUCTION_ENVIRONMENT = "PRODUCTION";

    // Envs variables
    public const string API_KEY = "ApiKey";
    public const string SQLSERVER_CNNSTRING = "SqlServerCnnString";

    public const string DEFAULT_CORS_POLICY_NAME = "DefaultCorsPolicy";

    // User authorization policies.
    public const string ADMIN_POLICY = "Admin";
    public const string OPERATOR_POLICY = "Operator";
    public const string USER_POLICY = "User";
    public const string EXTERNAL_POLICY = "External";

    public static readonly string[] AUTHORIZATION_POLICIES =
    [
        ADMIN_POLICY,
        OPERATOR_POLICY,
        USER_POLICY,
        EXTERNAL_POLICY
    ];

}
