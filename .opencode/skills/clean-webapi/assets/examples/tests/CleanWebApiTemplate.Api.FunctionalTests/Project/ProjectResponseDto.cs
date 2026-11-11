namespace CleanWebApiTemplate.Api.FunctionalTests.Project;

/// <summary>
/// The shape of a Project response as returned by the API. Functional tests are black box, so they describe the
/// contract they consume instead of referencing the API project.
/// </summary>
public sealed record ProjectResponseDto
{
    public Ulid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}
