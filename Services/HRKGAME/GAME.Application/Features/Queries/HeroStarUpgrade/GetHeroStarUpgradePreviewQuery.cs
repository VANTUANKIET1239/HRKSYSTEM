using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.HeroStarUpgrade;

namespace GAME.Application.Features.Queries.HeroStarUpgrade;

public sealed record GetHeroStarUpgradePreviewQuery(string UserId, long HeroId) : IQuery<BaseResponse<HeroStarUpgradePreviewDto>>;
