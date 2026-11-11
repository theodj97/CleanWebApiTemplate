using CleanWebApiTemplate.Api.Project.Create;
using CleanWebApiTemplate.Api.Project.Filter;
using CleanWebApiTemplate.Api.Project.Get;
using CleanWebApiTemplate.Api.Project.Update;
using CleanWebApiTemplate.Application.Project;
using CleanWebApiTemplate.Domain.Project;
using Microsoft.OpenApi;
using static CleanWebApiTemplate.Api.Configuration.OpenApiSchemaHelpers;

namespace CleanWebApiTemplate.Api.Project;

/// <summary>Schema descriptions for the Project contracts, plus the tag the routes use.</summary>
internal static class ProjectOpenApiSchemas
{
    public static void DescribeSchemas(OpenApiDocument document)
    {
        document.Tags ??= new HashSet<OpenApiTag>();
        document.Tags.Add(new OpenApiTag
        {
            Name = "Projects",
            Description =
                "Create, read, filter, update and delete Project items. Every operation requires a JWT; "
                + "the ownership and role rules are listed on each operation.",
        });

        if (document.Components?.Schemas is not { } schemas)
            return;

        Patch(schemas, nameof(CreateProjectRequest), schema =>
        {
            Describe(Property(schema, "name"),
                     "Short task description. Trimmed; unique per owner.",
                     maxLength: ProjectConstraints.NameMaxLength,
                     example: "Prepare the release notes");
            Describe(Property(schema, "description"),
                     "Optional details for the task.",
                     maxLength: ProjectConstraints.DescriptionMaxLength,
                     example: "Include the migration notes and the changelog link.");
        });

        Patch(schemas, nameof(UpdateProjectRequest), schema =>
        {
            Describe(Property(schema, "name"),
                     "New name. Names stay unique per owner.",
                     maxLength: ProjectConstraints.NameMaxLength,
                     example: "Publish the release notes");
            Describe(Property(schema, "description"),
                     "New details for the task.",
                     maxLength: ProjectConstraints.DescriptionMaxLength,
                     example: "The draft is ready for review.");
            DescribeStatus<EProjectStatus>(Property(schema, "status"),
                                        "New status. Omit to leave it unchanged.",
                                        withAllowedValues: false);
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
            DescribeStatus<EProjectStatus>(ArrayItems(Property(schema, "statuses")), withAllowedValues: true);
            Describe(ArrayItems(Property(schema, "owners")),
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
            DescribeSort(Property(schema, "sortProperties"), ProjectSortCatalog.Describe());
        });

        Patch(schemas, nameof(GetProjectNamesRequest), schema =>
        {
            DescribePaging(schema);
            DescribeSort(Property(schema, "sortProperties"), ProjectSortCatalog.Describe());
        });

        Patch(schemas, nameof(ProjectResponse), schema =>
        {
            Describe(Property(schema, "id"),
                     "Project identifier.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(Property(schema, "name"),
                     "Task name.",
                     maxLength: ProjectConstraints.NameMaxLength,
                     example: "Set up the local environment");
            Describe(Property(schema, "description"),
                     "Task details.",
                     maxLength: ProjectConstraints.DescriptionMaxLength,
                     example: "Started with Aspire, PostgreSQL and locally signed JWTs.");
            Describe(Property(schema, "createdAt"),
                     "Creation timestamp (UTC).",
                     format: "date-time",
                     example: "2026-10-26T09:15:00Z");
            Describe(Property(schema, "updatedAt"),
                     "Last update timestamp (UTC).",
                     format: "date-time",
                     example: "2026-10-26T10:00:00Z");
            DescribeStatus<EProjectStatus>(Property(schema, "status"), withAllowedValues: true);
            Describe(Property(schema, "createdBy"),
                     "Owner email.",
                     format: "email",
                     example: "local.user@example.com");
            Describe(Property(schema, "updatedBy"),
                     "Email of the last writer.",
                     format: "email",
                     example: "local.user@example.com");
        });

        Patch(schemas, nameof(ProjectNameResponse), schema =>
        {
            Describe(Property(schema, "id"),
                     "Project identifier.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(Property(schema, "name"),
                     "Task name.",
                     maxLength: ProjectConstraints.NameMaxLength,
                     example: "Set up the local environment");
        });
    }
}
