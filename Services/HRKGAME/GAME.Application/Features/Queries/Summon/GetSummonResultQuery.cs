using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Summon;

namespace GAME.Application.Features.Queries.Summon;

public sealed record GetSummonResultQuery(string UserId, Guid RequestId) : IQuery<BaseResponse<SummonResultDto>>;

public sealed class GetSummonResultQueryHandler(IUnitOfWork unitOfWork, IGamePlayerService players, IGameTemplateCatalog? templates = null) : SummonHandlerSupport(unitOfWork, templates), IQueryHandler<GetSummonResultQuery, BaseResponse<SummonResultDto>>
{
    private readonly IGamePlayerService _players = players;
    public async Task<BaseResponse<SummonResultDto>> Handle(GetSummonResultQuery request, CancellationToken ct)
    {
        return BaseResponse<SummonResultDto>.SuccessResponse(await ExecuteAsync(request.UserId, request.RequestId, ct));
    }

    private async Task<SummonResultDto> ExecuteAsync(string userId, Guid requestId, CancellationToken ct)
    {
        var player = await _players.GetPlayerByUserIdAsync(userId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var playerId = player.Id;
        var batch = await _unitOfWork.ReadOnlyRepository<HrkSummonBatch>().Query()
            .SingleOrDefaultAsync(x => x.PlayerId == playerId && x.RequestId == requestId, ct) ?? throw new BusinessException("SUMMON_RESULT_NOT_FOUND", "Chưa tìm thấy kết quả chiêu mộ.", BusinessErrorKind.NotFound);
        return ReadResult(batch);
    }
}
