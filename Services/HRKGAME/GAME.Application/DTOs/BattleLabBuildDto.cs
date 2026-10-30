namespace GAME.Application.DTOs;

public sealed class BattleLabBuildDto
{
    public int Level { get; set; } = 1;
    public int Stars { get; set; } = 1;
    public byte AuraTier { get; set; } = 1;
    public int RollSeed { get; set; } = 1;
    public List<BattleLabEquipmentDto> Equipment { get; set; } = [];
}
