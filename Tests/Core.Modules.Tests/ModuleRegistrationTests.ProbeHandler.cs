using Core.AUTH.DependencyInjection;
using Core.AUTH.Jwt;
using Core.Application.CQRS;
using Core.Application.DependencyInjection;
using Core.Caching;
using Core.Caching.Hybrid;
using Core.Common.Entity;
using Core.Persistence.Database.Extensions;
using Core.Persistence.DependencyInjection;
using Core.Persistence.Repositories;
using Core.Web.Cookies;
using Core.Web.DependencyInjection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RedisBackendOptions = Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions;
using Xunit;

namespace Core.Modules.Tests;
public partial class ModuleRegistrationTests
{
    public sealed class ProbeHandler : IQueryHandler<ProbeQuery, int>
    {
        public Task<int> Handle(ProbeQuery request, CancellationToken cancellationToken) => Task.FromResult(7);
    }
}
