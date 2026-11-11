using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Application.Project.Filter;

namespace CleanWebApiTemplate.Api.Project.Filter;

/// <summary>Filter, paging and sorting options for the Project list.</summary>
public sealed record FilteredProjectRequest
{
    /// <summary>Project ids to include.</summary>
    public string[]? Ids { get; init; }

    /// <summary>Name fragments to match (contains).</summary>
    public string[]? Names { get; init; }

    /// <summary>Statuses to include: 1 Draft, 2 Active, 3 Completed, 4 Archived.</summary>
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
    /// Sort keys and directions, applied in order. See <see cref="ProjectSortCatalog"/> for the
    /// accepted keys.
    /// </summary>
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; init; }

    public FilteredProjectInput ToInput(OwnerScope owner) => new()
    {
        Ids = Ids,
        Names = Names,
        Statuses = Statuses,
        Owners = owner.FilterOwners(Owners),
        StartDate = StartDate,
        EndDate = EndDate,
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties,
    };
}
