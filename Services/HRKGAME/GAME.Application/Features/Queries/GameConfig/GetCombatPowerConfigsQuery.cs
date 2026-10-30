using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
namespace GAME.Application.Features.Queries.GameConfig;

public sealed record GetCombatPowerConfigsQuery() : IQuery<BaseResponse<List<CombatPowerConfigDto>>>;
