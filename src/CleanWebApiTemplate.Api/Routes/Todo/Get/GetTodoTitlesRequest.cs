using CleanWebApiTemplate.Application.Abstractions.Identity;
using CleanWebApiTemplate.Application.Todo.Handlers.GetTitles;
using CleanWebApiTemplate.Application.Todo.Sorting;

namespace CleanWebApiTemplate.Api.Routes.Todo.Get;

/// <summary>Paging and sorting options for the TODO title projection.</summary>
public sealed class GetTodoTitlesRequest
{
    /// <summary>1-based page number. Required when PageSize is set.</summary>
    public byte? PageNumber { get; set; }

    /// <summary>Page size. Required when PageNumber is set.</summary>
    public byte? PageSize { get; set; }

    /// <summary>
    /// Sort keys and directions, applied in order. See <see cref="TodoSortCatalog"/> for the
    /// accepted keys.
    /// </summary>
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }

    public GetTodoTitlesInput ToInput(OwnerScope owner) => new()
    {
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties,
        CreatedBy = owner.VisibleOwner
    };
}
