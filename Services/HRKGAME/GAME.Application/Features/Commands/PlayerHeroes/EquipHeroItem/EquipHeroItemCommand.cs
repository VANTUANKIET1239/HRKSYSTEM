using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerHeroes;

namespace GAME.Application.Features.Commands.PlayerHeroes.EquipHeroItem;

public record EquipHeroItemCommand(long HeroId, EquipHeroItemRequestDto Request) : ICommand<BaseResponse<PlayerHeroDetailDto>>, IPlayerPowerChangeCommand
{
    string? IPlayerPowerChangeCommand.UserId => null;
}
