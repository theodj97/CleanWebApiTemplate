using CleanWebApiTemplate.Domain.ResultModel;
using CleanWebApiTemplate.Host.ResponseModels;
using Microsoft.AspNetCore.WebUtilities;

namespace CleanWebApiTemplate.Host.Common;

public static class ApiResultExtensions
{
    public static IResult ToResponse<TDto, TResponse>(this Result<TDto> result)
        where TResponse : class, IBaseResponse<TDto, TResponse>
        where TDto : class?
    {
        if (result.IsFailure) return ResolveError(result.Error!);
        if (result.IsNoContent) return Results.NoContent();

        var response = TResponse.ToResponseModel(result.Value);
        if (result.IsCreated) return Results.Created(string.Empty, response);
        else return Results.Ok(response);
    }

    public static IResult ToResponse<TDto, TResponse>(this Result<IEnumerable<TDto>> result)
        where TResponse : IBaseResponse<TDto, TResponse>
    {
        if (result.IsFailure) return ResolveError(result.Error!);
        if (result.IsNoContent) return Results.NoContent();

        var response = result.Value?.Select(TResponse.ToResponseModel);
        if (result.IsCreated) return Results.Created(string.Empty, response);
        else return Results.Ok(response);
    }

    public static IResult ToResponse(this Result<bool> result)
    {
        if (result.IsFailure) return ResolveError(result.Error!);
        if (result.IsNoContent) return Results.NoContent();

        if (result.IsCreated) return Results.Created(string.Empty, result.Value);
        else return Results.Ok(result.Value);
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
