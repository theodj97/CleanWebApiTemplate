using CleanWebApiTemplate.Api.Common.Responses;
using CleanWebApiTemplate.Api.Configuration;
using CleanWebApiTemplate.Application.Common.Results;

namespace CleanWebApiTemplate.Api.Extensions;

/// <summary>
/// The single conversion from <see cref="Result{T}"/> to HTTP: success becomes the documented 2xx, every
/// <see cref="Error"/> becomes the ProblemDetails body its type maps to.
/// </summary>
public static class ApiResultExtensions
{
    /// <summary>Maps a successful result to 200 with the response model.</summary>
    public static IResult ToResponse<TDto, TResponse>(this Result<TDto> result, HttpContext context)
        where TDto : class
        where TResponse : class, IBaseResponse<TDto, TResponse>
    {
        if (result.IsFailure)
            return ResolveError(result.Error!, context);

        return result.Value is null
            ? MissingValue(context)
            : Results.Ok(TResponse.ToResponseModel(result.Value));
    }

    /// <summary>Maps a successful collection to 200, with an empty array when nothing matched.</summary>
    public static IResult ToResponse<TDto, TResponse>(this Result<IEnumerable<TDto>> result, HttpContext context)
        where TDto : class
        where TResponse : IBaseResponse<TDto, TResponse>
    {
        if (result.IsFailure)
            return ResolveError(result.Error!, context);

        return result.Value is null
            ? MissingValue(context)
            : Results.Ok(result.Value.Select(TResponse.ToResponseModel));
    }

    /// <summary>Maps a successful result to 201 with a Location header.</summary>
    public static IResult ToCreatedResponse<TDto, TResponse>(this Result<TDto> result,
                                                             HttpContext context,
                                                             Func<TDto, string> locationFactory)
        where TDto : class
        where TResponse : class, IBaseResponse<TDto, TResponse>
    {
        if (result.IsFailure)
            return ResolveError(result.Error!, context);

        return result.Value is null
            ? MissingValue(context)
            : Results.Created(ToUri(locationFactory, result.Value), TResponse.ToResponseModel(result.Value));
    }

    /// <summary>Maps a successful result to 204.</summary>
    public static IResult ToNoContentResponse<T>(this Result<T> result, HttpContext context) =>
        result.IsFailure ? ResolveError(result.Error!, context) : Results.NoContent();

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
        _ => throw new ArgumentOutOfRangeException(nameof(error), $"Unhandled error type: {error.GetType().Name}"),
    };

    // A success with no value is a bug in the use case, not a client error: report it as 500 rather than
    // serializing null or throwing a NullReferenceException further up.
    private static IResult MissingValue(HttpContext context) =>
        Results.Problem(ProblemDetailsFactory.Create(context,
                                                     StatusCodes.Status500InternalServerError,
                                                     "The use case returned no value."));

    private static Uri? ToUri<T>(Func<T, string> locationFactory, T value)
    {
        string location = locationFactory(value);
        return string.IsNullOrWhiteSpace(location)
            ? null
            : new Uri(location, UriKind.RelativeOrAbsolute);
    }
}
