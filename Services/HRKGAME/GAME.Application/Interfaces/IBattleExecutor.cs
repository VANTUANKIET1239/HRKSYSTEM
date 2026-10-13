using GAME.Application.DTOs;

namespace GAME.Application.Interfaces;

public interface IBattleExecutor
{
    Task<StartBattleResultDto> ExecuteAsync(string userId, StartBattleRequestDto request, CancellationToken cancellationToken = default);
    Task<BattleInitialStateDto> BuildInitialStateAsync(string userId, CancellationToken cancellationToken = default, int? stageId = null, string? formationCode = null, IReadOnlyCollection<FormationPositionRequestDto>? positions = null);
}
