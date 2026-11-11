using CleanWebApiTemplate.Domain.Todo;

namespace CleanWebApiTemplate.Application.Todo;

/// <summary>The read model of a Todo as the API returns it.</summary>
public sealed record TodoDto
{
    public required Ulid Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
    public required ETodoStatus Status { get; init; }
    public required string CreatedBy { get; init; }
    public required string UpdatedBy { get; init; }
}
