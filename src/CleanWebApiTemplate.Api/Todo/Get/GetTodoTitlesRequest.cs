using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Application.Todo.GetTitles;

namespace CleanWebApiTemplate.Api.Todo.Get;

/// <summary>Paging and sorting options for the Todo title projection.</summary>
public sealed record GetTodoTitlesRequest
{
    /// <summary>1-based page number. Required when PageSize is set.</summary>
    public byte? PageNumber { get; init; }

    /// <summary>Page size. Required when PageNumber is set.</summary>
    public byte? PageSize { get; init; }

    /// <summary>
    /// Sort keys and directions, applied in order. See <see cref="TodoSortCatalog"/> for the
    /// accepted keys.
    /// </summary>
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; init; }

    public GetTodoTitlesInput ToInput(OwnerScope owner) => new()
    {
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties,
        VisibleOwner = owner.VisibleOwner,
    };
}
