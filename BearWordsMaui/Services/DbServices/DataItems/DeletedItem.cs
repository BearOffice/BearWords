namespace BearWordsMaui.Services.DbServices.DataItems;

public class DeletedItem
{
    public required string Id { get; set; }
    public required DeletedItemType Type { get; set; }
    public required string Name { get; set; }
    public required long DeletedAt { get; set; }
}