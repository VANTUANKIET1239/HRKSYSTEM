using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.Localization;

public interface ITranslationCache
{
    Task<TranslationBundle?> GetCurrentAsync(string group, string culture, CancellationToken ct = default);
    // Must throw on write failure: the outbox may only complete after a successful write.
    Task PublishAsync(TranslationBundle bundle, CancellationToken ct = default);
}
