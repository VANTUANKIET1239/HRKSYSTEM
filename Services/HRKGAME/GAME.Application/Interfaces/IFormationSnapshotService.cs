using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IFormationSnapshotService
    {
        Task<FormationBattleSnapshot> BuildAsync(
            long playerId,
            string? formationCode,
            IReadOnlyCollection<FormationPositionRequestDto>? positions,
            CancellationToken cancellationToken = default);

        Task<FormationPreviewResponseDto> PreviewAsync(
            long playerId,
            string formationCode,
            IReadOnlyCollection<FormationPositionRequestDto> positions,
            CancellationToken cancellationToken = default);
    }
}
