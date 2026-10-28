using Core.InternalApi;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Localization;

public sealed class TextLocalizer(ITranslationCache cache, IInternalApiClient api,
    IOptions<LocalizationOptions> options, ILogger<TextLocalizer> logger) : ITextLocalizer
{
    private readonly LocalizationOptions _options = options.Value;
    // Scoped to the current request only; never pins a revision across requests or instances.
    private readonly ConcurrentDictionary<string, Lazy<Task<TranslationBundle?>>> _requests = new(StringComparer.Ordinal);

    public Task<TranslationBundle?> GetBundleAsync(string group, string culture, CancellationToken ct = default)
    {
        if (!_options.Enabled || !TranslationIdentity.ValidGroup(group)) return Task.FromResult<TranslationBundle?>(null);
        culture = TranslationIdentity.Culture(culture) ?? _options.DefaultCulture;
        var identity = group + ":" + culture;
        return _requests.GetOrAdd(identity, _ => new Lazy<Task<TranslationBundle?>>(() => LoadAsync(group, culture, ct))).Value;
    }

    private async Task<TranslationBundle?> LoadAsync(string group, string culture, CancellationToken ct)
    {
        try
        {
            if (await cache.GetCurrentAsync(group, culture, ct) is { } cached) return cached;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogDebug(ex, "Translation cache unavailable for {Group}/{Culture}.", group, culture); }
        try
        {
            var bundle = await api.GetAsync<TranslationBundle>(_options.ServiceName,
                $"/api/language/bundles/{group}/{culture}",
                new InternalCallOptions { TimeoutSeconds = _options.ApiTimeoutSeconds }, ct);
            return bundle.Group == group && bundle.Culture == culture && bundle.Revision > 0 ? bundle : null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Translation API unavailable for {Group}/{Culture}; using fallback text.", group, culture);
            return null;
        }
    }

    public async Task<string> GetAsync(string group, string culture, string key, string fallback,
        IReadOnlyDictionary<string, object?>? arguments = null, CancellationToken ct = default)
    {
        culture = TranslationIdentity.Culture(culture) ?? _options.DefaultCulture;
        foreach (var candidate in new[] { (group, culture), ("common", culture),
                     (group, _options.DefaultCulture), ("common", _options.DefaultCulture) }.Distinct())
        {
            var bundle = await GetBundleAsync(candidate.Item1, candidate.Item2, ct);
            if (bundle?.Translations.TryGetValue(TranslationIdentity.TextKey(fallback), out var precise) == true)
                return TranslationIdentity.Format(precise, arguments, culture);
            if (bundle?.Translations.TryGetValue(key, out var translated) == true)
                return TranslationIdentity.Format(translated, arguments, culture);
        }
        return TranslationIdentity.Format(fallback, arguments, culture);
    }
}
