using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.HeroUpgrade;

namespace GAME.Application.Features.Queries.HeroUpgrade;

public sealed record GetHeroUpgradePreviewQuery(string UserId, long HeroId) : IQuery<BaseResponse<HeroUpgradePreviewDto>>;
