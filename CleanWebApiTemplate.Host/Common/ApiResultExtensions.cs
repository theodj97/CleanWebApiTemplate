using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Host.Routes.ResponseModels;
using Microsoft.AspNetCore.WebUtilities;

namespace CleanWebApiTemplate.Host.Common;

public static class ApiResultExtensions
{
    public static IResult ToResponse<TDto, TResponse>(this Result<TDto> result)
        where TResponse : IBaseResponse<TDto, TResponse>
    {
        if (result.IsSuccess)
            return ResolveSuccess(result.Value, result.IsCreated, result.IsNoContent, TResponse.ToResponse);

        return ResolveError(result.Error!);
    }

    public static IResult ToResponse<TDto, TResponse>(this Result<IEnumerable<TDto>> result)
        where TResponse : IBaseResponse<TDto, TResponse>
    {
        if (result.IsSuccess)
            return ResolveSuccess(result.Value, result.IsCreated, result.IsNoContent, items => items.Select(TResponse.ToResponse));

        return ResolveError(result.Error!);
    }

    private static IResult ResolveSuccess<TValue, TResponseModel>(
    TValue? value,
    bool isCreated,
    bool isNoContent,
    Func<TValue, TResponseModel> converter)
    {
        if (isNoContent)
            return Results.NoContent();

        var response = converter(value!);
        return isCreated
            ? Results.Created(string.Empty, response)
            : Results.Ok(response);
    }

    private static IResult ResolveError(Error error) => error switch
    {
        BadRequestError => ResolveProblemDetail(StatusCodes.Status400BadRequest, error),
        DomainError => ResolveProblemDetail(StatusCodes.Status400BadRequest, error),
        NotFoundError => ResolveProblemDetail(StatusCodes.Status404NotFound, error),
        ConflictError => ResolveProblemDetail(StatusCodes.Status409Conflict, error),
        UnauthorizedError => Results.Unauthorized(),
        ForbiddenError => Results.Forbid(),
        _ => throw new ArgumentOutOfRangeException(nameof(error), $"Unhandled error type: {error!.GetType().Name}")
    };

    private static IResult ResolveProblemDetail(int statusCode, Error error) =>
        Results.Problem(title: error.Title ?? ReasonPhrases.GetReasonPhrase(statusCode),
                        detail: error.Description,
                        statusCode: statusCode,
                        type: error.GetType().Name);
}
