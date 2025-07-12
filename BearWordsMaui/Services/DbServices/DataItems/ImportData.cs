namespace BearWordsMaui.Services.DbServices.DataItems;

public class ImportWordData
{
    private string _title = string.Empty;
    private string[] _alias = [];
    public string Lang { get; set; } = string.Empty;
    public string Title { get => _title; set => _title = value.ToLower(); }
    public string[] Alias { get => _alias; set => _alias = [.. value.Select(v => v.ToLower())]; }
    public string? Note { get; set; }
    public string[] Tags { get; set; } = [];
}

public class ImportTagData
{
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, string?> Tags { get; set; } = [];
}

public class ImportResult
{
    public string Title { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailedReason { get; set; }
    public ImportItemType ItemType { get; set; }
}

public enum ImportItemType
{
    Word,
    Phrase,
    Tag
}

public class ImportSummary
{
    public List<ImportResult> Results { get; set; } = [];
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }

    public string GetSummaryText()
    {
        var summary = $"Total: {TotalProcessed}, Success: {SuccessCount}, Failed: {FailedCount}\n\n";

        var successResults = Results.Where(r => r.Success).ToList();
        var failedResults = Results.Where(r => !r.Success).ToList();

        if (successResults.Count != 0)
        {
            summary += "Success:\n";
            foreach (var result in successResults)
            {
                summary += $"[{result.ItemType}] {result.Title}\n";
            }
            summary += "\n";
        }

        if (failedResults.Count != 0)
        {
            summary += "Failed:\n";
            foreach (var result in failedResults)
            {
                summary += $"Title: {result.Title}";
                if (!string.IsNullOrEmpty(result.FailedReason))
                {
                    summary += $" - {result.FailedReason}";
                }
                summary += "\n";
            }
        }

        return summary;
    }
}