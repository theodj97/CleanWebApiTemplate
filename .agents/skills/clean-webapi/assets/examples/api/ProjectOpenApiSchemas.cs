using System.Text.Json.Nodes;
using {App}.Api.Models.Responses.Project;
using {App}.Api.Routes.Project.Create;
using {App}.Api.Routes.Project.Filter;
using {App}.Application.Project.Sorting;
using {App}.Domain.Models.Enums.Project;
using {App}.Domain.Models.Project;
using Microsoft.OpenApi;
using static {App}.Api.Configuration.OpenApiSchemaHelpers;

namespace {App}.Api.Configuration;

/// <summary>Schema descriptions for the Project contracts.</summary>
internal static class ProjectOpenApiSchemas
{
    public static void DescribeSchemas(OpenApiDocument document)
    {
        if (document.Components?.Schemas is not { } schemas)
            return;

        Patch(schemas, nameof(CreateProjectRequest), schema =>
        {
            Describe(Property(schema, "name"),
                     "Project name. Trimmed; unique per owner.",
                     maxLength: ProjectConstraints.NameMaxLength,
                     example: "Release 1.0");
            Describe(Property(schema, "description"),
                     "Optional details for the project.",
                     maxLength: ProjectConstraints.DescriptionMaxLength,
                     example: "Ship the first public release.");
        });

        Patch(schemas, nameof(FilteredProjectRequest), schema =>
        {
            Describe(ArrayItems(Property(schema, "ids")),
                     "Project ids to include.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(ArrayItems(Property(schema, "names")),
                     "Name fragments to match (contains).",
                     maxLength: ProjectConstraints.NameMaxLength,
                     example: "release");
            DescribeProjectStatus(ArrayItems(Property(schema, "statuses")),
                                  "Statuses to include.");
            Describe(ArrayItems(Property(schema, "createdBys")),
                     "Owner emails. Admin only; other roles are always scoped to their own email.",
                     format: "email",
                     example: "local.user@example.com");
            Describe(Property(schema, "startDate"),
                     "Inclusive lower bound for CreatedAt.",
                     format: "date-time",
                     example: "2026-01-01T00:00:00Z");
            Describe(Property(schema, "endDate"),
                     "Inclusive upper bound for CreatedAt.",
                     format: "date-time",
                     example: "2026-12-31T23:59:59Z");

            DescribePaging(schema);
            OpenApiSchema? sort = Property(schema, "sortProperties");
            DescribeSort(sort);
            if (sort is not null)
            {
                // DescribeSort carries the Todo catalog; the Project feature owns its own vocabulary.
                sort.Description =
                    $"Sort keys and directions, applied in order. Supported keys: {ProjectSortCatalog.Describe()}.";
            }
        });

        Patch(schemas, nameof(ProjectResponse), schema =>
        {
            Describe(Property(schema, "id"),
                     "Project identifier.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(Property(schema, "name"),
                     "Project name.",
                     maxLength: ProjectConstraints.NameMaxLength,
                     example: "Release 1.0");
            Describe(Property(schema, "description"),
                     "Project details.",
                     maxLength: ProjectConstraints.DescriptionMaxLength,
                     example: "Ship the first public release.");
            Describe(Property(schema, "createdAt"),
                     "Creation timestamp (UTC).",
                     format: "date-time",
                     example: "2026-10-26T09:15:00Z");
            DescribeProjectStatus(Property(schema, "status"));
            Describe(Property(schema, "createdBy"),
                     "Owner email.",
                     format: "email",
                     example: "local.user@example.com");
            Describe(Property(schema, "updatedBy"),
                     "Email of the last writer.",
                     format: "email",
                     example: "local.user@example.com");
        });
    }

    /// <summary>
    /// The shared DescribeStatus derives its values from ETodoStatus. Project has the same numeric
    /// values but its own names, so this feature sets the description and the enum from EProjectStatus.
    /// </summary>
    private static void DescribeProjectStatus(OpenApiSchema? schema, string? description = null)
    {
        if (schema is null)
            return;

        string values = string.Join(", ", Enum.GetValues<EProjectStatus>()
                                                    .Select(status => $"{(int)status} {status}"));
        schema.Description = description is null ? $"Status: {values}." : $"{description} Values: {values}.";
        schema.Example = JsonValue.Create((int)EProjectStatus.Draft);
        schema.Enum = [.. Enum.GetValues<EProjectStatus>().Select(status => JsonValue.Create((int)status))];
    }
}
