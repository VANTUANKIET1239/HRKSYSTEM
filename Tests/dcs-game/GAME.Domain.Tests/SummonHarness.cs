using System.Text.Json;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Application.Services;
using GAME.Application.Templates;
using GAME.Infrastructure.Templates;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace GAME.Domain.Tests;

// Exercise the production CQRS handlers with the same SQL UOW, random source and reward granter.
public sealed class SummonHarness(

    Core.Persistence.Repositories.IUnitOfWork store,

    IGamePlayerService players,

    ISummonRandom random,

    SummonRewardGranter granter,
    IGameTemplateCatalog templates

)
{
    public async Task<SummonConfigurationDto> GetConfigurationAsync(string userId, CancellationToken ct) => (await new GAME.Application.Features.Queries.Summon.GetSummonConfigurationQueryHandler(store, players, templates).Handle(new(userId), ct)).Data!;
    public async Task<SummonResultDto> SummonAsync(string userId, SummonRequestDto request, CancellationToken ct) => (await new GAME.Application.Features.Commands.Summon.SummonCommandHandler(store, players, random, granter, RankingTestSupport.Spending(store), RankingTestSupport.Locks(store), templates).Handle(new(userId, request), ct)).Data!;
    public async Task<SummonResultDto> GetResultAsync(string userId, Guid requestId, CancellationToken ct) => (await new GAME.Application.Features.Queries.Summon.GetSummonResultQueryHandler(store, players).Handle(new(userId, requestId), ct)).Data!;
}
