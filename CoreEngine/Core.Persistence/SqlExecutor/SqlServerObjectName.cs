using System.Text.RegularExpressions;

namespace Core.Persistence.SqlExecutor;

internal sealed record SqlServerObjectName(string Schema, string Name)
{
    public string Quoted => $"[{Schema}].[{Name}]";

    // Accept simple local identifiers only; not arbitrary SQL or cross-database names.
    private static readonly Regex Identifier = new(@"\A[A-Za-z_][A-Za-z0-9_]{0,127}\z",
        RegexOptions.CultureInvariant);

    public static SqlServerObjectName Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        var parts = value.Split('.');
        if (parts.Length is < 1 or > 2 || parts.Any(part => !Identifier.IsMatch(part)))
            throw new ArgumentException("Use a simple SQL object name or schema.name (unquoted, maximum 128 characters per part).", nameof(value));
        return parts.Length == 1 ? new("dbo", parts[0]) : new(parts[0], parts[1]);
    }
}
