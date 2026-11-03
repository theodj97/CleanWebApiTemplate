using CleanWebApiTemplate.Api.Models.Responses.Todo;
using CleanWebApiTemplate.Api.Routes.Todo.Create;
using CleanWebApiTemplate.Api.Routes.Todo.Filter;
using CleanWebApiTemplate.Api.Routes.Todo.Get;
using CleanWebApiTemplate.Api.Routes.Todo.Update;
using CleanWebApiTemplate.Domain.Models.Todo;
using Microsoft.OpenApi;
using static CleanWebApiTemplate.Api.Configuration.OpenApiSchemaHelpers;

namespace CleanWebApiTemplate.Api.Configuration;

/// <summary>Schema descriptions for the Todo contracts.</summary>
internal static class TodoOpenApiSchemas
{
    public static void DescribeSchemas(OpenApiDocument document)
    {
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
            DescribeStatus(Property(schema, "status"),
                           "New status. Omit to leave it unchanged.",
                           withAllowedValues: false);
        });

        Patch(schemas, nameof(FilteredTodoRequest), schema =>
        {
            Describe(ArrayItems(Property(schema, "ids")),
                     "TODO ids to include.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(ArrayItems(Property(schema, "title")),
                     "Title fragments to match (contains).",
                     maxLength: TodoConstraints.TitleMaxLength,
                     example: "release");
            DescribeStatus(ArrayItems(Property(schema, "status")), withAllowedValues: true);
            Describe(ArrayItems(Property(schema, "createdBy")),
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
            DescribeSort(Property(schema, "sortProperties"));
        });

        Patch(schemas, nameof(GetTodoTitlesRequest), schema =>
        {
            DescribePaging(schema);
            DescribeSort(Property(schema, "sortProperties"));
        });

        Patch(schemas, nameof(TodoResponse), schema =>
        {
            Describe(Property(schema, "id"),
                     "TODO identifier.",
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
            DescribeStatus(Property(schema, "status"), withAllowedValues: true);
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
                     "TODO identifier.",
                     format: "ulid",
                     example: "01HWXHRB80000G000000000001");
            Describe(Property(schema, "title"),
                     "Task title.",
                     maxLength: TodoConstraints.TitleMaxLength,
                     example: "Set up the local environment");
        });
    }
}
