namespace GAME.Domain.Entities;

public class HrkDungeonStageEnemy
{
    public int Id { get; set; }
    public int StageId { get; set; }
    public int Position { get; set; }
    public int HeroTemplateId { get; set; }
    public string? DisplayName { get; set; }
    public string? ImagePath { get; set; }
    public int Level { get; set; }
    public byte Stars { get; set; }
    public decimal StatMultiplier { get; set; } = 1m;
    public bool IsBoss { get; set; }
    public virtual HrkDungeonStage Stage { get; set; } = null!;
    public virtual HrkHeroTemplate HeroTemplate { get; set; } = null!;
}
