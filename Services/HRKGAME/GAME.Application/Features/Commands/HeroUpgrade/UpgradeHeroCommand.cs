using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.HeroUpgrade;

namespace GAME.Application.Features.Commands.HeroUpgrade;

public sealed record UpgradeHeroCommand(

    string UserId,

    long HeroId,

    int Levels

)

    : ICommand<BaseResponse<PlayerHeroDetailDto>>, IPlayerPowerChangeCommand;
