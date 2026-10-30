using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Features.Commands.GameConfig;
using GAME.Application.Features.Queries.Catalog;
using GAME.Application.Templates;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class TemplateCacheTests
{
    private sealed class RefreshProbe : IGameTemplateRefresh
    {
        public bool Called;
        public Task<TemplateRefreshResult> RefreshAsync<T>(CancellationToken cancellationToken = default)
            where T : class => RefreshAsync(cancellationToken);
        public Task<TemplateRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(new TemplateRefreshResult("test", DateTimeOffset.UtcNow, 1, 1));
        }
    }
}
