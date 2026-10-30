using Core.Persistence.Repositories;
using GAME.Application.Features.Commands.Summon;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Application.Templates;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class TemplateCacheSqlTests
{
    private sealed class Random : ISummonRandom
    {
        public int Next(int exclusiveMaximum) => 0;
    }
}
