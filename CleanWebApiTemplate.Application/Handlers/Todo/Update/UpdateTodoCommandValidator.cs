﻿using CleanWebApiTemplate.Domain.Models.Entities;
using CleanWebApiTemplate.Infrastructure.Common;
using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo.Update;
public class UpdateTodoCommandValidator : TodoValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator(IBaseRepository<TodoEntity> repository) : base(repository)
    {
        RuleFor(x => x.Id)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUlid);

        RuleFor(x => x.Title)
            .CustomAsync(ValidateTitle!)
            .When(x => string.IsNullOrEmpty(x.Title) is false);

        RuleFor(x => x.UpdatedBy)
            .Custom(NotNullNotEmpty)
            .Custom(ValidateUserEmail);

        RuleFor(x => x.Description)
            .Custom(ValidateDescription!)
            .When(x => string.IsNullOrEmpty(x.Description) is false);

        RuleFor(x => (int)x.Status!)
            .Custom(ValidateStatus)
            .When(x => x.Status is not null || x.Status != 0);
    }
}
