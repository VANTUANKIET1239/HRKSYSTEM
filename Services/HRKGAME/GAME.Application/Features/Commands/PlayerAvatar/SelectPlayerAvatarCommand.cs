using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerAvatar;

namespace GAME.Application.Features.Commands.PlayerAvatar;

public sealed record SelectPlayerAvatarCommand(string UserId, int AvatarTemplateId) : ICommand<BaseResponse<PlayerProfileDto>>;

public sealed class SelectPlayerAvatarCommandHandler(
    IGamePlayerService playerLookups,
    IUnitOfWork unitOfWork,
    IPlayerReadModels readModels, IGameTemplateCatalog? templates = null)
    : PlayerAvatarHandlerSupport(playerLookups), ICommandHandler<SelectPlayerAvatarCommand, BaseResponse<PlayerProfileDto>>
{
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPlayerReadModels _readModels = readModels;
    public async Task<BaseResponse<PlayerProfileDto>> Handle(SelectPlayerAvatarCommand message, CancellationToken ct) => BaseResponse<PlayerProfileDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.AvatarTemplateId, ct));
    private async Task<PlayerProfileDto> ExecuteAsync(string userId, int avatarTemplateId, CancellationToken cancellationToken = default)
    {
        var player = await GetRequiredPlayerAsync(userId, cancellationToken, tracked: true);
        var templateExists = (await _templates.GetAllAsync<HrkAvatarTemplate>(cancellationToken)).Any(x => x.Id == avatarTemplateId && x.IsEnabled);
        if (!templateExists)
            throw new BusinessException("AVATAR_NOT_FOUND", "Avatar không tồn tại hoặc đã bị vô hiệu hóa.", BusinessErrorKind.NotFound);
        player.AvatarType = "TEMPLATE";
        player.AvatarTemplateId = avatarTemplateId;
        player.UpdatedOn = DateTime.UtcNow;
        _unitOfWork.Repository<HrkPlayer>().Update(player);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await _readModels.BuildProfileAsync(player, cancellationToken);
    }
}
