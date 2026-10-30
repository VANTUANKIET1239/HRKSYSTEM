namespace GAME.Application.DTOs;

public sealed class DungeonStageDto
{
    public int Id { get; set; }
    public int StageNumber { get; set; }
    public string Name { get; set; } = null!;
    public string StageType { get; set; } = null!;
    public string State { get; set; } = null!;
    public int BestStars { get; set; }
    public int StaminaCost { get; set; }
    public int RecommendedPower { get; set; }
    public int PlayerPower { get; set; }
    public int EnemyPower { get; set; }
    public long GoldReward { get; set; }
    public long FirstClearGoldReward { get; set; }
    public int PlayerExpReward { get; set; }
    public int HeroExpReward { get; set; }
    public string? BackgroundPath { get; set; }
    public List<DungeonEnemyDto> Enemies { get; set; } = new();
    public List<DungeonPossibleDropDto> PossibleDrops { get; set; } = new();
}
