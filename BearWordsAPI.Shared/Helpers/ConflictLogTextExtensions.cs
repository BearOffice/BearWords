using BearWordsAPI.Shared.Data.Models;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BearWordsAPI.Shared.Helpers;

public static class ConflictLogTextExtensions
{
    private static readonly JsonSerializerOptions _option = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
    };

    //public static string ReadableSerialize(this object obj)
    //{
    //    return JsonSerializer.Serialize(obj, _option);
    //}

    public static string ReadableSerialize(this Phrase obj, string clientId)
    {
        var dic = new Dictionary<string, object?>
        {
            ["Client Id"] = clientId,
            ["Id"] = obj.PhraseId,
            ["Phrase Lang"] = obj.PhraseLanguage,
            ["Phrase Text"] = obj.PhraseText,
            ["Note"] = obj.Note,
            ["Modified At"] = obj.ModifiedAt.ToDateTime(),
            ["Delete Flag"] = obj.DeleteFlag
        };

        return JsonSerializer.Serialize(dic, _option);
    }

    public static string ReadableSerialize(this PhraseTag obj, string clientId)
    {
        var dic = new Dictionary<string, object?>
        {
            ["Client Id"] = clientId,
            ["Id"] = obj.PhraseTagId,
            ["Phrase Id"] = obj.PhraseId,
            ["Tag Id"] = obj.TagId,
            ["Modified At"] = obj.ModifiedAt.ToDateTime(),
            ["Delete Flag"] = obj.DeleteFlag
        };

        return JsonSerializer.Serialize(dic, _option);
    }

    public static string ReadableSerialize(this TagCategory obj, string clientId)
    {
        var dic = new Dictionary<string, object?>
        {
            ["Client Id"] = clientId,
            ["Id"] = obj.TagCategoryId,
            ["Category Name"] = obj.CategoryName,
            ["Description"] = obj.Description,
            ["Modified At"] = obj.ModifiedAt.ToDateTime(),
            ["Delete Flag"] = obj.DeleteFlag
        };

        return JsonSerializer.Serialize(dic, _option);
    }

    public static string ReadableSerialize(this Tag obj, string clientId)
    {
        var dic = new Dictionary<string, object?>
        {
            ["Client Id"] = clientId,
            ["Id"] = obj.TagId,
            ["Tag Name"] = obj.TagName,
            ["Tag Category Id"] = obj.TagCategoryId,
            ["Description"] = obj.Description,
            ["Modified At"] = obj.ModifiedAt.ToDateTime(),
            ["Delete Flag"] = obj.DeleteFlag
        };

        return JsonSerializer.Serialize(dic, _option);
    }

    public static string ReadableSerialize(this Bookmark obj, string clientId)
    {
        var dic = new Dictionary<string, object?>
        {
            ["Client Id"] = clientId,
            ["Id"] = obj.BookmarkId,
            ["Word Id"] = obj.WordId,
            ["Note"] = obj.Note,
            ["Modified At"] = obj.ModifiedAt.ToDateTime(),
            ["Delete Flag"] = obj.DeleteFlag
        };

        return JsonSerializer.Serialize(dic, _option);
    }

    public static string ReadableSerialize(this BookmarkTag obj, string clientId)
    {
        var dic = new Dictionary<string, object?>
        {
            ["Client Id"] = clientId,
            ["Id"] = obj.BookmarkTagId,
            ["Bookmark Id"] = obj.BookmarkId,
            ["Tag Id"] = obj.TagId,
            ["Modified At"] = obj.ModifiedAt.ToDateTime(),
            ["Delete Flag"] = obj.DeleteFlag
        };

        return JsonSerializer.Serialize(dic, _option);
    }
}
