using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Caching.Hybrid;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Templates;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GAME.Infrastructure.Templates;
public sealed partial class GameTemplateStore
{
    internal sealed class TemplateTableExpiredException(string table) : InvalidOperationException($"Template table {table} is unavailable for this revision; retry with the current template revision.");
}
