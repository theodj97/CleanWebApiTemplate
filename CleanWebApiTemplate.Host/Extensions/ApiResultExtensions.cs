using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Host.Models.Responses;
using Microsoft.AspNetCore.WebUtilities;

namespace CleanWebApiTemplate.Host.Extensions;

public static class ApiResultExtensions
{
    public static IResult ToResponse<TDto, TResponse>(this Result<TDto> result)
        where TResponse : class, IBaseResponse<TDto, TResponse>
        where TDto : class?
    {
        return result.ToResponseCore(res => res.IsCreated
            ? Results.Created(string.Empty, TResponse.ToResponseModel(res.Value))
            : Results.Ok(TResponse.ToResponseModel(res.Value)));
    }

    public static IResult ToResponse<TDto, TResponse>(this Result<IEnumerable<TDto>> result)
        where TResponse : IBaseResponse<TDto, TResponse>
    {
        return result.ToResponseCore(res => res.IsCreated
            ? Results.Created(string.Empty, (result.Value ?? []).Select(TResponse.ToResponseModel))
            : Results.Ok((result.Value ?? []).Select(TResponse.ToResponseModel)));
    }

    public static IResult ToResponse(this Result<bool> result)
    {
        return result.ToResponseCore(res => res.IsCreated
            ? Results.Created(string.Empty, res.Value)
            : Results.Ok(res.Value));
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

    private static IResult ToResponseCore<T>(this Result<T> result, Func<Result<T>, IResult> successAction)
    {
        if (result.IsFailure)
            return ResolveError(result.Error!);

        if (result.IsNoContent)
            return Results.NoContent();

        return successAction(result);
    }
}
