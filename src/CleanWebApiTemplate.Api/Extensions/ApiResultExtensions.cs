using System.Collections;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Api.Models.Responses;
using CleanWebApiTemplate.Application.Results;

namespace CleanWebApiTemplate.Api.Extensions;

public static class ApiResultExtensions
{
    /// <summary>Maps a successful result to 200, or to 204 when it has no value.</summary>
    public static IResult ToResponse<TDto, TResponse>(this Result<TDto> result, HttpContext context)
        where TResponse : class, IBaseResponse<TDto, TResponse>
        where TDto : class?
    {
        return result.ToResponseCore(context, res => Results.Ok(TResponse.ToResponseModel(res.Value)));
    }

    /// <summary>Maps a successful collection to 200, or to 204 when it is empty.</summary>
    public static IResult ToResponse<TDto, TResponse>(this Result<IEnumerable<TDto>> result, HttpContext context)
        where TResponse : IBaseResponse<TDto, TResponse>
    {
        return result.ToResponseCore(context,
                                     res => Results.Ok((res.Value ?? []).Select(TResponse.ToResponseModel)));
    }

    /// <summary>Maps a successful scalar to 200, or to 204 when it is null.</summary>
    public static IResult ToResponse<T>(this Result<T> result, HttpContext context)
    {
        return result.ToResponseCore(context, res => Results.Ok(res.Value));
    }

    /// <summary>Maps a successful result to 201 with a Location header. Failures keep their error mapping.</summary>
    public static IResult ToCreatedResponse<TDto, TResponse>(this Result<TDto> result,
                                                             HttpContext context,
                                                             Func<TDto, string> locationFactory)
        where TResponse : class, IBaseResponse<TDto, TResponse>
        where TDto : class?
    {
        if (result.IsFailure)
            return ResolveError(result.Error!, context);

        if (result.Value is null)
            throw new InvalidOperationException("A created result requires a value.");

        return Results.Created(ToUri(locationFactory, result.Value),
                               TResponse.ToResponseModel(result.Value));
    }

    private static IResult ToResponseCore<T>(this Result<T> result,
                                             HttpContext context,
                                             Func<Result<T>, IResult> successAction)
    {
        if (result.IsFailure)
            return ResolveError(result.Error!, context);

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

    private static IResult ResolveError(Error error, HttpContext context) => error switch
    {
        BadRequestError or DomainError => Results.Problem(
            ProblemDetailsFactory.Create(context, StatusCodes.Status400BadRequest, error.Description, error.Title)),
        NotFoundError => Results.Problem(
            ProblemDetailsFactory.Create(context, StatusCodes.Status404NotFound, error.Description, error.Title)),
        ConflictError => Results.Problem(
            ProblemDetailsFactory.Create(context, StatusCodes.Status409Conflict, error.Description, error.Title)),
        UnauthorizedError => Results.Problem(
            ProblemDetailsFactory.Create(context, StatusCodes.Status401Unauthorized, error.Description, error.Title)),
        ForbiddenError => Results.Problem(
            ProblemDetailsFactory.Create(context, StatusCodes.Status403Forbidden, error.Description, error.Title)),
        _ => throw new ArgumentOutOfRangeException(nameof(error), $"Unhandled error type: {error!.GetType().Name}")
    };
}
