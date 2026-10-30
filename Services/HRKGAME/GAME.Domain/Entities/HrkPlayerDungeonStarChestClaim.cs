namespace GAME.Domain.Entities;

public class HrkPlayerDungeonStarChestClaim
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public int StarChestId { get; set; }
    public DateTime ClaimedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkDungeonMapStarChest StarChest { get; set; } = null!;
}
