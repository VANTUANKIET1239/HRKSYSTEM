namespace GAME.Application.DTOs;

public sealed class BattleLabCatalogDto
{
    public List<BattleLabItemOptionDto> Equipment { get; set; } = [];
    public List<PlayerHeroDto> Heroes { get; set; } = [];
    public decimal DefenseConstant { get; set; }
}
