namespace GAME.Domain.Entities;

public class HrkDungeonStageDropPool
{
    public int Id { get; set; }
    public int StageId { get; set; }
    public int ItemTemplateId { get; set; }
    public decimal DropRate { get; set; }
    public int Weight { get; set; } = 100;
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
    public bool IsFirstClearOnly { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkDungeonStage Stage { get; set; } = null!;
    public virtual HrkItemTemplate ItemTemplate { get; set; } = null!;
}
