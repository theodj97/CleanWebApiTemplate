using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
using FluentValidation;
using CleanWebApiTemplate.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using CleanWebApiTemplate.Application.Abstractions.Messages;
using CleanWebApiTemplate.Application.Abstractions;

namespace CleanWebApiTemplate.Application.Handlers.Todo;

public class TodoValidator<TMessage>(SqlDbContext dbContext) : BaseAbstractValidator<TMessage> where TMessage : class, IMessage
{
    private readonly SqlDbContext dbContext = dbContext;

    protected async Task ValidateTitle(string title,
                                       ValidationContext<TMessage> context,
                                       CancellationToken cancellationToken)
    {
        if (title.Length > TodoEntityConfiguration.TitleLenght)
            AddFailure(context,
                       $"Property '{context.DisplayName}' max length is {TodoEntityConfiguration.TitleLenght}.");

        if (await TitleIsUnique(title, cancellationToken: cancellationToken) is false)
            AddFailure(context,
                       $"Property '{context.DisplayName}' is already in use!");
    }

    protected async Task ValidateTitle(IdAndTitle idAndTitle,
                                       ValidationContext<TMessage> context,
                                       CancellationToken cancellationToken)
    {
        var id = idAndTitle.Id;
        if (id is null) AddFailure(context, $"Property '{context.DisplayName}' must be provided.", nameof(IdAndTitle.Id));
        var title = idAndTitle.Title;
        if (title is null) AddFailure(context, $"Property '{context.DisplayName}' must be provided.", nameof(IdAndTitle.Title));

        if (title!.Length > TodoEntityConfiguration.TitleLenght)
            AddFailure(context,
                       $"Property '{nameof(title)}' max length is {TodoEntityConfiguration.TitleLenght}.",
                       nameof(title));

        if (await TitleIsUnique(title, id, cancellationToken) is false)
            AddFailure(context,
                       $"Property '{nameof(title)}' is already in use!",
                       nameof(title));
    }

    protected void ValidateDescription(string description,
                                       ValidationContext<TMessage> context)
    {
        if (description.Length > TodoEntityConfiguration.DescriptionLenght)
            AddFailure(context, $"Property '{context.DisplayName}' max length is {TodoEntityConfiguration.DescriptionLenght}.");
    }

    protected void ValidateUserEmail(string createdBy,
                                     ValidationContext<TMessage> context)
    {
        if (IsValidEmail(createdBy) is false)
            AddFailure(context, $"Property '{context.DisplayName}' is not a valid email.");
    }

    protected void ValidateStatus(int status,
                                  ValidationContext<TMessage> context)
    {
        if (Enum.IsDefined(typeof(ETodoStatus), status) is false)
            AddFailure(context, $"Property '{context.DisplayName}' wasn't a registered {nameof(ETodoStatus)}.");
    }

    private async Task<bool> TitleIsUnique(string title,
                                           string? todoId = null,
                                           CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return true;

        if (todoId is null)
            return (await dbContext.TodoDb.Where(x => x.Title == title).ToListAsync(cancellationToken)).Count is 0;

        Ulid id = Ulid.Parse(todoId);
        return (await dbContext.TodoDb.Where(x => x.Title == title && x.Id != id).ToListAsync(cancellationToken)).Count is 0;
    }
}

/// <summary>
/// Strongly-typed input for Id/Title cross-property validation.
/// Replaces the previous anonymous type + dynamic parameter (not Native AOT compatible).
/// </summary>
public sealed record IdAndTitle(string? Id, string? Title);