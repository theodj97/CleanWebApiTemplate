using CleanWebApiTemplate.Application.Todo.GetTitles;

namespace CleanWebApiTemplate.Api.Routes.Todo.Get;

/// <summary>Paging and sorting options for the TODO title projection.</summary>
public sealed class GetTodoTitlesRequest
{
    /// <summary>1-based page number. Required when PageSize is set.</summary>
    public byte? PageNumber { get; set; }

    /// <summary>Page size. Required when PageNumber is set.</summary>
    public byte? PageSize { get; set; }

    /// <summary>
    /// Sort keys and directions, applied in order. Supported keys: id, title, description,
    /// createdAt, updatedAt, status, createdBy, updatedBy.
    /// </summary>
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }

    public GetTodoTitlesInput ToInput(string? ownerScope) => new()
    {
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties,
        CreatedBy = ownerScope
    };
}
