using System.Linq.Expressions;

namespace CleanWebApiTemplate.Infrastructure.Common;

public static class RepositoryExtension
{
    public static IQueryable<TValue> ManagePagination<TValue>(this IQueryable<TValue> query,
                                                              int? pageNumber,
                                                              int? pageSize)
    {
        if (pageNumber is not null && pageSize is not null)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber),
                                                      "Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                                                      "Page size must be greater than or equal to 1.");

            query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return query;
    }

    public static IQueryable<T> OrderElementsBy<T>(this IQueryable<T> query,
                                                   Expression<Func<T, object>>? orderBy,
                                                   bool descending = false)
    {
        if (orderBy is not null)
            query = descending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        return query;
    }

    /// <summary>
    /// Orders the query by the given property names without using reflection (Native AOT friendly).
    /// The caller provides the set of sortable properties as pre-compiled key selector expressions.
    /// </summary>
    /// <param name="source">The query to order.</param>
    /// <param name="sortProperties">Property name / descending pairs, in order of precedence.</param>
    /// <param name="sortableProperties">Map of sortable property names (case-insensitive) to their key selectors.</param>
    public static IQueryable<T> DynamicOrderBy<T>(this IQueryable<T> source,
                                                  IEnumerable<KeyValuePair<string, bool>>? sortProperties,
                                                  IReadOnlyDictionary<string, Expression<Func<T, object>>> sortableProperties)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sortableProperties);

        if (sortProperties is null || sortProperties.Any() is false) return source;

        bool isFirstSort = true;
        IQueryable<T> currentQuery = source;

        foreach (var sortProperty in sortProperties)
        {
            string propertyName = sortProperty.Key;
            bool descending = sortProperty.Value; // true for descending, false for ascending

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or whitespace.", nameof(sortProperties));

            Expression<Func<T, object>> keySelector = sortableProperties.TryGetValue(propertyName.Trim(), out var selector)
                ? selector
                : throw new ArgumentException($"Property '{propertyName}' not found on type '{typeof(T).FullName}'.");

            if (isFirstSort)
            {
                currentQuery = descending ? currentQuery.OrderByDescending(keySelector) : currentQuery.OrderBy(keySelector);
                isFirstSort = false;
            }
            else
            {
                IOrderedQueryable<T> orderedQuery = (IOrderedQueryable<T>)currentQuery;
                currentQuery = descending ? orderedQuery.ThenByDescending(keySelector) : orderedQuery.ThenBy(keySelector);
            }
        }

        return currentQuery ?? source;
    }
}
