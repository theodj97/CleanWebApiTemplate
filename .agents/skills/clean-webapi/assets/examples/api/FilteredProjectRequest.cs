using {App}.Application.Abstractions.Identity;
using {App}.Application.Project.Handlers.Filtered;

namespace {App}.Api.Routes.Project.Filter;

/// <summary>Filter, paging and sorting options for the project list.</summary>
public sealed class FilteredProjectRequest
{
    /// <summary>Project ids to include.</summary>
    public string[]? Ids { get; set; }

    /// <summary>Name fragments to match (contains).</summary>
    public string[]? Names { get; set; }

    /// <summary>Statuses to include: 1 Draft, 2 Active, 3 Completed, 4 Archived.</summary>
    public int[]? Statuses { get; set; }

    /// <summary>Admin-only owner filter. Accepts one or more email addresses.</summary>
    public string[]? CreatedBys { get; set; }

    /// <summary>Inclusive lower bound for CreatedAt.</summary>
    public string? StartDate { get; set; }

    /// <summary>Inclusive upper bound for CreatedAt.</summary>
    public string? EndDate { get; set; }

    /// <summary>1-based page number. Required when PageSize is set.</summary>
    public byte? PageNumber { get; set; }

    /// <summary>Page size. Required when PageNumber is set.</summary>
    public byte? PageSize { get; set; }

    /// <summary>Sort keys and directions, applied in order.</summary>
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; set; }

    public FilteredProjectInput ToInput(OwnerScope owner) => new()
    {
        Ids = Ids,
        Names = Names,
        Statuses = Statuses,
        CreatedBys = owner.FilterOwners(CreatedBys),
        StartDate = StartDate,
        EndDate = EndDate,
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties
    };
}
