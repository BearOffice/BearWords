using BearWordsAPI.Shared.Data.Models;

namespace BearWordsMaui.Services.DbServices.DataItems;

public class TagsContainer
{
    public required string TagCategoryId { get; set; }
    public required string CategoryName { get; set; }
    public string? Description { get; set; }
    public required IEnumerable<Tag> Tags { get; set; }
}
