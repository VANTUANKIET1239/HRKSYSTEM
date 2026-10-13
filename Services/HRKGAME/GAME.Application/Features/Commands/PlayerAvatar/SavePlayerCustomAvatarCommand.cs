using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerAvatar;

namespace GAME.Application.Features.Commands.PlayerAvatar;

public sealed record SavePlayerCustomAvatarCommand(string UserId, CustomAvatarUploadDto Upload) : ICommand<BaseResponse<PlayerProfileDto>>;

public sealed class SavePlayerCustomAvatarCommandHandler(
    IGamePlayerService playerLookups,
    IUnitOfWork unitOfWork,
    IPlayerReadModels readModels)
    : PlayerAvatarHandlerSupport(playerLookups), ICommandHandler<SavePlayerCustomAvatarCommand, BaseResponse<PlayerProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IPlayerReadModels _readModels = readModels;
    public async Task<BaseResponse<PlayerProfileDto>> Handle(SavePlayerCustomAvatarCommand message, CancellationToken ct) => BaseResponse<PlayerProfileDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.Upload, ct));
    private async Task<PlayerProfileDto> ExecuteAsync(string userId, CustomAvatarUploadDto upload, CancellationToken cancellationToken = default)
    {
        var player = await GetRequiredPlayerAsync(userId, cancellationToken, tracked: true);
        var repository = _unitOfWork.Repository<HrkPlayerCustomAvatar>();
        var avatar = await repository.Query()
            .FirstOrDefaultAsync(x => x.PlayerId == player.Id, cancellationToken);
        var now = DateTime.UtcNow;
        var isNew = avatar == null;
        if (avatar == null)
        {
            avatar = new HrkPlayerCustomAvatar
            {
                PlayerId = player.Id,
                CreatedOn = now
            };
            await repository.AddAsync(avatar);
        }

        avatar.ImageData = upload.ImageData;
        avatar.ContentType = upload.ContentType;
        avatar.FileName = upload.FileName;
        avatar.FileSize = upload.ImageData.Length;
        avatar.Width = upload.Width;
        avatar.Height = upload.Height;
        avatar.ContentHash = Convert.ToHexString(SHA256.HashData(upload.ImageData)).ToLowerInvariant();
        avatar.UpdatedOn = now;
        if (!isNew)
            repository.Update(avatar);
        player.AvatarType = "CUSTOM";
        player.UpdatedOn = now;
        _unitOfWork.Repository<HrkPlayer>().Update(player);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await _readModels.BuildProfileAsync(player, cancellationToken);
    }
}
