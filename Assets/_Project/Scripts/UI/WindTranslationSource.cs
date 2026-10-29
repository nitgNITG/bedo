using Lean.Localization;

// One Lean source keeps the reviewed bilingual catalogue together.
public sealed class WindTranslationSource : LeanSource
{
    public override void Register(string primaryLanguage, string secondaryLanguage)
    {
        foreach (var entry in WindLocalization.Catalogue.entries)
        {
            var translation = LeanLocalization.RegisterTranslation(WindLocalization.Key(entry.english));
            translation.Register("English", this);
            translation.Register("Arabic", this);
            translation.Data = primaryLanguage == "Arabic" ? entry.arabic : entry.english;
            translation.Primary = true;
        }
    }
}
