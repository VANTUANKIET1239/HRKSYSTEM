using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Localization;

public interface ITextLocalizer
{
    Task<TranslationBundle?> GetBundleAsync(string group, string culture, CancellationToken ct = default);
    Task<string> GetAsync(string group, string culture, string key, string fallback,
        IReadOnlyDictionary<string, object?>? arguments = null, CancellationToken ct = default);
}
