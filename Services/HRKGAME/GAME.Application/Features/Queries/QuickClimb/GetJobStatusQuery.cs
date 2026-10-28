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

public sealed record GetJobStatusQuery(string UserId, string JobId) : IQuery<BaseResponse<QuickClimbJobStatusDto>>;

public sealed class GetJobStatusQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService gamePlayerService) : IQueryHandler<GetJobStatusQuery, BaseResponse<QuickClimbJobStatusDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    public async Task<BaseResponse<QuickClimbJobStatusDto>> Handle(GetJobStatusQuery message, CancellationToken ct) => BaseResponse<QuickClimbJobStatusDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.JobId, ct));
    private Task<QuickClimbJobStatusDto> ExecuteAsync(string userId, string jobId, CancellationToken ct = default) => GetJobStatusAsyncCore(userId, jobId, ct);
    private async Task<QuickClimbJobStatusDto> GetJobStatusAsyncCore(string userId, string jobId, CancellationToken ct = default)
    {
        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var job = await _unitOfWork.ReadOnlyRepository<HrkTowerQuickClimbJob>().Query()
            .FirstOrDefaultAsync(j => j.JobId == jobId && j.PlayerId == player.Id, ct) ?? throw BusinessException.Localized("QUICK_CLIMB_JOB_NOT_FOUND", $"Không tìm thấy phiên leo nhanh '{jobId}'.", BusinessErrorKind.NotFound);
        return MapJobToDto(job);
    }
}
