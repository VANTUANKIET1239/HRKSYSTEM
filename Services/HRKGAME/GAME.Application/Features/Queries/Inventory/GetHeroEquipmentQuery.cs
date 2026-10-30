using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;

namespace GAME.Application.Features.Queries.Inventory;

public record GetHeroEquipmentQuery(long HeroId) : IQuery<BaseResponse<HeroEquipmentDto>>;
