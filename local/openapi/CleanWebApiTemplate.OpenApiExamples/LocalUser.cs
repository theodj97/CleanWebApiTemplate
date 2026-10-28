namespace CleanWebApiTemplate.OpenApiExamples;

internal sealed record LocalUser(
    string Profile,
    string Name,
    string Email,
    IReadOnlyList<string> Roles,
    string Subject);
