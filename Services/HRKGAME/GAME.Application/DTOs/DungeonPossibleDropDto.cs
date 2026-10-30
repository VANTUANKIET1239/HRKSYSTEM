namespace GAME.Application.DTOs;

public sealed class DungeonPossibleDropDto
{
    public int ItemTemplateId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? ImagePath { get; set; }
    public string RarityCode { get; set; } = null!;
    public string RarityName { get; set; } = null!;
    public string? RarityColorHex { get; set; }
    public string CategoryCode { get; set; } = null!;
    public string? CategoryName { get; set; }
    public decimal DropRatePercent { get; set; }
}
