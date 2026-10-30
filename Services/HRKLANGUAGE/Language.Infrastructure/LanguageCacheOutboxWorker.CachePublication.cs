using System.Text.Json;
using Core.Localization;
using Core.TransactionalMessaging.Configuration;
using Core.TransactionalMessaging.Outbox;
using Language.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Language.Infrastructure;
public sealed partial class LanguageCacheOutboxWorker
{
    private sealed record CachePublication(string Group, string Culture);
}
