using Core.Common.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Battle;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services;

public sealed class TowerRewardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEquipmentInstanceFactory _equipmentInstanceFactory;
    private readonly ILevelExperienceService _levelExperienceService;
    private readonly IHeroProgressionStatService _progressionStats;
    private readonly ICombatPowerService _combatPower;

    public TowerRewardService(IUnitOfWork unitOfWork, IEquipmentInstanceFactory equipmentInstanceFactory,
        ILevelExperienceService levelExperienceService, IHeroProgressionStatService progressionStats, ICombatPowerService combatPower)
    {
        _unitOfWork = unitOfWork;
        _equipmentInstanceFactory = equipmentInstanceFactory;
        _levelExperienceService = levelExperienceService;
        _progressionStats = progressionStats;
        _combatPower = combatPower;
    }

    public async Task<(List<GenericRewardItemDto> granted, List<GenericRewardItemDto> pending, bool isBagFull)>
        GrantGenericRewardsAsync(long playerId, List<GenericRewardItemDto> rewards, long eventPeriodId,
            string sourceDesc, CancellationToken ct, bool createPending = true, bool experienceHandled = false)
    {
        var granted = new List<GenericRewardItemDto>();
        var pending = new List<GenericRewardItemDto>();
        var wallet = await _unitOfWork.Repository<HrkPlayerWallet>().Query().SingleAsync(w => w.PlayerId == playerId, ct);
        var inventory = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
            .Where(i => i.PlayerId == playerId && i.IsActive).ToListAsync(ct);
        int slots = inventory.Count(i => !i.IsEquipped);

        foreach (var reward in rewards)
        {
            if (reward.Quantity <= 0)
                throw new InvalidOperationException("Số lượng thưởng phải lớn hơn 0.");
            if (reward.Type.ToUpperInvariant() is "PLAYER_EXP" or "HERO_EXP")
            {
                if (!experienceHandled)
                    throw new InvalidOperationException("EXP cần có ngữ cảnh trận và người tham gia; không thể cấp như vật phẩm.");
                continue;
            }
            var r = JsonSerializer.Deserialize<GenericRewardItemDto>(JsonSerializer.Serialize(reward))!;
            switch (r.Type.ToUpperInvariant())
            {
                case "GOLD":
                    r.Name = "Vàng";
                    wallet.Gold = checked(wallet.Gold + r.Quantity);
                    granted.Add(r);
                    continue;
                case "DIAMOND":
                    r.Name = "Kim cương";
                    wallet.Diamonds = checked(wallet.Diamonds + r.Quantity);
                    granted.Add(r);
                    continue;
                // EXP is applied explicitly to battle participants, never treated as a granted item.
                case "PLAYER_EXP":
                case "HERO_EXP":
                    continue;
            }

            var code = ResolveItemCode(r);
            var template = await _unitOfWork.ReadOnlyRepository<HrkItemTemplate>().Query()
                .Include(t => t.Category).Include(t => t.Rarity)
                .Include(t => t.Attributes).ThenInclude(a => a.AttributeType)
                .SingleOrDefaultAsync(t => r.ItemTemplateId.HasValue ? t.Id == r.ItemTemplateId : t.Code == code, ct)
                ?? throw new InvalidOperationException($"Không tìm thấy vật phẩm thưởng '{code ?? r.ItemTemplateId?.ToString()}'.");
            r.ItemTemplateId = template.Id;
            r.Code = template.Code;
            r.Name = template.Name;
            r.ImagePath = template.ImagePath;
            r.RarityCode = template.Rarity.Code;
            r.RarityColorHex = template.Rarity.ColorHex;
            int remaining = r.Quantity;

            if (template.Category.IsEquipment)
            {
                while (remaining > 0 && slots < wallet.MaxCapacity)
                {
                    var created = await _equipmentInstanceFactory.CreateAsync(playerId, template,
                        new EquipmentAcquisitionContext { Source = "TOWER_REWARD" }, ct);
                    await _unitOfWork.Repository<HrkPlayerInventory>().AddAsync(created.InventoryItem);
                    await _unitOfWork.SaveChangesAsync(ct);
                    created.DroppedDto.InventoryItemId = created.InventoryItem.Id;
                    var itemReward = JsonSerializer.Deserialize<GenericRewardItemDto>(JsonSerializer.Serialize(r))!;
                    itemReward.Quantity = 1;
                    itemReward.DroppedEquipment = created.DroppedDto;
                    granted.Add(itemReward);
                    remaining--;
                    slots++;
                }
            }
            else
            {
                int stackLimit = template.IsStackable ? Math.Max(1, template.MaxStackSize) : 1;
                foreach (var stack in inventory.Where(i => i.ItemTemplateId == template.Id && !i.IsEquipped && i.Count < stackLimit))
                {
                    int add = Math.Min(remaining, stackLimit - stack.Count);
                    stack.Count += add;
                    stack.UpdatedOn = DateTime.UtcNow;
                    remaining -= add;
                    if (remaining == 0) break;
                }
                while (remaining > 0 && slots < wallet.MaxCapacity)
                {
                    int count = Math.Min(remaining, stackLimit);
                    var item = new HrkPlayerInventory
                    {
                        PlayerId = playerId,
                        ItemTemplateId = template.Id,
                        Count = count,
                        IsActive = true,
                        AcquiredOn = DateTime.UtcNow,
                        UpdatedOn = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<HrkPlayerInventory>().AddAsync(item);
                    inventory.Add(item);
                    remaining -= count;
                    slots++;
                }
                if (remaining < r.Quantity)
                {
                    var received = JsonSerializer.Deserialize<GenericRewardItemDto>(JsonSerializer.Serialize(r))!;
                    received.Quantity -= remaining;
                    granted.Add(received);
                }
            }
            if (remaining > 0)
            {
                r.Quantity = remaining;
                pending.Add(r);
            }
        }

        wallet.UpdatedOn = DateTime.UtcNow;
        if (createPending && pending.Count > 0)
            await _unitOfWork.Repository<HrkPlayerPendingReward>().AddAsync(new HrkPlayerPendingReward
            {
                PlayerId = playerId,
                EventPeriodId = eventPeriodId,
                SourceType = "BAG_FULL_REWARD",
                Description = sourceDesc,
                RewardItemsJson = JsonSerializer.Serialize(pending),
                Status = "PENDING",
                CreatedOnUtc = DateTime.UtcNow
            });
        return (granted, pending, pending.Count > 0);
    }

    public static (int Player, int Hero) GetExperience(IEnumerable<GenericRewardItemDto> rewards) =>
        (rewards.Where(r => r.Type == "PLAYER_EXP").Sum(r => r.Quantity),
         rewards.Where(r => r.Type == "HERO_EXP").Sum(r => r.Quantity));

    public static List<GenericRewardItemDto> ParseRewards(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        var rewards = JsonSerializer.Deserialize<List<GenericRewardItemDto>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Cấu hình thưởng không được là null.");
        foreach (var r in rewards)
        {
            if (string.IsNullOrWhiteSpace(r.Type) || r.Quantity <= 0)
                throw new InvalidOperationException("Loại hoặc số lượng thưởng không hợp lệ.");
            r.Type = r.Type.ToUpperInvariant();
            r.Name = string.IsNullOrWhiteSpace(r.Name) ? r.Type switch
            {
                "GOLD" => "Vàng",
                "DIAMOND" => "Kim cương",
                "PLAYER_EXP" => "EXP người chơi",
                "HERO_EXP" => "EXP võ tướng",
                "ENHANCEMENT_STONE" => $"Đá cường hóa cấp {r.StoneGrade}",
                "CHARM" => "Bùa cường hóa",
                "UNIVERSAL_STAR_STONE" => "Đá tăng sao",
                _ => r.Code ?? "Vật phẩm"
            } : r.Name;
        }
        return rewards;
    }

    public static string? ResolveItemCode(GenericRewardItemDto reward)
    {
        if (reward.ItemTemplateId.HasValue || !string.IsNullOrWhiteSpace(reward.Code))
            return reward.Code;
        return reward.Type.ToUpperInvariant() switch
        {
            "ENHANCEMENT_STONE" => reward.StoneGrade switch
            {
                1 => "ENHANCEMENT_STONE_I",
                2 => "ENHANCEMENT_STONE_II",
                3 => "ENHANCEMENT_STONE_III",
                4 => "ENHANCEMENT_STONE_IV",
                5 => "ENHANCEMENT_STONE_V",
                _ => throw new InvalidOperationException("Cấp đá thưởng không hợp lệ.")
            },
            "CHARM" => reward.CharmType?.ToUpperInvariant() switch
            {
                "LUCKY_CHARM" => "ENHANCEMENT_LUCKY_CHARM",
                "GREATER_LUCKY_CHARM" => "ENHANCEMENT_GREATER_LUCKY_CHARM",
                "PROTECTION_CHARM" => "ENHANCEMENT_PROTECTION_CHARM",
                _ => throw new InvalidOperationException("Loại bùa thưởng không hợp lệ.")
            },
            "UNIVERSAL_STAR_STONE" => "HERO_STAR_STONE",
            _ => throw new InvalidOperationException("Phần thưởng phải tham chiếu ItemTemplateId hoặc Code hợp lệ.")
        };
    }

    public async Task<List<HeroExpResultDto>> AddParticipantHeroExp(long playerId, IReadOnlyCollection<long> participantHeroIds, int exp, CancellationToken ct)
    {
        if (participantHeroIds == null || participantHeroIds.Count == 0 || exp <= 0) return new();
        var heroes = await _unitOfWork.Repository<HrkPlayerHero>().Query()
            .Include(x => x.HeroTemplate)
            .Where(x => x.PlayerId == playerId && participantHeroIds.Contains(x.Id))
            .ToListAsync(ct);

        var results = new List<HeroExpResultDto>();
        foreach (var hero in heroes)
        {
            var oldMaxExp = hero.MaxExp;
            var item = new HeroExpResultDto
            {
                PlayerHeroId = hero.Id,
                HeroName = hero.HeroTemplate.Name,
                Avatar = hero.HeroTemplate.Avatar ?? string.Empty,
                ExpGained = exp,
                OldLevel = hero.Level,
                OldExp = hero.Exp,
                OldMaxExp = oldMaxExp
            };
            var requirement = await _levelExperienceService.GetHeroRequirementAsync(hero.Level, ct);
            hero.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
            if (!requirement.IsMaxLevel) hero.Exp += exp;

            while (!requirement.IsMaxLevel && hero.MaxExp > 0 && hero.Exp >= hero.MaxExp)
            {
                hero.Exp -= hero.MaxExp;
                hero.Level++;
                requirement = await _levelExperienceService.GetHeroRequirementAsync(hero.Level, ct);
                hero.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
                if (requirement.IsMaxLevel) hero.Exp = 0;
            }
            if (hero.Level != item.OldLevel)
            {
                var growth = await _unitOfWork.ReadOnlyRepository<HrkHeroRarityUpgradeConfig>().Query()
                    .SingleAsync(c => c.RarityId == hero.HeroTemplate.RarityId, ct);
                var starGrowth = await _unitOfWork.ReadOnlyRepository<HrkHeroStarUpgradeConfig>().Query()
                    .Where(c => c.RarityId == hero.HeroTemplate.RarityId && c.NextStar == hero.Stars && c.IsEnabled)
                    .Select(c => c.GrowthBonusPercent).FirstOrDefaultAsync(ct);
                var bonuses = await (
                    from bonus in _unitOfWork.ReadOnlyRepository<HrkPlayerHeroBonusAttribute>().Query()
                    join attribute in _unitOfWork.ReadOnlyRepository<HrkAttributeType>().Query()
                        on bonus.AttributeTypeId equals attribute.Id
                    where bonus.PlayerHeroId == hero.Id
                    select new { attribute.Code, bonus.Value }).ToListAsync(ct);
                var stats = _progressionStats.Calculate(hero.HeroTemplate, hero.Level, growth.StatGrowthRate,
                    starGrowth, bonuses.Select(b => (b.Code, b.Value)));
                hero.CurrentStats = JsonSerializer.Serialize(stats);
                hero.Power = await _combatPower.CalculateAsync(stats, ct);
            }
            item.NewLevel = hero.Level;
            item.NewExp = hero.Exp;
            item.NewMaxExp = hero.MaxExp;
            hero.UpdatedOn = DateTime.UtcNow;
            results.Add(item);
        }
        return results;
    }

    public async Task AddPlayerExpAsync(HrkPlayer player, int exp, CancellationToken ct)
    {
        if (exp <= 0) return;
        var requirement = await _levelExperienceService.GetPlayerRequirementAsync(player.Level, ct);
        player.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
        if (!requirement.IsMaxLevel) player.Exp += exp;

        while (!requirement.IsMaxLevel && player.MaxExp > 0 && player.Exp >= player.MaxExp)
        {
            player.Exp -= player.MaxExp;
            player.Level++;
            requirement = await _levelExperienceService.GetPlayerRequirementAsync(player.Level, ct);
            player.MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp;
            if (requirement.IsMaxLevel) player.Exp = 0;
        }
        player.UpdatedOn = DateTime.UtcNow;
    }


}
