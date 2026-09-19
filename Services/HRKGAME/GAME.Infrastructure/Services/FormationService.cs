using Core.Common.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GAME.Infrastructure.Services
{
    public class FormationService : IFormationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGamePlayerService _gamePlayerService;
        private readonly IFormationStatService _formationStatService;
        private readonly ICombatPowerService _combatPowerService;
        private readonly IHeroStatCalculationService _heroStatCalculationService;

        public FormationService(
            IUnitOfWork unitOfWork,
            IGamePlayerService gamePlayerService,
            IFormationStatService formationStatService,
            ICombatPowerService combatPowerService,
            IHeroStatCalculationService heroStatCalculationService)
        {
            _unitOfWork = unitOfWork;
            _gamePlayerService = gamePlayerService;
            _formationStatService = formationStatService;
            _combatPowerService = combatPowerService;
            _heroStatCalculationService = heroStatCalculationService;
        }

        public async Task<List<PlayerFormationSummaryDto>> GetPlayerFormationsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerRequiredAsync(userId, cancellationToken);
            await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);

            var templates = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
                .Where(t => t.IsEnabled)
                .Include(t => t.LevelConfigs)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync(cancellationToken);

            var playerFormations = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                .Where(f => f.PlayerId == player.Id && f.IsActive)
                .ToListAsync(cancellationToken);

            var pfByTemplateId = playerFormations.ToDictionary(f => f.FormationTemplateId);
            var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);

            var result = new List<PlayerFormationSummaryDto>();

            foreach (var tmpl in templates)
            {
                pfByTemplateId.TryGetValue(tmpl.Id, out var pf);
                int level = pf?.Level ?? 1;
                bool isSelected = pf?.IsSelected ?? (tmpl.Code == "LUC_DO");

                var currentLevelCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == level);
                var currentBonus = _formationStatService.ParseBonus(currentLevelCfg?.StatBonusJson);

                // Tính lực chiến của formation này
                int basePower = 0;
                int totalPower = 0;
                int bonusPower = 0;
                int heroCount = 0;

                if (pf != null)
                {
                    var placedHeroIds = GetPlacedHeroIds(pf);
                    heroCount = placedHeroIds.Count;

                    if (heroCount > 0)
                    {
                        var (heroes, equipments) = await LoadHeroesAndEquipmentsAsync(player.Id, placedHeroIds, cancellationToken);
                        var heroEquipmentList = placedHeroIds
                            .Where(id => heroes.ContainsKey(id))
                            .Select(id => (heroes[id], equipments.GetValueOrDefault(id)))
                            .ToList();

                        var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, currentBonus, powerConfigs);
                        basePower = powerResult.BaseHeroPower;
                        totalPower = powerResult.TotalPower;
                        bonusPower = powerResult.FormationBonusPower;
                    }
                }

                result.Add(new PlayerFormationSummaryDto
                {
                    TemplateId = tmpl.Id,
                    Code = tmpl.Code,
                    Name = tmpl.Name,
                    Description = tmpl.Description,
                    ImagePath = tmpl.ImagePath,
                    Level = level,
                    MaxLevel = tmpl.MaxLevel,
                    IsUnlocked = true,
                    IsSelected = isSelected,
                    BaseHeroPower = basePower,
                    FormationBonusPower = bonusPower,
                    TotalPower = totalPower,
                    HeroCount = heroCount,
                    CurrentBonus = currentBonus,
                    DisplayOrder = tmpl.DisplayOrder
                });
            }

            return result;
        }

        public async Task<FormationDetailDto?> GetFormationDetailAsync(string userId, string formationCode, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerRequiredAsync(userId, cancellationToken);
            await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);

            var tmpl = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
                .Where(t => t.Code == formationCode && t.IsEnabled)
                .Include(t => t.Slots.OrderBy(s => s.Slot))
                .Include(t => t.LevelConfigs.OrderBy(c => c.Level))
                .FirstOrDefaultAsync(cancellationToken);

            if (tmpl == null) return null;

            var pf = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationTemplateId == tmpl.Id && f.IsActive, cancellationToken);

            int currentLevel = pf?.Level ?? 1;
            bool isSelected = pf?.IsSelected ?? false;

            var currentCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == currentLevel);
            var nextCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == currentLevel + 1);

            var currentBonus = _formationStatService.ParseBonus(currentCfg?.StatBonusJson);
            var nextBonus = nextCfg != null ? _formationStatService.ParseBonus(nextCfg.StatBonusJson) : null;

            // Load hero & equipment
            var placedHeroIds = pf != null ? GetPlacedHeroIds(pf) : new List<long>();
            var (heroes, equipments) = await LoadHeroesAndEquipmentsAsync(player.Id, placedHeroIds, cancellationToken);
            var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);

            var heroEquipmentList = placedHeroIds
                .Where(id => heroes.ContainsKey(id))
                .Select(id => (heroes[id], equipments.GetValueOrDefault(id)))
                .ToList();

            var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, currentBonus, powerConfigs);

            // Xây dựng 5 slots
            var slotDtos = new List<FormationSlotDto>();
            for (int slotNum = 1; slotNum <= 5; slotNum++)
            {
                var slotTmpl = tmpl.Slots.FirstOrDefault(s => s.Slot == slotNum);
                long? heroId = pf != null ? GetHeroIdBySlot(pf, slotNum) : null;
                PlayerHeroDto? heroDto = null;

                if (heroId.HasValue && heroes.TryGetValue(heroId.Value, out var heroEntity))
                {
                    heroDto = GameDtoMapper.MapPlayerHero(heroEntity);
                    if (heroDto != null)
                    {
                        heroDto.Stats = powerResult.HeroFormationStats.GetValueOrDefault(heroId.Value)
                                       ?? _formationStatService.ApplyFormationBonus(_heroStatCalculationService.CalculateStats(heroEntity, equipments.GetValueOrDefault(heroId.Value)).FinalStats, currentBonus);
                        heroDto.Power = powerResult.HeroTotalPowers.GetValueOrDefault(heroId.Value);
                    }
                }

                slotDtos.Add(new FormationSlotDto
                {
                    Slot = slotNum,
                    RowType = slotTmpl?.RowType ?? (slotNum % 2 == 1 ? "FRONT" : "BACK"),
                    Lane = slotTmpl?.Lane ?? slotNum,
                    DisplayX = slotTmpl?.DisplayX ?? slotNum,
                    DisplayY = slotTmpl?.DisplayY ?? (slotNum % 2 == 1 ? 2 : 1),
                    Hero = heroDto
                });
            }

            // Chi phí nâng cấp
            var upgradeCost = await BuildUpgradeCostAsync(player.Id, tmpl, currentLevel, currentCfg, cancellationToken);

            return new FormationDetailDto
            {
                TemplateId = tmpl.Id,
                Code = tmpl.Code,
                Name = tmpl.Name,
                Description = tmpl.Description,
                ImagePath = tmpl.ImagePath,
                Level = currentLevel,
                MaxLevel = tmpl.MaxLevel,
                IsSelected = isSelected,
                IsUnlocked = true,
                Slots = slotDtos,
                BaseHeroPower = powerResult.BaseHeroPower,
                FormationBonusPower = powerResult.FormationBonusPower,
                TotalPower = powerResult.TotalPower,
                CurrentBonus = currentBonus,
                NextLevelBonus = nextBonus,
                UpgradeCost = upgradeCost
            };
        }

        public async Task<FormationDetailDto> UpdatePositionsAsync(string userId, string formationCode, UpdateFormationPositionsRequest request, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerRequiredAsync(userId, cancellationToken);
            await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);

            var tmpl = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
                .FirstOrDefaultAsync(t => t.Code == formationCode && t.IsEnabled, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy trận pháp '{formationCode}'.");

            var pf = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
                .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationTemplateId == tmpl.Id && f.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy bản ghi đội hình cho trận pháp '{formationCode}'.");

            // Validate slots 1 to 5
            var positions = request?.Positions ?? new List<SlotHeroPositionDto>();
            if (positions.Any(p => p.Slot < 1 || p.Slot > 5))
                throw new ArgumentException("Vị trí ô chiến thuật chỉ hợp lệ từ 1 đến 5.");

            if (positions.GroupBy(p => p.Slot).Any(g => g.Count() > 1))
                throw new ArgumentException("Không được gửi trùng lặp số thứ tự ô chiến thuật.");

            var assignedHeroIds = positions
                .Where(p => p.HeroId.HasValue && p.HeroId.Value > 0)
                .Select(p => p.HeroId!.Value)
                .ToList();

            if (assignedHeroIds.GroupBy(id => id).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Một võ tướng không thể xếp vào nhiều ô trong cùng một trận pháp.");

            // Validate hero ownership and active status
            if (assignedHeroIds.Count > 0)
            {
                var ownedCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                    .Where(h => h.PlayerId == player.Id && h.IsActive && assignedHeroIds.Contains(h.Id))
                    .CountAsync(cancellationToken);

                if (ownedCount != assignedHeroIds.Count)
                    throw new InvalidOperationException("Võ tướng được chọn không thuộc tài khoản người chơi hoặc không còn hoạt động.");
            }

            // Gán vị trí
            pf.Position1 = positions.FirstOrDefault(p => p.Slot == 1)?.HeroId;
            pf.Position2 = positions.FirstOrDefault(p => p.Slot == 2)?.HeroId;
            pf.Position3 = positions.FirstOrDefault(p => p.Slot == 3)?.HeroId;
            pf.Position4 = positions.FirstOrDefault(p => p.Slot == 4)?.HeroId;
            pf.Position5 = positions.FirstOrDefault(p => p.Slot == 5)?.HeroId;
            pf.UpdatedOn = DateTime.UtcNow;

            // Tính toán lại power
            var currentLevelCfg = await _unitOfWork.ReadOnlyRepository<HrkFormationLevelConfig>().Query()
                .FirstOrDefaultAsync(c => c.FormationTemplateId == tmpl.Id && c.Level == pf.Level, cancellationToken);
            var currentBonus = _formationStatService.ParseBonus(currentLevelCfg?.StatBonusJson);
            var powerConfigs = await _combatPowerService.GetConfigsAsync(cancellationToken);

            var placedHeroIds = GetPlacedHeroIds(pf);
            var (heroes, equipments) = await LoadHeroesAndEquipmentsAsync(player.Id, placedHeroIds, cancellationToken);
            var heroEquipmentList = placedHeroIds
                .Where(id => heroes.ContainsKey(id))
                .Select(id => (heroes[id], equipments.GetValueOrDefault(id)))
                .ToList();

            var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, currentBonus, powerConfigs);
            pf.TotalPower = powerResult.TotalPower;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return (await GetFormationDetailAsync(userId, formationCode, cancellationToken))!;
        }

        public async Task<FormationDetailDto> SelectFormationAsync(string userId, string formationCode, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerRequiredAsync(userId, cancellationToken);
            await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);

            var tmpl = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
                .FirstOrDefaultAsync(t => t.Code == formationCode && t.IsEnabled, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy trận pháp '{formationCode}'.");

            var allPfs = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
                .Where(f => f.PlayerId == player.Id && f.IsActive)
                .ToListAsync(cancellationToken);

            var targetPf = allPfs.FirstOrDefault(f => f.FormationTemplateId == tmpl.Id)
                ?? throw new KeyNotFoundException($"Người chơi chưa mở khóa trận pháp '{formationCode}'.");

            // Kiểm tra trận pháp có ít nhất 1 hero
            if (targetPf.Position1 == null && targetPf.Position2 == null && targetPf.Position3 == null &&
                targetPf.Position4 == null && targetPf.Position5 == null)
            {
                throw new InvalidOperationException("Không thể chọn sử dụng trận pháp khi chưa xếp bất kỳ võ tướng nào.");
            }

            // Đổi trạng thái IsSelected
            foreach (var pf in allPfs)
            {
                pf.IsSelected = (pf.Id == targetPf.Id);
                pf.UpdatedOn = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return (await GetFormationDetailAsync(userId, formationCode, cancellationToken))!;
        }

        public async Task<FormationUpgradeResultDto> UpgradeFormationAsync(string userId, string formationCode, CancellationToken cancellationToken = default)
        {
            var player = await GetPlayerRequiredAsync(userId, cancellationToken);
            await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);

            var tmpl = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
                .Include(t => t.LevelConfigs)
                .FirstOrDefaultAsync(t => t.Code == formationCode && t.IsEnabled, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy trận pháp '{formationCode}'.");

            var pf = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
                .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.FormationTemplateId == tmpl.Id && f.IsActive, cancellationToken)
                ?? throw new KeyNotFoundException($"Không tìm thấy dữ liệu trận pháp '{formationCode}'.");

            if (pf.Level >= tmpl.MaxLevel)
                throw new InvalidOperationException($"Trận pháp '{tmpl.Name}' đã đạt cấp độ tối đa (Lv.{tmpl.MaxLevel}).");

            var currentCfg = tmpl.LevelConfigs.FirstOrDefault(c => c.Level == pf.Level)
                ?? throw new InvalidOperationException($"Không tìm thấy cấu hình nâng cấp cho cấp {pf.Level}.");

            // Kiểm tra ví vàng
            var wallet = await _unitOfWork.Repository<HrkPlayerWallet>().Query()
                .FirstOrDefaultAsync(w => w.PlayerId == player.Id, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy ví của người chơi.");

            if (!wallet.HasEnoughGold(currentCfg.GoldCost))
                throw new InvalidOperationException($"Số dư Vàng không đủ để nâng cấp ({wallet.Gold:N0}/{currentCfg.GoldCost:N0} Vàng).");

            // Kiểm tra đá trận pháp
            var stoneItems = await _unitOfWork.Repository<HrkPlayerInventory>().Query()
                .Where(i => i.PlayerId == player.Id && i.ItemTemplateId == currentCfg.StoneItemTemplateId && i.IsActive)
                .OrderBy(i => i.Id)
                .ToListAsync(cancellationToken);

            int totalStones = stoneItems.Sum(i => i.Count);
            if (totalStones < currentCfg.StoneCost)
                throw new InvalidOperationException($"Không đủ Đá Trận Pháp để nâng cấp ({totalStones}/{currentCfg.StoneCost} viên).");

            // Prepare container for result
            FormationUpgradeResultDto? upgradeResult = null;

            await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                // Bắt đầu transaction
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // Trừ vàng
                    wallet.DeductGold(currentCfg.GoldCost);

                    // Trừ đá trận pháp
                    int remainingToConsume = currentCfg.StoneCost;
                    foreach (var item in stoneItems)
                    {
                        if (remainingToConsume <= 0) break;
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
                    var heroEquipmentList = placedHeroIds
                        .Where(id => heroes.ContainsKey(id))
                        .Select(id => (heroes[id], equipments.GetValueOrDefault(id)))
                        .ToList();

                    var powerResult = _formationStatService.CalculateFormationPower(heroEquipmentList, newBonus, powerConfigs);
                    pf.TotalPower = powerResult.TotalPower;

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync();

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
            });

            // Ensure we have a result and return it
            if (upgradeResult == null)
                throw new InvalidOperationException("Formation upgrade did not complete successfully.");

            return upgradeResult;
        }

        public async Task<FormationDto> GetPlayerFormationAsync(string userId, string formationName = "Main Team", CancellationToken cancellationToken = default)
        {
            var player = await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken);
            if (player == null)
            {
                return new FormationDto
                {
                    PlayerId = 0,
                    FormationName = formationName,
                    TotalPower = 0,
                    Positions = Enumerable.Range(1, 5).Select(i => new FormationPositionDto { Slot = i, Hero = null }).ToList()
                };
            }

            await EnsurePlayerFormationsInitializedAsync(player, cancellationToken);

            var selectedFormation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                .Include(f => f.FormationTemplate)
                .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.IsSelected && f.IsActive, cancellationToken);

            if (selectedFormation == null)
            {
                selectedFormation = await _unitOfWork.ReadOnlyRepository<HrkPlayerFormation>().Query()
                    .Include(f => f.FormationTemplate)
                    .FirstOrDefaultAsync(f => f.PlayerId == player.Id && f.IsActive, cancellationToken);
            }

            if (selectedFormation != null)
            {
                var detail = await GetFormationDetailAsync(userId, selectedFormation.FormationTemplate.Code, cancellationToken);
                if (detail != null)
                {
                    return new FormationDto
                    {
                        PlayerId = player.Id,
                        FormationName = detail.Name,
                        TotalPower = detail.TotalPower,
                        Positions = detail.Slots.Select(s => new FormationPositionDto
                        {
                            Slot = s.Slot,
                            Hero = s.Hero
                        }).ToList()
                    };
                }
            }

            return new FormationDto
            {
                PlayerId = player.Id,
                FormationName = formationName,
                TotalPower = 0,
                Positions = Enumerable.Range(1, 5).Select(i => new FormationPositionDto { Slot = i, Hero = null }).ToList()
            };
        }

        #region Private Helpers

        private async Task<HrkPlayer> GetPlayerRequiredAsync(string userId, CancellationToken cancellationToken)
        {
            return await _gamePlayerService.GetPlayerByUserIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy thông tin người chơi tương ứng với tài khoản đăng nhập.");
        }

        private async Task EnsurePlayerFormationsInitializedAsync(HrkPlayer player, CancellationToken cancellationToken)
        {
            var existingPfs = await _unitOfWork.Repository<HrkPlayerFormation>().Query()
                .Where(f => f.PlayerId == player.Id && f.IsActive)
                .ToListAsync(cancellationToken);

            var templates = await _unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
                .Where(t => t.IsEnabled)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync(cancellationToken);

            bool hasChanges = false;
            bool anySelected = existingPfs.Any(f => f.IsSelected);

            // Tìm hero của player để gán mặc định nếu formation đầu tiên hoàn toàn rỗng
            var activeHeroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                .Where(h => h.PlayerId == player.Id && h.IsActive)
                .OrderByDescending(h => h.Level).ThenBy(h => h.Id)
                .Take(5)
                .ToListAsync(cancellationToken);

            foreach (var tmpl in templates)
            {
                var pf = existingPfs.FirstOrDefault(f => f.FormationTemplateId == tmpl.Id);
                if (pf == null)
                {
                    bool shouldSelect = !anySelected && (tmpl.Code == "LUC_DO" || existingPfs.Count == 0);
                    if (shouldSelect) anySelected = true;

                    var newPf = new HrkPlayerFormation
                    {
                        PlayerId = player.Id,
                        FormationTemplateId = tmpl.Id,
                        Level = 1,
                        FormationName = tmpl.Name,
                        IsSelected = shouldSelect,
                        IsActive = true,
                        UpdatedOn = DateTime.UtcNow
                    };

                    // Nếu là LUC_DO và chưa có tướng nào được xếp, khởi tạo 5 tướng mẫu nếu có
                    if (tmpl.Code == "LUC_DO" && activeHeroes.Count > 0)
                    {
                        if (activeHeroes.Count > 0) newPf.Position1 = activeHeroes[0].Id;
                        if (activeHeroes.Count > 1) newPf.Position2 = activeHeroes[1].Id;
                        if (activeHeroes.Count > 2) newPf.Position3 = activeHeroes[2].Id;
                        if (activeHeroes.Count > 3) newPf.Position4 = activeHeroes[3].Id;
                        if (activeHeroes.Count > 4) newPf.Position5 = activeHeroes[4].Id;
                    }

                    await _unitOfWork.Repository<HrkPlayerFormation>().AddAsync(newPf);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<(Dictionary<long, HrkPlayerHero> Heroes, Dictionary<long, HrkPlayerEquipment> Equipments)> LoadHeroesAndEquipmentsAsync(
            long playerId, List<long> heroIds, CancellationToken cancellationToken)
        {
            if (heroIds.Count == 0)
                return (new Dictionary<long, HrkPlayerHero>(), new Dictionary<long, HrkPlayerEquipment>());

            var heroes = await _unitOfWork.ReadOnlyRepository<HrkPlayerHero>().Query()
                .Where(h => h.PlayerId == playerId && h.IsActive && heroIds.Contains(h.Id))
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Faction)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Class)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.Rarity)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.EffectType)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.TargetType)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.Scalings).ThenInclude(sc => sc.AttributeType)
                .Include(h => h.HeroTemplate).ThenInclude(ht => ht.HeroSkills).ThenInclude(hs => hs.Skill).ThenInclude(s => s.Effects).ThenInclude(e => e.StatModifiers).ThenInclude(sm => sm.AttributeType)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var equipments = await _unitOfWork.ReadOnlyRepository<HrkPlayerEquipment>().Query()
                .Where(e => e.PlayerId == playerId && heroIds.Contains(e.HeroId))
                .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Weapon).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Armor).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Helmet).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Boots).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Ring).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Rarity)
                .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Category)
                .Include(e => e.Artifact).ThenInclude(x => x!.ItemTemplate).ThenInclude(x => x.Attributes).ThenInclude(x => x.AttributeType)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return (heroes.ToDictionary(h => h.Id), equipments.ToDictionary(e => e.HeroId));
        }

        private async Task<FormationUpgradeCostDto?> BuildUpgradeCostAsync(
            long playerId, HrkFormationTemplate tmpl, int currentLevel, HrkFormationLevelConfig? currentCfg, CancellationToken cancellationToken)
        {
            if (currentLevel >= tmpl.MaxLevel || currentCfg == null)
            {
                return new FormationUpgradeCostDto
                {
                    GoldCost = 0,
                    StoneCost = 0,
                    StoneItemTemplateId = 0,
                    PlayerGold = 0,
                    PlayerStones = 0,
                    CanUpgrade = false,
                    CannotUpgradeReason = "Trận pháp đã đạt cấp độ tối đa."
                };
            }

            var wallet = await _unitOfWork.ReadOnlyRepository<HrkPlayerWallet>().Query()
                .FirstOrDefaultAsync(w => w.PlayerId == playerId, cancellationToken);
            long playerGold = wallet?.Gold ?? 0;

            int stoneCount = await _unitOfWork.ReadOnlyRepository<HrkPlayerInventory>().Query()
                .Where(i => i.PlayerId == playerId && i.ItemTemplateId == currentCfg.StoneItemTemplateId && i.IsActive)
                .SumAsync(i => i.Count, cancellationToken);

            bool canUpgrade = true;
            string? reason = null;

            if (playerGold < currentCfg.GoldCost)
            {
                canUpgrade = false;
                reason = $"Không đủ Vàng ({playerGold:N0}/{currentCfg.GoldCost:N0}).";
            }
            else if (stoneCount < currentCfg.StoneCost)
            {
                canUpgrade = false;
                reason = $"Không đủ Đá Trận Pháp ({stoneCount}/{currentCfg.StoneCost}).";
            }

            return new FormationUpgradeCostDto
            {
                GoldCost = currentCfg.GoldCost,
                StoneCost = currentCfg.StoneCost,
                StoneItemTemplateId = currentCfg.StoneItemTemplateId,
                PlayerGold = playerGold,
                PlayerStones = stoneCount,
                CanUpgrade = canUpgrade,
                CannotUpgradeReason = reason
            };
        }

        private static List<long> GetPlacedHeroIds(HrkPlayerFormation pf)
        {
            var list = new List<long>();
            if (pf.Position1.HasValue && pf.Position1.Value > 0) list.Add(pf.Position1.Value);
            if (pf.Position2.HasValue && pf.Position2.Value > 0) list.Add(pf.Position2.Value);
            if (pf.Position3.HasValue && pf.Position3.Value > 0) list.Add(pf.Position3.Value);
            if (pf.Position4.HasValue && pf.Position4.Value > 0) list.Add(pf.Position4.Value);
            if (pf.Position5.HasValue && pf.Position5.Value > 0) list.Add(pf.Position5.Value);
            return list;
        }

        private static long? GetHeroIdBySlot(HrkPlayerFormation pf, int slot) => slot switch
        {
            1 => pf.Position1,
            2 => pf.Position2,
            3 => pf.Position3,
            4 => pf.Position4,
            5 => pf.Position5,
            _ => null
        };

        #endregion
    }
}
