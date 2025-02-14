using CleanWebApiTemplate.Domain.ResultModel;
using FluentValidation;
using MediatR;

namespace CleanWebApiTemplate.Application.Behaviours;

public sealed class FluentValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly IEnumerable<IValidator<TRequest>> validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.ValidateAsync(context))
            .SelectMany(result => result.Result.Errors)
            .Where(f => f is not null)
            .Select(x => x.ErrorMessage)
            .Distinct()
            .ToList();

        if (failures is not null && failures!.Count != 0)
        {
            var failuresMessages = string.Join('\n', failures);
            var error = new BadRequestError("Validation Errors", string.Join("\n", failuresMessages));

            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = resultType.GetGenericArguments()[0];
                var result = typeof(Result<>)
                    .MakeGenericType(valueType)
                    .GetMethod(nameof(Result<int>.Failure))!
                    .Invoke(null, [error]);

                return (result as TResponse) ?? throw new Exception("Could not invoke Failure method in Result<> class.");
            }

            throw new InvalidOperationException("ValidationBehavior expected a TResponse of type Result<T>");
        }
        return await next();
    }
}