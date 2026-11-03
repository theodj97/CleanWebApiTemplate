namespace CleanWebApiTemplate.Api.Configuration;

public static class Constants
{
    // Configuration keys
    public const string API_KEY = "API_KEY";
    public const string AUTHENTICATION_MODE = "Authentication:Mode";
    public const string AUTHORITY = "Authentication:Authority";
    public const string AUDIENCE = "Audience";
    public const string CORS_ALLOWED_ORIGINS = "Cors:AllowedOrigins";
    public const string CORS_ALLOW_CREDENTIALS = "Cors:AllowCredentials";
    public const string OPENAPI_ENABLED = "OpenApi:Enabled";
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
