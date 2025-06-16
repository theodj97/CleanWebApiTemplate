using CleanWebApiTemplate.Application.Helpers.Validators;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
using FluentValidation;
using CustomMediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo;

public class TodoValidator<TCommand>(IBaseQueryRepository<TodoEntity> repository) : BaseAbstractValidator<TCommand> where TCommand : class, IRequest<object>
{
    private readonly IBaseQueryRepository<TodoEntity> repository = repository;

    /// <summary>
    /// Title validation. Lenght and Uniqueness.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task ValidateTitle(string title,
                                       ValidationContext<TCommand> context,
                                       CancellationToken cancellationToken)
    {
        if (title.Length > TodoEntityConfiguration.TitleLenght)
            AddFailure(context,
                       $"Property '{context.DisplayName}' max length is {TodoEntityConfiguration.TitleLenght}.");

        if (await TitleIsUnique(title, cancellationToken: cancellationToken) is false)
            AddFailure(context,
                       $"Property '{context.DisplayName}' is already in use!");
    }

    /// <summary>
    /// Title validation for updates. You recieve the ID of the element you want to update to not take it's title in the validation.
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    /// <param name="idAndTitleType"></param>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task ValidateTitle(IdAndTitleType idAndTitleType,
                                       ValidationContext<TCommand> context,
                                       CancellationToken cancellationToken)
    {
        if (idAndTitleType.Title.Length > TodoEntityConfiguration.TitleLenght)
            AddFailure(context,
                       $"Property '{nameof(idAndTitleType.Title)}' max length is {TodoEntityConfiguration.TitleLenght}.",
                       nameof(idAndTitleType.Title));

        if (await TitleIsUnique(idAndTitleType.Title, idAndTitleType.Id, cancellationToken) is false)
            AddFailure(context,
                       $"Property '{nameof(idAndTitleType.Title)}' is already in use!",
                       nameof(idAndTitleType.Title));
    }

    protected void ValidateDescription(string description,
                                       ValidationContext<TCommand> context)
    {
        if (description.Length > TodoEntityConfiguration.DescriptionLenght)
            AddFailure(context, $"Property '{context.DisplayName}' max length is {TodoEntityConfiguration.DescriptionLenght}.");
    }

    protected void ValidateUserEmail(string createdBy,
                                     ValidationContext<TCommand> context)
    {
        if (IsValidEmail(createdBy) is false)
            AddFailure(context, $"Property '{context.DisplayName}' is not a valid email.");
    }

    protected void ValidateStatus(int status,
                                  ValidationContext<TCommand> context)
    {
        if (Enum.IsDefined(typeof(ETodoStatus), status) is false)
            AddFailure(context, $"Property '{context.DisplayName}' wasn't a registered {nameof(ETodoStatus)}.");
    }

    /// <summary>
    /// Title uniqueness DB validation.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Validation result</returns>
    private async Task<bool> TitleIsUnique(string title,
                                           string? todoId = null,
                                           CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return true;

        if (todoId is null)
            return (await repository.FilterAsync(x => x.Title == title, cancellationToken: cancellationToken)).Count is 0;

        Ulid id = Ulid.Parse(todoId);
        return (await repository.FilterAsync(x => x.Title == title && x.Id != id, cancellationToken: cancellationToken)).Count is 0;
    }

    public struct IdAndTitleType(string? id, string? title)
    {
        public string Id = id ?? "";
        public string Title = title ?? "";
    }
}
