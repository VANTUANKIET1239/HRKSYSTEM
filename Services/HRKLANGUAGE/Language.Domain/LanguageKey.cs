namespace Language.Domain;

public sealed class LanguageKey
{
    public long Id { get; set; }
    public string Group { get; set; } = "";
    public string Key { get; set; } = "";
}
