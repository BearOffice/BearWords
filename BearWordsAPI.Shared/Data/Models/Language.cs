using System;
using System.Collections.Generic;

namespace BearWordsAPI.Shared.Data.Models;

public partial class Language
{
    public string LanguageCode { get; set; } = null!;
    public string LanguageName { get; set; } = null!;

    public virtual ICollection<Dictionary> Dictionaries { get; set; } = [];
    public virtual ICollection<Phrase> Phrases { get; set; } = [];
    public virtual ICollection<Translation> Translations { get; set; } = [];
}
