using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IFormationService
    {
        Task<List<PlayerFormationSummaryDto>> GetPlayerFormationsAsync(string userId, CancellationToken cancellationToken = default);
        Task<FormationDetailDto?> GetFormationDetailAsync(string userId, string formationCode, CancellationToken cancellationToken = default);
        Task<FormationDetailDto> UpdatePositionsAsync(string userId, string formationCode, UpdateFormationPositionsRequest request, CancellationToken cancellationToken = default);
        Task<FormationDetailDto> SelectFormationAsync(string userId, string formationCode, CancellationToken cancellationToken = default);
        Task<FormationUpgradeResultDto> UpgradeFormationAsync(string userId, string formationCode, CancellationToken cancellationToken = default);

        // Legacy compatibility
        Task<FormationDto> GetPlayerFormationAsync(string userId, string formationName = "Main Team", CancellationToken cancellationToken = default);
    }
}
