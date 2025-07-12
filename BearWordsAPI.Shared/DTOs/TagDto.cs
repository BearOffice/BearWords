using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class TagDto
{
    public string TagId { get; set; } = null!;
    public string TagName { get; set; } = null!;
    public string TagCategoryId { get; set; } = null!;
    public string? Description { get; set; }
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public TagDto() { }

    public TagDto(Tag tag)
    {
        TagId = tag.TagId;
        TagName = tag.TagName;
        TagCategoryId = tag.TagCategoryId;
        Description = tag.Description;
        ModifiedAt = tag.ModifiedAt;
        DeleteFlag = tag.DeleteFlag;
    }

    public Tag ToEntity()
    {
        return new Tag
        {
            TagId = TagId,
            TagName = TagName,
            TagCategoryId = TagCategoryId,
            Description = Description,
            ModifiedAt = ModifiedAt,
            DeleteFlag = DeleteFlag
        };
    }
}
