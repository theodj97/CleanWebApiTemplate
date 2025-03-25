namespace CleanWebApiTemplate.Infrastructure.Common;

public static class RepositoryHelper
{
    public static IQueryable<TValue> ManagePagination<TValue>(IQueryable<TValue> query, int? pageNumber, int? pageSize)
    {
        if (pageNumber is not null && pageSize is not null)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than or equal to 1.");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than or equal to 1.");

            query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return query;
    }
}
