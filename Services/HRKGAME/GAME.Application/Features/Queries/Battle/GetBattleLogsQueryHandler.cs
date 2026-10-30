using Core.Persistence.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Battle;

public sealed class GetBattleLogsQueryHandler(IUnitOfWork unitOfWork) : IQueryHandler<GetBattleLogsQuery, BaseResponse<List<BattleLogDto>>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    public async Task<BaseResponse<List<BattleLogDto>>> Handle(GetBattleLogsQuery request, CancellationToken cancellationToken)
    {
        var logs = await ExecuteAsync(request.BattleId, cancellationToken);
        return BaseResponse<List<BattleLogDto>>.SuccessResponse(logs, "Retrieved battle logs successfully.");
    }

    private async Task<List<BattleLogDto>> ExecuteAsync(string battleId, CancellationToken cancellationToken = default)
    {
        var logs = await _unitOfWork.ReadOnlyRepository<HrkBattleLog>().Query()
            .Where(b => b.BattleId == battleId)
            .Include(b => b.Skill)
            .OrderBy(b => b.Turn)
            .ToListAsync(cancellationToken);
        return logs.Select(b => new BattleLogDto { Id = b.Id, BattleId = b.BattleId, Turn = b.Turn, ActorHeroId = b.ActorHeroId, TargetHeroId = b.TargetHeroId, SkillId = b.SkillId, SkillName = b.Skill?.Name, Damage = b.Damage, IsCrit = b.IsCrit, BattleDetails = GameJsonHelper.ParseJson(b.BattleDetails), CreatedOn = b.CreatedOn }).ToList();
    }
}
