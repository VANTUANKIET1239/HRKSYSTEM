using Core.Persistence.Repositories;
using GAME.Application.Templates;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.GameConfig;

public sealed record GetGameFeaturesQuery() : IQuery<BaseResponse<List<GameFeatureConfigDto>>>;
