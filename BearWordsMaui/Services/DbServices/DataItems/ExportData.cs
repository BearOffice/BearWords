namespace BearWordsMaui.Services.DbServices.DataItems;

public class ExportTagData
{
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, string?> Tags { get; set; } = [];
}

public class ExportWordData
{
    public string Lang { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string[] Alias { get; set; } = [];
    public string? Note { get; set; }
    public string[] Tags { get; set; } = [];
}

public class ExportResult
{
    public string TagCategoryJson { get; set; } = string.Empty;
    public string BookmarkDataJson { get; set; } = string.Empty;
}
