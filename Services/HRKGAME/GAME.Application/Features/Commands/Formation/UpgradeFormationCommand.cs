using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Formation;

namespace GAME.Application.Features.Commands.Formation;

public sealed record UpgradeFormationCommand(string UserId, string FormationCode) : ICommand<BaseResponse<FormationUpgradeResultDto>>;

public sealed class UpgradeFormationCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService gamePlayerService,
    IFormationStatService formationStatService,
    ICombatPowerService combatPowerService,
    IHeroStatCalculationService heroStatCalculationService, IGameTemplateCatalog? templates = null)
    : FormationCommandHandlerSupport(unitOfWork, gamePlayerService, formationStatService, combatPowerService, heroStatCalculationService, templates), ICommandHandler<UpgradeFormationCommand, BaseResponse<FormationUpgradeResultDto>>
{
    public async Task<BaseResponse<FormationUpgradeResultDto>> Handle(UpgradeFormationCommand message, CancellationToken ct) => BaseResponse<FormationUpgradeResultDto>.SuccessResponse(await ExecuteAsync(message.UserId, message.FormationCode, ct));
    private async Task<FormationUpgradeResultDto> ExecuteAsync(string userId, string formationCode, CancellationToken cancellationToken = default)
    {
        var player = await GetPlayerRequiredAsync(userId, cancellationToken);
        await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);
        var tmpl = (await _templates.GetAllAsync<HrkFormationTemplate>(cancellationToken))
            .FirstOrDefault(t => t.Code == formationCode && t.IsEnabled) ?? throw BusinessException.Localized("FORMATION_NOT_FOUND", $"Không tìm thấy trận pháp '{formationCode}'.", BusinessErrorKind.NotFound);
        var pf = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
            .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationTemplateId == tmpl.Id && f.IsActive, cancellationToken) ?? throw BusinessException.Localized("FORMATION_NOT_FOUND", $"Không tìm thấy dữ liệu trận pháp '{formationCode}'.", BusinessErrorKind.NotFound);
        if (pf.Level >= tmpl.MaxLevel)
            throw BusinessException.Localized("MAX_LEVEL_REACHED", $"Trận pháp '{tmpl.Name}' đã đạt cấp độ tối đa (Lv.{tmpl.MaxLevel}).", BusinessErrorKind.Conflict);
        var currentCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == pf.Level) ?? throw new InvalidOperationException($"Không tìm thấy cấu hình nâng cấp cho cấp {pf.Level}.");
        // Kiểm tra ví vàng
        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(player.Id, cancellationToken) ?? throw new BusinessException("PLAYER_WALLET_NOT_FOUND", "Không tìm thấy ví của người chơi.", BusinessErrorKind.NotFound);
        if (!wallet.HasEnoughGold(currentCfg.GoldCost))
            throw BusinessException.Localized("INSUFFICIENT_GOLD", $"Số dư Vàng không đủ để nâng cấp ({wallet.Gold:N0}/{currentCfg.GoldCost:N0} Vàng).");
        // Kiểm tra đá trận pháp
        var stoneItems = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
            .Where(i => i.PlayerId == player.Id && i.ItemTemplateId == currentCfg.StoneItemTemplateId && i.IsActive)
            .OrderBy(i => i.Id)
            .ToListAsync(cancellationToken);
        int totalStones = stoneItems.Sum(i => i.Count);
        if (totalStones < currentCfg.StoneCost)
            throw BusinessException.Localized("INSUFFICIENT_MATERIALS", $"Không đủ Đá Trận Pháp để nâng cấp ({totalStones}/{currentCfg.StoneCost} viên).", BusinessErrorKind.Conflict);
        // Prepare container for result
        FormationUpgradeResultDto? upgradeResult = null;
        await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            // Bắt đầu transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Trừ vàng
                wallet.DeductGold(currentCfg.GoldCost);
                // Trừ đá trận pháp
                int remainingToConsume = currentCfg.StoneCost;
                foreach (var item in stoneItems)
                {
                    if (remainingToConsume <= 0)
                        break;
                    int consume = Math.Min(item.Count, remainingToConsume);
                    item.ConsumeQuantity(consume);
                    remainingToConsume -= consume;
                }

                // Tăng cấp độ
                pf.Level += 1;
                pf.UpdatedOn = DateTime.UtcNow;
                // Tính lại lực chiến
                var newLevelCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == pf.Level);
                var newBonus = _formationStatService.ParseBonus(newLevelCfg?.StatBonusJson);
                var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);
                var placedHeroIds = GetPlacedHeroIds(pf);
                var (heroes, equipments) = await LoadHeroesAndEquipmentsAsync(player.Id, placedHeroIds, cancellationToken);
                var heroEquipmentList = placedHeroIds.Where(id => heroes.ContainsKey(id))
                    .Select(id => (heroes[id], equipments.GetValueOrDefault(id))).ToList();
                var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, newBonus, powerConfigs);
                pf.TotalPower = powerResult.TotalPower;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                var nextUpgradeCost = await BuildUpgradeCostAsync(player.Id, tmpl, pf.Level, newLevelCfg, cancellationToken);
                // Store result into outer variable (do NOT return from lambda)
                upgradeResult = new FormationUpgradeResultDto
                {
                    Code = tmpl.Code,
                    NewLevel = pf.Level,
                    NewBonus = newBonus,
                    NewWalletGold = wallet.Gold,
                    NewStoneQuantity = totalStones - currentCfg.StoneCost,
                    BaseHeroPower = powerResult.BaseHeroPower,
                    FormationBonusPower = powerResult.FormationBonusPower,
                    TotalPower = powerResult.TotalPower,
                    NextUpgradeCost = nextUpgradeCost
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }, cancellationToken);
        // Ensure we have a result and return it
        if (upgradeResult == null)
            throw new InvalidOperationException("Formation upgrade did not complete successfully.");
        return upgradeResult;
    }
}
