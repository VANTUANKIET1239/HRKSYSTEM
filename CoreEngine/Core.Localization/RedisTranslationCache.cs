using System.Globalization;
using System.Text.Json;
using Core.Caching.Redis;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Core.Localization;

public sealed class RedisTranslationCache(IRedisConnectionProvider connections,
    IOptions<LocalizationOptions> options) : ITranslationCache
{
    private readonly LocalizationOptions _options = options.Value;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private string BundleKey(string group, string culture)
    {
        if (!TranslationIdentity.ValidGroup(group) || TranslationIdentity.Culture(culture) != culture)
            throw new ArgumentException("Invalid translation bundle identity.");
        return _options.RedisKeyPrefix + "{" + group + ":" + culture + "}:bundle";
    }

    public async Task<TranslationBundle?> GetCurrentAsync(string group, string culture, CancellationToken ct = default)
    {
        var key = BundleKey(group, culture);
        var db = await connections.GetDatabaseAsync(_options.RedisConnectionString, ct);
        var json = await db.HashGetAsync(key, "payload").WaitAsync(ct);
        if (json.IsNullOrEmpty) return null;
        var bundle = JsonSerializer.Deserialize<TranslationBundle>(json.ToString(), Json);
        return bundle is { Revision: > 0, Translations: not null } && bundle.Group == group && bundle.Culture == culture
            ? bundle : null;
    }

    public async Task PublishAsync(TranslationBundle bundle, CancellationToken ct = default)
    {
        if (bundle.Revision < 1) throw new ArgumentException("A published revision must be positive.");
        var key = BundleKey(bundle.Group, bundle.Culture);
        var db = await connections.GetDatabaseAsync(_options.RedisConnectionString, ct);
        await db.ScriptEvaluateAsync(RedisTranslationScripts.PublishCurrentIfNewer,
            [key], [bundle.Revision.ToString(CultureInfo.InvariantCulture),
                JsonSerializer.Serialize(bundle, Json), _options.CacheSeconds]).WaitAsync(ct);
    }
}
