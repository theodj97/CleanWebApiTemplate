using FluentValidation;

namespace CleanWebApiTemplate.Application.Handlers.Todo;

public class TodoValidator<T> : AbstractValidator<T> where T : class
{
    internal const byte MaxTitleLength = 15;
    internal const byte MaxCreatedByLength = 40;
    internal const byte MaxDescriptionLength = 150;
}
