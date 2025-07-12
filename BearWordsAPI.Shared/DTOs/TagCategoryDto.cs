using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class TagCategoryDto
{
    public string TagCategoryId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? Description { get; set; }
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public TagCategoryDto() { }

    public TagCategoryDto(TagCategory tagCategory)
    {
        TagCategoryId = tagCategory.TagCategoryId;
        CategoryName = tagCategory.CategoryName;
        UserName = tagCategory.UserName;
        Description = tagCategory.Description;
        ModifiedAt = tagCategory.ModifiedAt;
        DeleteFlag = tagCategory.DeleteFlag;
    }

    public TagCategory ToEntity()
    {
        return new TagCategory
        {
            TagCategoryId = TagCategoryId,
            CategoryName = CategoryName,
            UserName = UserName,
            Description = Description,
            ModifiedAt = ModifiedAt,
            DeleteFlag = DeleteFlag,
        };
    }
}
