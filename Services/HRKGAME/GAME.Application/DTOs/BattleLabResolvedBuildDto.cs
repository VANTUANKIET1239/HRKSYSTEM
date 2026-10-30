namespace GAME.Application.DTOs;

public sealed class BattleLabResolvedBuildDto
{
    public int Team { get; set; }
    public int Position { get; set; }
    public List<HeroStatBreakdownDto> Breakdown { get; set; } = [];
    public List<HeroBonusAttributeDto> StarBonuses { get; set; } = [];
    public List<DungeonDroppedEquipmentDto> Equipment { get; set; } = [];
}
