using Core.Localization;
using Core.Persistence.Repositories;
using Language.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Language.Application;

public interface IPublishedLanguageReader
{
    Task<TranslationBundle?> GetAsync(string group, string culture, CancellationToken ct);
    Task<TranslationBundle?> GetFromDatabaseAsync(string group, string culture, CancellationToken ct);
}
