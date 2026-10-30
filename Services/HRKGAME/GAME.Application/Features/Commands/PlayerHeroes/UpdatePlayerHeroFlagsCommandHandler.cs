using Core.Application.CQRS;
using Core.Common.Errors;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Commands.PlayerHeroes;

public sealed class UpdatePlayerHeroFlagsCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService players,
    IGameRequestContext actor)
    : ICommandHandler<UpdatePlayerHeroFlagsCommand, BaseResponse<PlayerHeroFlagsDto>>
{
    public async Task<BaseResponse<PlayerHeroFlagsDto>> Handle(UpdatePlayerHeroFlagsCommand command, CancellationToken ct)
    {
        if (command.Request.IsLocked is null && command.Request.IsFavorite is null)
            throw new BusinessException("INVALID_HERO_FLAGS", "Cần cung cấp IsLocked hoặc IsFavorite.", BusinessErrorKind.Validation);
        var player = await players.GetPlayerByUserIdAsync(actor.UserId, ct) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy người chơi.", BusinessErrorKind.NotFound);
        var hero = await unitOfWork.Repository<HrkPlayerHero>().Query()
            .SingleOrDefaultAsync(x => x.Id == command.HeroId && x.PlayerId == player.Id && x.IsActive, ct) ?? throw new BusinessException("HERO_NOT_FOUND", "Không tìm thấy võ tướng.", BusinessErrorKind.NotFound);
        if (command.Request.IsLocked is { } locked)
            hero.IsLocked = locked;
        if (command.Request.IsFavorite is { } favorite)
            hero.IsFavorite = favorite;
        hero.UpdatedOn = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
        return BaseResponse<PlayerHeroFlagsDto>.SuccessResponse(new(hero.Id, hero.IsLocked, hero.IsFavorite));
    }
}
