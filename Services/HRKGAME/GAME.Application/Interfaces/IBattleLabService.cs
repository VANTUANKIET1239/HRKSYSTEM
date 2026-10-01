using GAME.Application.DTOs;

namespace GAME.Application.Interfaces;

public interface IBattleLabService
{
    Task<BattleLabCatalogDto> GetCatalogAsync(CancellationToken ct);
    Task<BattleLabReportDto> RunAsync(BattleLabRequestDto request, CancellationToken ct);
}
