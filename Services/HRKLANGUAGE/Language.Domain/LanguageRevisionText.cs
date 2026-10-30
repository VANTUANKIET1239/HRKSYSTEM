namespace Language.Domain;

public sealed class LanguageRevisionText
{
    public long RevisionId { get; set; }
    public long LanguageKeyId { get; set; }
    public string Value { get; set; } = "";
}
