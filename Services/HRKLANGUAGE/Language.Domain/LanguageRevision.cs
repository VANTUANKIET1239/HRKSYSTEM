namespace Language.Domain;

public sealed class LanguageRevision
{
    public long Id { get; set; }
    public string Group { get; set; } = "";
    public string Culture { get; set; } = "";
    // Null only while the publication transaction is inserting its text rows.
    public DateTime? PublishedAtUtc { get; set; }
    public string PublishedBy { get; set; } = "script";
}
