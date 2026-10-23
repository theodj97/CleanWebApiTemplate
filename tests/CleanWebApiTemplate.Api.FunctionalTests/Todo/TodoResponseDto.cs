namespace CleanWebApiTemplate.Api.FunctionalTests.Todo;

/// <summary>
/// The shape of a Todo response as returned by the API. Functional tests are black box, so they describe the
/// contract they consume instead of referencing the API project.
/// </summary>
public sealed record TodoResponseDto
{
    public Ulid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
