using GAME.Application.DTOs;

namespace GAME.Application.Interfaces
{
    public interface ICombatPowerService
    {
        Task<List<CombatPowerConfigDto>> GetConfigsAsync(CancellationToken cancellationToken = default);
        int Calculate(CalculatedStatsDto stats, IReadOnlyCollection<CombatPowerConfigDto> configs);
        Task<int> CalculateAsync(CalculatedStatsDto stats, CancellationToken cancellationToken = default);
    }
}
