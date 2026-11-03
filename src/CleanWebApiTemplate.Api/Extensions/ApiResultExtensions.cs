using System.Collections;
using CleanWebApiTemplate.Api.Models.Responses;
using CleanWebApiTemplate.Application.Results;
using Microsoft.AspNetCore.WebUtilities;

namespace CleanWebApiTemplate.Api.Extensions;

public static class ApiResultExtensions
{
    /// <summary>Maps a successful result to 200, or to 204 when it has no value.</summary>
    public static IResult ToResponse<TDto, TResponse>(this Result<TDto> result)
        where TResponse : class, IBaseResponse<TDto, TResponse>
        where TDto : class?
    {
        return result.ToResponseCore(res => Results.Ok(TResponse.ToResponseModel(res.Value)));
    }

    /// <summary>Maps a successful collection to 200, or to 204 when it is empty.</summary>
    public static IResult ToResponse<TDto, TResponse>(this Result<IEnumerable<TDto>> result)
        where TResponse : IBaseResponse<TDto, TResponse>
    {
        return result.ToResponseCore(res => Results.Ok((res.Value ?? []).Select(TResponse.ToResponseModel)));
    }

    /// <summary>Maps a successful scalar to 200, or to 204 when it is null.</summary>
    public static IResult ToResponse<T>(this Result<T> result)
    {
        return result.ToResponseCore(res => Results.Ok(res.Value));
    }

    /// <summary>Maps a successful result to 201 with a Location header. Failures keep their error mapping.</summary>
    public static IResult ToCreatedResponse<TDto, TResponse>(this Result<TDto> result,
                                                             Func<TDto, string> locationFactory)
        where TResponse : class, IBaseResponse<TDto, TResponse>
        where TDto : class?
    {
        if (result.IsFailure)
            return ResolveError(result.Error!);

        if (result.Value is null)
            throw new InvalidOperationException("A created result requires a value.");

        return Results.Created(ToUri(locationFactory, result.Value),
                               TResponse.ToResponseModel(result.Value));
    }

    private static IResult ToResponseCore<T>(this Result<T> result, Func<Result<T>, IResult> successAction)
    {
        if (result.IsFailure)
            return ResolveError(result.Error!);

        if (IsNoContent(result.Value))
            return Results.NoContent();

        return successAction(result);
    }

    private static bool IsNoContent(object? value)
    {
        if (value is null)
            return true;

        // An empty string is a value, not an empty collection.
        if (value is string)
            return false;

        if (value is not IEnumerable enumerable)
            return false;

        IEnumerator enumerator = enumerable.GetEnumerator();
        try
        {
            return enumerator.MoveNext() is false;
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    private static Uri? ToUri<T>(Func<T, string> locationFactory, T value)
    {
        string location = locationFactory(value);
        return string.IsNullOrWhiteSpace(location)
            ? null
            : new Uri(location, UriKind.RelativeOrAbsolute);
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
