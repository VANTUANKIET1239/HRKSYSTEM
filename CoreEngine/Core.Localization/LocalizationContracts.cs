using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Localization;

public sealed record TranslationBundle(string Group, string Culture, long Revision,
    IReadOnlyDictionary<string, string> Translations);

public interface ITranslationCache
{
    Task<TranslationBundle?> GetCurrentAsync(string group, string culture, CancellationToken ct = default);
    // Must throw on write failure: the outbox may only complete after a successful write.
    Task PublishAsync(TranslationBundle bundle, CancellationToken ct = default);
}

public interface ITextLocalizer
{
    Task<TranslationBundle?> GetBundleAsync(string group, string culture, CancellationToken ct = default);
    Task<string> GetAsync(string group, string culture, string key, string fallback,
        IReadOnlyDictionary<string, object?>? arguments = null, CancellationToken ct = default);
}

public static partial class TranslationIdentity
{
    // Stable cross-platform identifier for existing literal messages, not a security hash.
    public static string TextKey(string text)
    {
        ulong hash = 14695981039346656037;
        foreach (var value in Encoding.UTF8.GetBytes(text)) hash = unchecked((hash ^ value) * 1099511628211);
        return "text_" + hash.ToString("x16", CultureInfo.InvariantCulture);
    }

    public static string? Culture(string? culture) => culture?.Trim().ToLowerInvariant() switch
    {
        "vi" or "vi-vn" => "vi-VN",
        "en" or "en-us" => "en-US",
        _ => null
    };

    public static bool ValidGroup(string group) => GroupPattern().IsMatch(group);
    [GeneratedRegex("^[a-z][a-z0-9-]{0,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex GroupPattern();

    public static string Format(string text, IReadOnlyDictionary<string, object?>? arguments, string culture)
    {
        if (arguments is null) return text;
        var provider = CultureInfo.GetCultureInfo(Culture(culture) ?? "vi-VN");
        return Placeholder().Replace(text, match => arguments.TryGetValue(match.Groups[1].Value, out var value)
            ? Convert.ToString(value, provider) ?? "" : match.Value);
    }

    [GeneratedRegex(@"\{\{\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\}\}")]
    private static partial Regex Placeholder();
}
