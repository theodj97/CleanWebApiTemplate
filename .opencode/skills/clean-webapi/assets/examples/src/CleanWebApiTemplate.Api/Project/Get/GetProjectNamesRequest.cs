using CleanWebApiTemplate.Application.Common.Identity;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Application.Project.GetNames;

namespace CleanWebApiTemplate.Api.Project.Get;

/// <summary>Paging and sorting options for the Project name projection.</summary>
public sealed record GetProjectNamesRequest
{
    /// <summary>1-based page number. Required when PageSize is set.</summary>
    public byte? PageNumber { get; init; }

    /// <summary>Page size. Required when PageNumber is set.</summary>
    public byte? PageSize { get; init; }

    /// <summary>
    /// Sort keys and directions, applied in order. See <see cref="ProjectSortCatalog"/> for the
    /// accepted keys.
    /// </summary>
    public IEnumerable<KeyValuePair<string, bool>>? SortProperties { get; init; }

    public GetProjectNamesInput ToInput(OwnerScope owner) => new()
    {
        PageNumber = PageNumber,
        PageSize = PageSize,
        SortProperties = SortProperties,
        VisibleOwner = owner.VisibleOwner,
    };
}
