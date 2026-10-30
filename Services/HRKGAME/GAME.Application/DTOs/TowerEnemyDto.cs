namespace GAME.Application.DTOs;

public class TowerEnemyDto
{
    public int Position { get; set; }
    public int HeroTemplateId { get; set; }
    public string Name { get; set; } = null!;
    public string ImagePath { get; set; } = null!;
    public int Level { get; set; }
    public byte Stars { get; set; }
    public int Power { get; set; }
    public bool IsBoss { get; set; }
    public string? RarityCode { get; set; }
    public string? RarityColorHex { get; set; }
    public string? ClassCode { get; set; }
    public string? FactionCode { get; set; }
}
