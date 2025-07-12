using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.Helpers;

public static class UserDataExtensions
{
    public static IQueryable<T> WhereUser<T>(this IQueryable<T> query, string userName)
        where T : IUserData
    {
        return query.Where(x => x.UserName == userName);
    }

    public static IEnumerable<T> WhereUser<T>(this IEnumerable<T> query, string userName)
        where T : IUserData
    {
        return query.Where(x => x.UserName == userName);
    }

    // Special cases
    public static IQueryable<BookmarkTag> WhereUser(this IQueryable<BookmarkTag> query, string userName)
    {
        return query.Where(x => x.Bookmark.UserName == userName);
    }

    public static IEnumerable<BookmarkTag> WhereUser(this IEnumerable<BookmarkTag> query, string userName)
    {
        return query.Where(x => x.Bookmark.UserName == userName);
    }

    public static IQueryable<PhraseTag> WhereUser(this IQueryable<PhraseTag> query, string userName)
    {
        return query.Where(x => x.Phrase.UserName == userName);
    }

    public static IEnumerable<PhraseTag> WhereUser(this IEnumerable<PhraseTag> query, string userName)
    {
        return query.Where(x => x.Phrase.UserName == userName);
    }

    public static IQueryable<Tag> WhereUser(this IQueryable<Tag> query, string userName)
    {
        return query.Where(x => x.TagCategory.UserName == userName);
    }

    public static IEnumerable<Tag> WhereUser(this IEnumerable<Tag> query, string userName)
    {
        return query.Where(x => x.TagCategory.UserName == userName);
    }
}