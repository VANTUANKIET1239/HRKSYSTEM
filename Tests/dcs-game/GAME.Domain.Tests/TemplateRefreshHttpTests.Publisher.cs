using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Core.Application.DependencyInjection;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Web.Responses;
using GAME.Application.Features.Commands.GameConfig;
using GAME.Application.Features.Queries.GameConfig;
using GAME.Application.Templates;
using GAME.Infrastructure.Templates;
using HRK.GAME.Common;
using HRK.GAME.Controllers;
using HRK.GAME.Filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class TemplateRefreshHttpTests
{
    private sealed class Publisher : IGameTemplateRefresh
    {
        public int Calls;
        public Task<TemplateRefreshResult> RefreshAsync<T>(CancellationToken cancellationToken = default)
            where T : class => RefreshAsync(cancellationToken);
        public Task<TemplateRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(new TemplateRefreshResult("revision", DateTimeOffset.UtcNow, 41, 12));
        }
    }
}
