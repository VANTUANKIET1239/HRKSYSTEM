using GAME.Application.DTOs;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Application.Interfaces;

public interface ITowerQuickClimbService
{
    Task<QuickClimbJobStatusDto> StartQuickClimbAsync(string userId, StartQuickClimbRequestDto request, CancellationToken ct = default);
    Task<QuickClimbJobStatusDto?> GetActiveJobAsync(string userId, CancellationToken ct = default);
    Task<QuickClimbJobStatusDto> GetJobStatusAsync(string userId, string jobId, CancellationToken ct = default);
    Task<QuickClimbJobStatusDto> StopQuickClimbAsync(string userId, string jobId, CancellationToken ct = default);
    Task<bool> ProcessJobFloorMessageAsync(
        Guid messageId,
        string jobId,
        string userId,
        int expectedFloor,
        long expectedVersion,
        string workerId,
        CancellationToken ct = default);
}
