using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerHeroes;

namespace GAME.Application.Features.Commands.PlayerHeroes.SwapHeroEquipment;

public record SwapHeroEquipmentCommand(long SourceHeroId, SwapHeroEquipmentRequestDto Request) : ICommand<BaseResponse<SwapHeroEquipmentResultDto>>;

public sealed class SwapHeroEquipmentCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IPlayerReadModels playerReadModels,
    IGameRequestContext actor)
    : PlayerHeroesHandlerSupport(), ICommandHandler<SwapHeroEquipmentCommand, BaseResponse<SwapHeroEquipmentResultDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = gamePlayerService;
    private readonly IPlayerReadModels _playerReadModels = playerReadModels;
    private readonly IGameRequestContext _actor = actor;
    public async Task<BaseResponse<SwapHeroEquipmentResultDto>> Handle(SwapHeroEquipmentCommand request, CancellationToken cancellationToken)
    {
        if (request.Request == null || request.Request.TargetHeroId <= 0)
        {
            return BaseResponse<SwapHeroEquipmentResultDto>.FailResponse("Võ tướng đích không hợp lệ.", statusCode: 400);
        }

        if (request.SourceHeroId == request.Request.TargetHeroId)
        {
            return BaseResponse<SwapHeroEquipmentResultDto>.FailResponse("Không thể hoán đổi trang bị với chính võ tướng này.", statusCode: 400);
        }

        {
            var userId = _actor.UserId;
            var result = await ExecuteAsync(userId, request.SourceHeroId, request.Request.TargetHeroId, cancellationToken);
            return BaseResponse<SwapHeroEquipmentResultDto>.SuccessResponse(result, "Hoán đổi toàn bộ bộ trang bị giữa hai võ tướng thành công.");
        }
    }

    private async Task<SwapHeroEquipmentResultDto> ExecuteAsync(string userId, long sourceHeroId, long targetHeroId, CancellationToken cancellationToken = default)
    {
        if (sourceHeroId == targetHeroId)
        {
            throw new BusinessException("INVALID_REQUEST", "Hai võ tướng hoán đổi phải khác nhau.", BusinessErrorKind.Validation);
        }

        var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken) ?? throw new BusinessException("PLAYER_NOT_FOUND", "Không tìm thấy thông tin người chơi.", BusinessErrorKind.NotFound);
        var sourceHero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .FirstOrDefaultAsync(h => h.Id == sourceHeroId && h.PlayerId == player.Id && h.IsActive, cancellationToken) ?? throw BusinessException.Localized("HERO_NOT_FOUND", $"Không tìm thấy võ tướng nguồn (ID: {sourceHeroId}) hoặc không thuộc quyền sở hữu.", BusinessErrorKind.NotFound);
        var targetHero = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .FirstOrDefaultAsync(h => h.Id == targetHeroId && h.PlayerId == player.Id && h.IsActive, cancellationToken) ?? throw BusinessException.Localized("HERO_NOT_FOUND", $"Không tìm thấy võ tướng đích (ID: {targetHeroId}) hoặc không thuộc quyền sở hữu.", BusinessErrorKind.NotFound);
        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var sourceEquipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                    .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == sourceHeroId, cancellationToken);
                var targetEquipment = await _unitOfWork.Repository<HrkPlayerEquipment>().Query()
                    .FirstOrDefaultAsync(e => e.PlayerId == player.Id && e.HeroId == targetHeroId, cancellationToken);
                bool isNewSource = sourceEquipment == null;
                if (isNewSource)
                {
                    sourceEquipment = new HrkPlayerEquipment
                    {
                        PlayerId = player.Id,
                        HeroId = sourceHeroId
                    };
                    await _unitOfWork.Repository<HrkPlayerEquipment>().AddAsync(sourceEquipment);
                }

                bool isNewTarget = targetEquipment == null;
                if (isNewTarget)
                {
                    targetEquipment = new HrkPlayerEquipment
                    {
                        PlayerId = player.Id,
                        HeroId = targetHeroId
                    };
                    await _unitOfWork.Repository<HrkPlayerEquipment>().AddAsync(targetEquipment);
                }

                var srcWeapon = sourceEquipment!.WeaponId;
                var srcArmor = sourceEquipment.ArmorId;
                var srcHelmet = sourceEquipment.HelmetId;
                var srcBoots = sourceEquipment.BootsId;
                var srcRing = sourceEquipment.RingId;
                var srcArtifact = sourceEquipment.ArtifactId;
                var tgtWeapon = targetEquipment!.WeaponId;
                var tgtArmor = targetEquipment.ArmorId;
                var tgtHelmet = targetEquipment.HelmetId;
                var tgtBoots = targetEquipment.BootsId;
                var tgtRing = targetEquipment.RingId;
                var tgtArtifact = targetEquipment.ArtifactId;
                sourceEquipment.WeaponId = tgtWeapon;
                sourceEquipment.ArmorId = tgtArmor;
                sourceEquipment.HelmetId = tgtHelmet;
                sourceEquipment.BootsId = tgtBoots;
                sourceEquipment.RingId = tgtRing;
                sourceEquipment.ArtifactId = tgtArtifact;
                targetEquipment.WeaponId = srcWeapon;
                targetEquipment.ArmorId = srcArmor;
                targetEquipment.HelmetId = srcHelmet;
                targetEquipment.BootsId = srcBoots;
                targetEquipment.RingId = srcRing;
                targetEquipment.ArtifactId = srcArtifact;
                if (!isNewSource)
                    _unitOfWork.Repository<HrkPlayerEquipment>().Update(sourceEquipment);
                if (!isNewTarget)
                    _unitOfWork.Repository<HrkPlayerEquipment>().Update(targetEquipment);
                var itemsMovingToTarget = new List<long?>
                {
                    srcWeapon,
                    srcArmor,
                    srcHelmet,
                    srcBoots,
                    srcRing,
                    srcArtifact
                }.Where(x => x.HasValue)
                    .Select(x => x!.Value).ToList();
                var itemsMovingToSource = new List<long?>
                {
                    tgtWeapon,
                    tgtArmor,
                    tgtHelmet,
                    tgtBoots,
                    tgtRing,
                    tgtArtifact
                }.Where(x => x.HasValue)
                    .Select(x => x!.Value).ToList();
                if (itemsMovingToTarget.Count > 0)
                {
                    var targetMovedItems = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                        .Where(i => itemsMovingToTarget.Contains(i.Id) && i.PlayerId == player.Id)
                        .ToListAsync(cancellationToken);
                    foreach (var itm in targetMovedItems)
                    {
                        itm.EquippedHeroId = targetHeroId;
                        itm.IsEquipped = true;
                        itm.UpdatedOn = DateTime.UtcNow;
                        _unitOfWork.Repository<HrkPlayerInventory>().Update(itm);
                    }
                }

                if (itemsMovingToSource.Count > 0)
                {
                    var sourceMovedItems = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                        .Where(i => itemsMovingToSource.Contains(i.Id) && i.PlayerId == player.Id)
                        .ToListAsync(cancellationToken);
                    foreach (var itm in sourceMovedItems)
                    {
                        itm.EquippedHeroId = sourceHeroId;
                        itm.IsEquipped = true;
                        itm.UpdatedOn = DateTime.UtcNow;
                        _unitOfWork.Repository<HrkPlayerInventory>().Update(itm);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }, cancellationToken);
        var sourceDetail = await _playerReadModels.GetPlayerHeroDetailAsync(userId, sourceHeroId, cancellationToken);
        var targetDetail = await _playerReadModels.GetPlayerHeroDetailAsync(userId, targetHeroId, cancellationToken);
        return new SwapHeroEquipmentResultDto
        {
            SourceHero = sourceDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng nguồn sau khi hoán đổi."),
            TargetHero = targetDetail ?? throw new InvalidOperationException("Không thể lấy dữ liệu võ tướng đích sau khi hoán đổi.")
        };
    }
}
