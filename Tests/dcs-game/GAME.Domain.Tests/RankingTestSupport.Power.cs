using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Infrastructure.Locking;

namespace GAME.Domain.Tests;
internal static partial class RankingTestSupport
{
    internal sealed class Power(int initial = 0) : IFormationPowerQueryService
    {
        public int Value { get; set; } = initial;
        public Exception? Failure { get; set; }
        public int Calls { get; private set; }

        public Task<FormationPowerResultDto> GetDefaultFormationPowerAsync(long playerId, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Failure is not null) throw Failure;
            return Task.FromResult(new FormationPowerResultDto { TotalPower = Value });
        }
        public Task<FormationPowerResultDto> CalculateFormationPowerAsync(long playerId, string? formationCode, IReadOnlyCollection<FormationPositionRequestDto>? positions, CancellationToken cancellationToken = default) => GetDefaultFormationPowerAsync(playerId, cancellationToken);
    }
}
