using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.BattleLab;

namespace GAME.Application.Features.Queries.BattleLab;

public sealed record GetBattleLabCatalogQuery() : IQuery<BaseResponse<BattleLabCatalogDto>>;
