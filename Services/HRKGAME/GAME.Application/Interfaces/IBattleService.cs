using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IBattleService
    {
        Task<BattleInitialStateDto> GetBattleInitialStateAsync(string userId, CancellationToken cancellationToken = default);
        Task<List<BattleLogDto>> GetBattleLogsAsync(string battleId, CancellationToken cancellationToken = default);
    }
}
