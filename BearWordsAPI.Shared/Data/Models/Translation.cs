using BearWordsAPI.Shared.Helpers;

namespace BearWordsAPI.Shared.Data.Models;

public partial class Translation : ITimestamps, ISoftDeletable
{
    public int TranslationId { get; set; }
    public int WordId { get; set; }
    public string TargetLanguage { get; set; } = null!;
    public string TranslationText { get; set; } = null!;
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public virtual Language TargetLanguageNavigation { get; set; } = null!;

    [CascadeSoftDelete(OnDelete = false, OnRestore = true)]
    public virtual Dictionary Word { get; set; } = null!;
}
