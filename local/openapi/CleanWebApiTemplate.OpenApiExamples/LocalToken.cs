namespace CleanWebApiTemplate.OpenApiExamples;

internal sealed record LocalToken(
    string Token,
    DateTimeOffset ExpiresAt,
    int ExpiresIn,
    string Profile,
    string Email,
    IReadOnlyList<string> Roles);
