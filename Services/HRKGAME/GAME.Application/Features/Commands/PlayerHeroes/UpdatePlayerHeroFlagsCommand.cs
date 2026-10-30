using Core.Application.CQRS;
using Core.Common.Errors;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Commands.PlayerHeroes;
public record UpdatePlayerHeroFlagsCommand(long HeroId, UpdatePlayerHeroFlagsRequest Request) : ICommand<BaseResponse<PlayerHeroFlagsDto>>;
