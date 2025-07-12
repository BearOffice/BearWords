using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using BearWordsMaui.Services.DbServices.DataItems;
using Microsoft.EntityFrameworkCore;

namespace BearWordsMaui.Services.DbServices;

public class TagService
{
    private readonly ConfigService _config;
    private readonly IUUIDGenerator _uuid;
    private readonly IDbContextService _dbContextService;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public TagService(IDbContextService dbContextService, ConfigService config, IUUIDGenerator uuid)
    {
        _dbContextService = dbContextService;
        _config = config;
        _uuid = uuid;
    }

    public async Task<PaginatedResult<TagsContainer>> GetTagCategoriesWithTagsAsync(string? searchText = null,
        SortOption sortOption = SortOption.Alphabetical, int page = 1, int pageSize = 100)
    {
        var tagQuery = Context.Tags
            .AsNoTracking()
            .WhereNotDeleted()
            .Include(t => t.TagCategory)
            .AsQueryable();

        // Apply search filter to tags
        if (!string.IsNullOrEmpty(searchText))
        {
            searchText = searchText.Trim().ToLower();
            tagQuery = tagQuery.Where(t => t.TagName.ToLower().Contains(searchText) ||
                                           t.TagCategory.CategoryName.ToLower().Contains(searchText));
        }

        // Apply sorting by tag category
        tagQuery = sortOption switch
        {
            SortOption.Alphabetical => tagQuery.OrderBy(t => t.TagCategory.CategoryName).ThenBy(t => t.TagName),
            SortOption.AlphabeticalDesc => tagQuery.OrderByDescending(t => t.TagCategory.CategoryName).ThenByDescending(t => t.TagName),
            _ => tagQuery.OrderBy(t => t.TagCategory.CategoryName).ThenBy(t => t.TagName)
        };

        var totalItems = await tagQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var paginatedTags = await tagQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Group tags by category
        var categories = paginatedTags
            .GroupBy(t => t.TagCategoryId)
            .Select(g =>
            {
                var tags = g.ToList();
                var cat = tags.FirstOrDefault()!.TagCategory;
                return new TagsContainer
                {
                    TagCategoryId = cat.TagCategoryId,
                    CategoryName = cat.CategoryName,
                    Description = cat.Description,
                    Tags = tags
                };
            })
            .ToList();

        if (page == totalPages || totalPages == 0)
        {
            var emptyCat = Context.TagCategories
                .WhereNotDeleted()
                .Where(tc => !tc.Tags.Any());

            if (!string.IsNullOrEmpty(searchText))
            {
                emptyCat = emptyCat.Where(tc => tc.CategoryName.ToLower().Contains(searchText));
            }

            emptyCat = sortOption switch
            {
                SortOption.Alphabetical => emptyCat.OrderBy(e => e.CategoryName),
                SortOption.AlphabeticalDesc => emptyCat.OrderByDescending(e => e.CategoryName),
                _ => emptyCat.OrderBy(e => e.CategoryName)
            };
            categories.AddRange(emptyCat.Select(e =>
                new TagsContainer
                {
                    TagCategoryId = e.TagCategoryId,
                    CategoryName = e.CategoryName,
                    Description = e.Description,
                    Tags = Enumerable.Empty<Tag>()
                }));
        }

        return new PaginatedResult<TagsContainer>
        {
            Items = categories,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    public async Task<Tag?> GetTagByIdAsync(string tagId)
    {
        return await Context.Tags
            .AsNoTracking()
            .WhereNotDeleted()
            .Include(t => t.TagCategory)
            .FirstOrDefaultAsync(t => t.TagId == tagId);
    }

    public async Task<Tag?> GetTagByNameAsync(string tagName)
    {
        return await Context.Tags
            .AsNoTracking()
            .WhereNotDeleted()
            .Include(t => t.TagCategory)
            .FirstOrDefaultAsync(t => t.TagName == tagName);
    }

    public async Task<TagCategory[]> GetTagCategoriesAsync()
    {
        return await Context.TagCategories
            .AsNoTracking()
            .WhereNotDeleted()
            .Include(t => t.Tags)
            .OrderBy(tc => tc.CategoryName)
            .ToArrayAsync();
    }

    public async Task<TagCategory?> GetTagCategoryByIdAsync(string tagCategoryId)
    {
        return await Context.TagCategories
            .AsNoTracking()
            .WhereNotDeleted()
            .FirstOrDefaultAsync(tc => tc.TagCategoryId == tagCategoryId);
    }

    public async Task<TagCategory?> GetTagCategoryByNameAsync(string categoryName)
    {
        return await Context.TagCategories
            .AsNoTracking()
            .WhereNotDeleted()
            .FirstOrDefaultAsync(tc => tc.CategoryName == categoryName);
    }

    public async Task<Tag?> CreateTagAsync(string tagName, string? description, string tagCategoryId)
    {
        var tag = await Context.Tags
            .FirstOrDefaultAsync(t => t.TagName == tagName && t.TagCategoryId == tagCategoryId);

        if (tag is null)
        {
            tag = new Tag
            {
                TagId = _uuid.Generate(),
                TagName = tagName,
                Description = description,
                TagCategoryId = tagCategoryId
            };

            Context.Tags.Add(tag);
        }
        else
        {
            if (!tag.DeleteFlag) throw new Exception("The tag already exists.");

            tag.UnsetDeleteFlag();
            tag.Description = description;
        }

        await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
        return tag;
    }

    public async Task DeleteTagAsync(string tagId)
    {
        var tag = await Context.Tags
            .WhereNotDeleted()
            .FirstOrDefaultAsync(t => t.TagId == tagId);

        tag!.SetDeleteFlag();

        await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
    }

    public async Task UpdateTagAsync(string tagId, string tagName, string? description, string tagCategoryId)
    {
        var tag = await Context.Tags
            .WhereNotDeleted()
            .FirstOrDefaultAsync(t => t.TagId == tagId);

        if (tag is not null)
        {
            tag.TagName = tagName;
            tag.Description = description;
            tag.TagCategoryId = tagCategoryId;

            await Context.SaveChangesAsync(updateTimestamps: true);
        }
    }

    public async Task<TagCategory?> CreateTagCategoryAsync(string categoryName, string? description)
    {
        var tagCategory = await Context.TagCategories
            .FirstOrDefaultAsync(tc => tc.CategoryName == categoryName);

        if (tagCategory is null)
        {
            tagCategory = new TagCategory
            {
                TagCategoryId = _uuid.Generate(),
                CategoryName = categoryName,
                Description = description,
                UserName = _config.UserName
            };

            Context.TagCategories.Add(tagCategory);
        }
        else
        {
            if (!tagCategory.DeleteFlag) throw new Exception("The tag category already exists.");

            tagCategory.UnsetDeleteFlag();
            tagCategory.Description = description;
        }

        await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
        return tagCategory;
    }

    public async Task DeleteTagCategoryAsync(string tagCategoryId)
    {
        var tagCategory = await Context.TagCategories
            .WhereNotDeleted()
            .FirstOrDefaultAsync(tc => tc.TagCategoryId == tagCategoryId);

        tagCategory!.SetDeleteFlag();

        await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
    }

    public async Task UpdateTagCategoryAsync(string tagCategoryId, string categoryName, string? description)
    {
        var tagCategory = await Context.TagCategories
            .WhereNotDeleted()
            .FirstOrDefaultAsync(tc => tc.TagCategoryId == tagCategoryId);

        if (tagCategory is not null)
        {
            tagCategory.CategoryName = categoryName;
            tagCategory.Description = description;

            await Context.SaveChangesAsync(updateTimestamps: true);
        }
    }
}