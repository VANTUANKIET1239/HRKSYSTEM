using Core.Localization;
using Core.Persistence.Repositories;
using Language.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Language.Application;

public sealed class PublishedLanguageReader(IUnitOfWork uow, ITranslationCache cache,
    ILogger<PublishedLanguageReader> logger) : IPublishedLanguageReader
{
    public async Task<TranslationBundle?> GetAsync(string group, string culture, CancellationToken ct)
    {
        if (!TranslationIdentity.ValidGroup(group) || TranslationIdentity.Culture(culture) != culture) return null;
        try
        {
            if (await cache.GetCurrentAsync(group, culture, ct) is { } cached) return cached;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogDebug(ex, "Language Redis read failed; reading published DB snapshot."); }
        var bundle = await GetFromDatabaseAsync(group, culture, ct);
        if (bundle is null) return null;
        try { await cache.PublishAsync(bundle, ct); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { logger.LogWarning(ex, "Language Redis recovery failed for {Group}/{Culture}.", group, culture); }
        return bundle;
    }

    public async Task<TranslationBundle?> GetFromDatabaseAsync(string group, string culture, CancellationToken ct)
    {
        // Read the pointer and all its immutable text rows in one SQL statement.
        // Left joins preserve an explicitly published empty bundle (for example hrm).
        var rows = await (from publication in uow.ReadOnlyRepository<LanguagePublication>().Query()
            join item in uow.ReadOnlyRepository<LanguageRevision>().Query() on publication.RevisionId equals item.Id
            join locale in uow.ReadOnlyRepository<LanguageCulture>().Query() on item.Culture equals locale.Code
            join text in uow.ReadOnlyRepository<LanguageRevisionText>().Query() on item.Id equals text.RevisionId into texts
            from text in texts.DefaultIfEmpty()
            join key in uow.ReadOnlyRepository<LanguageKey>().Query() on text.LanguageKeyId equals key.Id into keys
            from key in keys.DefaultIfEmpty()
            where publication.Group == @group && publication.Culture == culture && locale.IsActive
            select new { RevisionId = item.Id, Key = (string?)key.Key, Value = (string?)text.Value }).ToListAsync(ct);
        return rows.Count == 0 ? null : new TranslationBundle(group, culture, rows[0].RevisionId,
            rows.Where(row => row.Key is not null).ToDictionary(row => row.Key!, row => row.Value!, StringComparer.Ordinal));
    }
}
