using CleanWebApiTemplate.Domain.Helpers.Validators;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums.Todo;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
using FluentValidation;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo;

public class TodoValidator<T>(IBaseRepository<TodoEntity> repository) : BaseAbstractValidator<T> where T : class, IRequest<object>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    /// <summary>
    /// Title validation. Lenght and Uniqueness.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected async Task ValidateTitle<TCommand>(string title,
                                                 ValidationContext<TCommand> context,
                                                 CancellationToken cancellationToken) where TCommand : class, IRequest<object>
    {
        if (title.Length > TodoEntityConfiguration.TitleLenght)
            AddFailure(context, "Property '{0}' max length is {1}.", TodoEntityConfiguration.TitleLenght);

        if (await TitleIsUnique(title, cancellationToken: cancellationToken) is false)
            AddFailure(context, "Property '{0}' must be unique!");
    }

    protected async Task ValidateTitle<TCommand>(IdAndTitleType idAndTitleType,
                                                 ValidationContext<TCommand> context,
                                                 CancellationToken cancellationToken) where TCommand : class, IRequest<object>
    {
        if (idAndTitleType.Title.Length > TodoEntityConfiguration.TitleLenght)
            AddFailure(context, "Property '{0}' max length is {1}.", TodoEntityConfiguration.TitleLenght);

        if (await TitleIsUnique(idAndTitleType.Title, idAndTitleType.Id, cancellationToken) is false)
            AddFailure(context, "Property '{0}' must be unique!");
    }

    protected void ValidateDescription<TCommand>(string description,
                                                 ValidationContext<TCommand> context) where TCommand : class, IRequest<object>
    {
        if (description.Length > TodoEntityConfiguration.DescriptionLenght)
            AddFailure(context, "Property '{0}' max length is {1}.", TodoEntityConfiguration.DescriptionLenght);
    }

    protected void ValidateUserEmail<TCommand>(string createdBy,
                                               ValidationContext<TCommand> context) where TCommand : class, IRequest<object>
    {
        if (IsValidEmail(createdBy) is false)
            AddFailure(context, "Property '{0}' is not a valid email.");
    }

    protected void ValidateStatus<TCommand>(int status,
                                            ValidationContext<TCommand> context) where TCommand : class, IRequest<object>
    {
        if (Enum.IsDefined(typeof(ETodoStatus), status) is false)
            AddFailure(context, "Property '{0}' wasn't a registered {1}.", status, nameof(ETodoStatus));
    }

    protected void ValidTodoOrderBy<TCommand>(byte? orderBy,
                                              ValidationContext<TCommand> context) where TCommand : class, IRequest<object>
    {
        if (orderBy is null) return;

        if (Enum.IsDefined(typeof(ETodoOrderBy), (int)orderBy.Value) is false)
            AddFailure(context, "Property '{0}' wasn't a registered {1} property.", orderBy, nameof(ETodoOrderBy));
    }

    /// <summary>
    /// Title uniqueness DB validation.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Validation result</returns>
    private async Task<bool> TitleIsUnique(string title, string? todoId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return true;

        if (todoId is null)
            return (await repository.FilterAsyncANT(x => x.Title == title, cancellationToken: cancellationToken)).Count is 0;

        Ulid id = Ulid.Parse(todoId);
        return (await repository.FilterAsyncANT(x => x.Title == title && x.Id != id, cancellationToken: cancellationToken)).Count is 0;
    }

    public class IdAndTitleType(string? id, string? title)
    {
        public string Id = id ?? "";
        public string Title = title ?? "";
    }
}
