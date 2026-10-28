namespace Language.Domain;

public sealed class LanguageCulture
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public sealed class LanguageRevision
{
    public long Id { get; set; }
    public string Group { get; set; } = "";
    public string Culture { get; set; } = "";
    // Null only while the publication transaction is inserting its text rows.
    public DateTime? PublishedAtUtc { get; set; }
    public string PublishedBy { get; set; } = "script";
}

public sealed class LanguageKey
{
    public long Id { get; set; }
    public string Group { get; set; } = "";
    public string Key { get; set; } = "";
}

public sealed class LanguageRevisionText
{
    public long RevisionId { get; set; }
    public long LanguageKeyId { get; set; }
    public string Value { get; set; } = "";
}

public sealed class LanguagePublication
{
    public string Group { get; set; } = "";
    public string Culture { get; set; } = "";
    public long RevisionId { get; set; }
}
