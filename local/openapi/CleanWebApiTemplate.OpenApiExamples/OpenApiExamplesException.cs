namespace CleanWebApiTemplate.OpenApiExamples;

/// <summary>
/// The local examples catalog cannot be applied to the API OpenAPI document.
/// </summary>
internal sealed class OpenApiExamplesException : Exception
{
    public OpenApiExamplesException(IReadOnlyList<string> problems)
        : base(problems.Count == 1
            ? problems[0]
            : $"The local OpenAPI examples have {problems.Count} problems.")
    {
        Problems = problems;
    }

    public IReadOnlyList<string> Problems { get; }
}
