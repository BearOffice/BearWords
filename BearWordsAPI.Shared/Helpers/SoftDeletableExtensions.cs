using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.Helpers;

public static class SoftDeletableExtensions
{
    public static IQueryable<T> WhereNotDeleted<T>(this IQueryable<T> query)
        where T : ISoftDeletable
    {
        return query.Where(x => !x.DeleteFlag);
    }

    public static IQueryable<T> WhereDeleted<T>(this IQueryable<T> query)
        where T : ISoftDeletable
    {
        return query.Where(x => x.DeleteFlag);
    }

    public static IQueryable<T> IncludeDeleted<T>(this IQueryable<T> query)
        where T : ISoftDeletable
    {
        return query;
    }

    public static IEnumerable<T> WhereNotDeleted<T>(this IEnumerable<T> query)
        where T : ISoftDeletable
    {
        return query.Where(x => !x.DeleteFlag);
    }

    public static IEnumerable<T> WhereDeleted<T>(this IEnumerable<T> query)
        where T : ISoftDeletable
    {
        return query.Where(x => x.DeleteFlag);
    }

    public static IEnumerable<T> IncludeDeleted<T>(this IEnumerable<T> query)
        where T : ISoftDeletable
    {
        return query;
    }

    public static void SetDeleteFlag(this ISoftDeletable entity)
    {
        entity.DeleteFlag = true;
    }

    public static void UnsetDeleteFlag(this ISoftDeletable entity)
    {
        entity.DeleteFlag = false;
    }
}