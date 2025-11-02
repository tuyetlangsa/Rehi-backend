namespace Rehi.Application.Abstraction.QueryExtension;

public static class QueryableExtension
{
    public static IQueryable<TEntity> Applypagination<TEntity>(this IQueryable<TEntity> items, int pageNumber,
        int pageSize)
    {
        return items.Skip((pageNumber - 1) * pageSize).Take(pageSize);
    }
}