using static GAME.Application.Features.Shared.QuickClimb.QuickClimbProjection;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.QuickClimb;

public sealed record GetActiveJobQuery(string UserId) : IQuery<BaseResponse<QuickClimbJobStatusDto?>>;

public sealed class GetActiveJobQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService) : IQueryHandler<GetActiveJobQuery, BaseResponse<QuickClimbJobStatusDto?>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    public async Task<BaseResponse<QuickClimbJobStatusDto?>> Handle(GetActiveJobQuery message, CancellationToken ct) => BaseResponse<QuickClimbJobStatusDto?>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private Task<QuickClimbJobStatusDto?> ExecuteAsync(string userId, CancellationToken ct = default) => GetActiveJobAsyncCore(userId, ct);
    private async Task<QuickClimbJobStatusDto?> GetActiveJobAsyncCore(string userId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var job = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .Where(j => j.PlayerId == player.Id)
            .OrderByDescending(j => j.CreatedOnUtc)
            .FirstOrDefaultAsync(ct);
        if (job == null)
            return null;
        // If job is still active, or completed very recently (within 10 minutes)
        if (job.Status == "QUEUED" || job.Status == "PROCESSING" || (job.CompletedOnUtc.HasValue && (DateTime.UtcNow - job.CompletedOnUtc.Value).TotalMinutes < 10))
        {
            return MapJobToDto(job);
        }

        return null;
    }
}
