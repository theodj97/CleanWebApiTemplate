using CleanWebApiTemplate.Domain.Helpers.Validators;
using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Domain.Models.Enums;
using CleanWebApiTemplate.Domain.Models.Responses;
using CleanWebApiTemplate.Infrastructure.Common;
using CleanWebApiTemplate.Infrastructure.EntityConfiguration;
using FluentValidation;
using MediatR;

namespace CleanWebApiTemplate.Application.Handlers.Todo;

public class TodoValidator<T>(IBaseRepository<TodoEntity> repository) : BaseAbstractValidator<T> where T : class, IRequest<object>
{
    private readonly IBaseRepository<TodoEntity> repository = repository;

    /// <summary>
    /// Title validation.
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

        if (await TitleIsUnique(title, cancellationToken) is false)
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
        if (Enum.IsDefined(typeof(TodoStatusEnum), status) is false)
            AddFailure(context, "Property '{0}' wasn't a registered status.");
    }

    protected void ValidateTitleOrderProperty<TCommand>(string? orderBy, ValidationContext<TCommand> context) where TCommand : class, IRequest<object>
    {
        if (orderBy is null) return;

        var validProperties = typeof(TodoTitleResponse)
                                .GetProperties()
                                .Select(p => p.Name.ToLower())
                                .ToList();

        if (validProperties.Contains(orderBy.ToLower()) is false)
            AddFailure(context, "Property '{0}' isn't a valid order property.");
    }

    /// <summary>
    /// Title uniqueness DB validation.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Validation result</returns>
    private async Task<bool> TitleIsUnique(string title, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
            return true;

        return (await repository.FilterAsyncANT(x => x.Title == title, cancellationToken: cancellationToken)).Count is 0;
    }
}
