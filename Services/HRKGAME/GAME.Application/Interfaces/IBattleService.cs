using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IBattleService
    {
        Task<StartBattleResultDto> StartBattleAsync(string userId, StartBattleRequestDto request, CancellationToken cancellationToken = default);
        Task<BattleInitialStateDto> GetBattleInitialStateAsync(string userId, CancellationToken cancellationToken = default, int? stageId = null, string? formationCode = null, IReadOnlyCollection<FormationPositionRequestDto>? positions = null);
        Task<List<BattleLogDto>> GetBattleLogsAsync(string battleId, CancellationToken cancellationToken = default);
    }
}
