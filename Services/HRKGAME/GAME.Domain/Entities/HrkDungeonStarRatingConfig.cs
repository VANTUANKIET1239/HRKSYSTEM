namespace GAME.Domain.Entities;

public class HrkDungeonStarRatingConfig
{
    public int Id { get; set; }
    public int? DungeonMapId { get; set; }
    public int Stars { get; set; }
    public decimal MinRemainingHpRate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkDungeonMap? DungeonMap { get; set; }
}
