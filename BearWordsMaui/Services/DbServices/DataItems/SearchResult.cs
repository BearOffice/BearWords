namespace BearWordsMaui.Services.DbServices.DataItems;

public class SearchResult
{
    public required object Item { get; set; }
    public required SearchItemType Type { get; set; }
    public required double Similarity { get; set; }
    public required bool IsBookmarked { get; set; }
    public IEnumerable<string> TagNames { get; set; } = [];
}
