using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Application.Services;

/// <summary>Stages currency/items inside the caller's transaction. The caller decides how to handle pending items.</summary>
public sealed class GameRewardGranter(IUnitOfWork unitOfWork, IGamePlayerService players,
    IEquipmentInstanceFactory equipment, IGameTemplateCatalog? templates = null)
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IGamePlayerService _gamePlayerService = players;
    private readonly IEquipmentInstanceFactory _equipmentInstanceFactory = equipment;
    private readonly IGameTemplateCatalog _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    public async Task<(List<GenericRewardItemDto> granted, List<GenericRewardItemDto> pending, bool isBagFull)> GrantAsync(long playerId, List<GenericRewardItemDto> rewards, string source, CancellationToken ct, long? eventPeriodId = null, string sourceDesc = "", bool createPending = false, bool experienceHandled = false)
    {
        var granted = new List<GenericRewardItemDto>();
        var pending = new List<GenericRewardItemDto>();
        var wallet = await _gamePlayerService.GetWalletByPlayerIdAsync(playerId, ct) ?? throw new BusinessException("PLAYER_WALLET_NOT_FOUND", "Không tìm thấy ví người chơi.", BusinessErrorKind.NotFound);
        var inventory = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
            .Where(i => i.PlayerId == playerId && i.IsActive)
            .ToListAsync(ct);
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
            var template = (await _templates.GetAllAsync<HrkItemTemplate>(ct))
                .SingleOrDefault(t => r.ItemTemplateId.HasValue ? t.Id == r.ItemTemplateId : t.Code == code) ?? throw new InvalidOperationException($"Không tìm thấy vật phẩm thưởng '{code ?? r.ItemTemplateId?.ToString()}'.");
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
                    var created = await _equipmentInstanceFactory.CreateAsync(playerId, template, new EquipmentAcquisitionContext { Source = source }, ct);
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
                    if (remaining == 0)
                        break;
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
            await _unitOfWork.Repository<HrkPlayerPendingReward>().AddAsync(new HrkPlayerPendingReward { PlayerId = playerId, EventPeriodId = eventPeriodId ?? throw new InvalidOperationException("Pending tower rewards require an event period."), SourceType = "BAG_FULL_REWARD", Description = sourceDesc, RewardItemsJson = JsonSerializer.Serialize(pending), Status = "PENDING", CreatedOnUtc = DateTime.UtcNow });
        return (granted, pending, pending.Count > 0);
    }

    public static List<GenericRewardItemDto> ParseRewards(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new();
        var rewards = JsonSerializer.Deserialize<List<GenericRewardItemDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("Cấu hình thưởng không được là null.");
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


}
