using System.Linq.Expressions;
using System.Reflection;

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

    public static IQueryable<T> DynamicOrderBy<T>(this IQueryable<T> source,
                                                  params IEnumerable<KeyValuePair<string, bool>>? sortProperties)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (sortProperties is null || sortProperties.Any() is false) return source;

        var entityType = typeof(T);

        var parameter = Expression.Parameter(entityType, "x");

        bool isFirstSort = true;
        IQueryable<T> currentQuery = source;

        foreach (var sortProperty in sortProperties)
        {
            string propertyName = sortProperty.Key;
            bool descending = sortProperty.Value; // true for descending, false for ascending

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentException("Property name cannot be null or whitespace.", nameof(sortProperties));

            propertyName = propertyName.Trim();
            PropertyInfo propertyInfo = entityType.GetProperty(propertyName,
                                                               BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ?? throw new ArgumentException($"Property '{propertyName}' not found on type '{entityType.FullName}'.");

            Expression propertyAccess = Expression.Property(parameter, propertyInfo);

            var lambda = Expression.Lambda(propertyAccess, parameter);

            string methodName;
            if (isFirstSort)
            {
                methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
                isFirstSort = false;
            }
            else
                methodName = descending ? nameof(Queryable.ThenByDescending) : nameof(Queryable.ThenBy);

            MethodCallExpression resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [entityType, propertyInfo.PropertyType],
                currentQuery.Expression,
                Expression.Quote(lambda)
            );

            currentQuery = currentQuery.Provider.CreateQuery<T>(resultExpression);
        }

        return currentQuery ?? source;
    }
}
