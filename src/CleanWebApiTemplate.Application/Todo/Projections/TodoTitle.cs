namespace CleanWebApiTemplate.Application.Todo.Projections;

/// <summary>The id/title projection of a Todo. Only these two fields exist by design.</summary>
public sealed record TodoTitle(Ulid Id, string Title);
