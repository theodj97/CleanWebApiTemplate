using CleanWebApiTemplate.Api.Todo.Create;
using CleanWebApiTemplate.Api.Todo.Filter;
using CleanWebApiTemplate.Api.Todo.Get;
using CleanWebApiTemplate.Api.Todo.Update;
using CleanWebApiTemplate.Application.Todo;
using CleanWebApiTemplate.Domain.Todo;
using Microsoft.OpenApi;
using static CleanWebApiTemplate.Api.Configuration.OpenApiSchemaHelpers;

namespace CleanWebApiTemplate.Api.Todo;

/// <summary>Schema descriptions for the Todo contracts, plus the tag the routes use.</summary>
internal static class TodoOpenApiSchemas
{
    public static void DescribeSchemas(OpenApiDocument document)
    {
        document.Tags ??= new HashSet<OpenApiTag>();
        document.Tags.Add(new OpenApiTag
        {
            Name = "Todos",
            Description =
                "Create, read, filter, update and delete Todo items. Every operation requires a JWT; "
                + "the ownership and role rules are listed on each operation.",
        });

        if (document.Components?.Schemas is not { } schemas)
            return;

        Patch(schemas, nameof(CreateTodoRequest), schema =>
        {
            Describe(Property(schema, "title"),
                     "Short task description. Trimmed; unique per owner.",
                     maxLength: TodoConstraints.TitleMaxLength,
                     example: "Prepare the release notes");
            Describe(Property(schema, "description"),
                     "Optional details for the task.",
                     maxLength: TodoConstraints.DescriptionMaxLength,
                     example: "Include the migration notes and the changelog link.");
        });

        Patch(schemas, nameof(UpdateTodoRequest), schema =>
        {
            Describe(Property(schema, "title"),
                     "New title. Titles stay unique per owner.",
                     maxLength: TodoConstraints.TitleMaxLength,
                     example: "Publish the release notes");
            Describe(Property(schema, "description"),
                     "New details for the task.",
                     maxLength: TodoConstraints.DescriptionMaxLength,
                     example: "The draft is ready for review.");
            DescribeStatus<ETodoStatus>(Property(schema, "status"),
                                        "New status. Omit to leave it unchanged.",
                                        withAllowedValues: false);
        });

        Patch(schemas, nameof(FilteredTodoRequest), schema =>
        {
            Describe(ArrayItems(Property(schema, "ids")),
                     "Todo ids to include.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(ArrayItems(Property(schema, "titles")),
                     "Title fragments to match (contains).",
                     maxLength: TodoConstraints.TitleMaxLength,
                     example: "release");
            DescribeStatus<ETodoStatus>(ArrayItems(Property(schema, "statuses")), withAllowedValues: true);
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
            DescribeSort(Property(schema, "sortProperties"), TodoSortCatalog.Describe());
        });

        Patch(schemas, nameof(GetTodoTitlesRequest), schema =>
        {
            DescribePaging(schema);
            DescribeSort(Property(schema, "sortProperties"), TodoSortCatalog.Describe());
        });

        Patch(schemas, nameof(TodoResponse), schema =>
        {
            Describe(Property(schema, "id"),
                     "Todo identifier.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(Property(schema, "title"),
                     "Task title.",
                     maxLength: TodoConstraints.TitleMaxLength,
                     example: "Set up the local environment");
            Describe(Property(schema, "description"),
                     "Task details.",
                     maxLength: TodoConstraints.DescriptionMaxLength,
                     example: "Started with Aspire, PostgreSQL and locally signed JWTs.");
            Describe(Property(schema, "createdAt"),
                     "Creation timestamp (UTC).",
                     format: "date-time",
                     example: "2026-10-26T09:15:00Z");
            Describe(Property(schema, "updatedAt"),
                     "Last update timestamp (UTC).",
                     format: "date-time",
                     example: "2026-10-26T10:00:00Z");
            DescribeStatus<ETodoStatus>(Property(schema, "status"), withAllowedValues: true);
            Describe(Property(schema, "createdBy"),
                     "Owner email.",
                     format: "email",
                     example: "local.user@example.com");
            Describe(Property(schema, "updatedBy"),
                     "Email of the last writer.",
                     format: "email",
                     example: "local.user@example.com");
        });

        Patch(schemas, nameof(TodoTitleResponse), schema =>
        {
            Describe(Property(schema, "id"),
                     "Todo identifier.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(Property(schema, "title"),
                     "Task title.",
                     maxLength: TodoConstraints.TitleMaxLength,
                     example: "Set up the local environment");
        });
    }
}
