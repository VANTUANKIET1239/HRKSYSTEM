using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.BattleLab;

namespace GAME.Application.Features.Queries.BattleLab;

public sealed class GetBattleLabCatalogQueryHandler(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null) : BattleLabHandlerSupport(unitOfWork, templates), IQueryHandler<GetBattleLabCatalogQuery, BaseResponse<BattleLabCatalogDto>>
{
    public async Task<BaseResponse<BattleLabCatalogDto>> Handle(GetBattleLabCatalogQuery message, CancellationToken ct) => BaseResponse<BattleLabCatalogDto>.SuccessResponse(await ExecuteAsync(ct));
    private Task<BattleLabCatalogDto> ExecuteAsync(CancellationToken ct) => GetCatalogAsync(ct);
}
