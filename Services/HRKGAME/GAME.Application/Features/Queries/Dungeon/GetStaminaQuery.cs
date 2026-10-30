using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Dungeon;

namespace GAME.Application.Features.Queries.Dungeon;

public sealed record GetStaminaQuery(string UserId) : IQuery<BaseResponse<DungeonStaminaDto>>;
