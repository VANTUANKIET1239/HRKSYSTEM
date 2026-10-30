namespace GAME.Domain.Entities;

public class HrkDungeonStage
{
    public int Id { get; set; }
    public int DungeonMapId { get; set; }
    public int StageNumber { get; set; }
    public string Name { get; set; } = null!;
    public string StageType { get; set; } = "NORMAL";
    public int StaminaCost { get; set; } = 6;
    public int RecommendedPower { get; set; }
    public long GoldReward { get; set; }
    public int PlayerExpReward { get; set; }
    public int HeroExpReward { get; set; }
    public long FirstClearGoldReward { get; set; }
    public string? BackgroundPath { get; set; }
    public bool IsActive { get; set; } = true;
    public virtual HrkDungeonMap DungeonMap { get; set; } = null!;
    public virtual ICollection<HrkDungeonStageEnemy> Enemies { get; set; } = new List<HrkDungeonStageEnemy>();
    public virtual ICollection<HrkDungeonStageDropPool> DropPools { get; set; } = new List<HrkDungeonStageDropPool>();
}
