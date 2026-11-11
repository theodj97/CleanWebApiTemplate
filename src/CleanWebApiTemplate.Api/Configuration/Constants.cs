namespace CleanWebApiTemplate.Api.Configuration;

public static class Constants
{
    // Configuration keys
    public const string ApiKey = "API_KEY";
    public const string AuthenticationMode = "Authentication:Mode";
    public const string Authority = "Authentication:Authority";
    public const string Audience = "Audience";
    public const string CorsAllowedOrigins = "Cors:AllowedOrigins";
    public const string CorsAllowCredentials = "Cors:AllowCredentials";
    public const string OpenApiEnabled = "OpenApi:Enabled";
    public const string DefaultCorsPolicyName = "DefaultCorsPolicy";

    // Authorization policies. Policy names are also the role values the tokens carry.
    public const string AdminPolicy = "Admin";
    public const string OperatorPolicy = "Operator";
    public const string UserPolicy = "User";
    public const string ExternalPolicy = "External";

    public static readonly string[] AuthorizationPolicies =
    [
        AdminPolicy,
        OperatorPolicy,
        UserPolicy,
        ExternalPolicy,
    ];
}
