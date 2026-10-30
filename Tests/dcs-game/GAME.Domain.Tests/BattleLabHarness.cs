using GAME.Application.DTOs;
using Core.Common.Errors;
using GAME.Domain.Battle;
using GAME.Domain.Entities;
using GAME.Application.Services;
using Xunit;

namespace GAME.Domain.Tests;

internal sealed class BattleLabHarness(TowerTestStore store)
{
    public async Task<BattleLabReportDto> RunAsync(BattleLabRequestDto request, CancellationToken ct) => (await new GAME.Application.Features.Commands.BattleLab.RunBattleLabCommandHandler(store, new BattleSimulationEngine(), null).Handle(new(request), ct)).Data!;
}
