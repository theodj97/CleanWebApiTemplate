using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Application.Todo.Filter;

namespace CleanWebApiTemplate.Api.Todo.Filter;

/// <summary>Filter, paging and sorting options for the Todo list.</summary>
public sealed record FilteredTodoRequest
{
    /// <summary>Todo ids to include.</summary>
    public string[]? Ids { get; init; }

    /// <summary>Title fragments to match (contains).</summary>
    public string[]? Titles { get; init; }

    /// <summary>Statuses to include: 1 Pending, 2 InProgress, 3 Completed, 4 Cancelled.</summary>
    public int[]? Statuses { get; init; }

    /// <summary>
    /// Admin-only owner filter. Accepts one or more email addresses; every listed
    /// owner is included in the database filter.
    /// </summary>
    public string[]? Owners { get; init; }

    /// <summary>Inclusive lower bound for CreatedAt.</summary>
    public string? StartDate { get; init; }

    /// <summary>Inclusive upper bound for CreatedAt.</summary>
    public string? EndDate { get; init; }

    /// <summary>1-based page number. Required when PageSize is set.</summary>
    public byte? PageNumber { get; init; }

    /// <summary>Page size. Required when PageNumber is set.</summary>
    public byte? PageSize { get; init; }

    /// <summary>
    /// Sort keys and directions, applied in order. See <see cref="TodoSortCatalog"/> for the
    /// accepted keys.
    /// </summary>
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; init; }

    public FilteredTodoInput ToInput(OwnerScope owner) => new()
    {
        Ids = Ids,
        Titles = Titles,
        Statuses = Statuses,
        Owners = owner.FilterOwners(Owners),
        StartDate = StartDate,
        EndDate = EndDate,
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties,
    };
}
