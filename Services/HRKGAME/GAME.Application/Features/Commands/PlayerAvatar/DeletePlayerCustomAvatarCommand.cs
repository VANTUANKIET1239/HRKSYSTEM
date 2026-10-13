using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerAvatar;

namespace GAME.Application.Features.Commands.PlayerAvatar;

public sealed record DeletePlayerCustomAvatarCommand(string UserId) : ICommand<BaseResponse<PlayerProfileDto>>;

public sealed class DeletePlayerCustomAvatarCommandHandler(
    IGamePlayerService playerLookups,
    IUnitOfWork unitOfWork,
    IPlayerReadModels readModels)
    : PlayerAvatarHandlerSupport(playerLookups), ICommandHandler<DeletePlayerCustomAvatarCommand, BaseResponse<PlayerProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPlayerReadModels _readModels = readModels;
    public async Task<BaseResponse<PlayerProfileDto>> Handle(DeletePlayerCustomAvatarCommand message, CancellationToken ct) => BaseResponse<PlayerProfileDto>.SuccessResponse(await ExecuteAsync(message.UserId, ct));
    private async Task<PlayerProfileDto> ExecuteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var player = await GetRequiredPlayerAsync(userId, cancellationToken, tracked: true);
        var repository = _unitOfWork.Repository<HrkPlayerCustomAvatar>();
        var avatar = await repository.Query()
            .FirstOrDefaultAsync(x => x.PlayerId == player.Id, cancellationToken);
        if (avatar != null)
            repository.Remove(avatar);
        var defaultTemplateId = await _unitOfWork.ReadOnlyRepository<HrkAvatarTemplate>().Query()
            .Where(x => x.IsEnabled)
            .OrderByDescending(x => x.IsDefault).ThenBy(x => x.DisplayOrder)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        player.AvatarType = "TEMPLATE";
        player.AvatarTemplateId = defaultTemplateId;
        player.UpdatedOn = DateTime.UtcNow;
        _unitOfWork.Repository<HrkPlayer>().Update(player);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await _readModels.BuildProfileAsync(player, cancellationToken);
    }
}
