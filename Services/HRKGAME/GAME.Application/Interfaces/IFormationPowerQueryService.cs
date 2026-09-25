using GAME.Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces
{
    public interface IFormationPowerQueryService
    {
        /// <summary>
        /// Lấy và tính toán lực chiến của đội hình mặc định (IsSelected) của người chơi.
        /// Chỉ query các trường cần thiết để tính toán chỉ số và lực chiến, không load skill/effect graph.
        /// Trả về 0 nếu chưa có đội hình mặc định hoặc chưa xếp tướng.
        /// </summary>
        Task<FormationPowerResultDto> GetDefaultFormationPowerAsync(long playerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tính toán lực chiến cho một đội hình cụ thể hoặc danh sách vị trí tướng.
        /// </summary>
        Task<FormationPowerResultDto> CalculateFormationPowerAsync(
            long playerId,
            string? formationCode,
            IReadOnlyCollection<FormationPositionRequestDto>? positions,
            CancellationToken cancellationToken = default);
    }
}
