using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Collections.Generic;

namespace GAME.Application.Interfaces
{
    public interface IFormationStatService
    {
        /// <summary>
        /// Parse chuỗi JSON bonus phần trăm từ config cấp độ trận pháp.
        /// </summary>
        FormationStatBonusDto ParseBonus(string? json);

        /// <summary>
        /// Áp dụng bonus phần trăm từ cấp trận pháp lên chỉ số gốc cuối cùng của hero.
        /// Chỉ tăng các chỉ số cơ bản: HP, ATK, DEF, SPD, MagicDamage, MagicResistance.
        /// </summary>
        CalculatedStatsDto ApplyFormationBonus(CalculatedStatsDto baseStats, FormationStatBonusDto? bonus);

        /// <summary>
        /// Tính toán tổng lực chiến và phân rã (BaseHeroPower, FormationBonusPower, TotalPower)
        /// cho danh sách hero và trang bị tương ứng trong trận hình.
        /// </summary>
        FormationPowerCalculationResult CalculateFormationPower(
            IEnumerable<(HrkPlayerHero Hero, HrkPlayerEquipment? Equipment)> heroEquipmentList,
            FormationStatBonusDto? bonus,
            IReadOnlyCollection<CombatPowerConfigDto> powerConfigs);
    }
}
