namespace Language.Domain;

public sealed class LanguagePublication
{
    public string Group { get; set; } = "";
    public string Culture { get; set; } = "";
    public long RevisionId { get; set; }
}
