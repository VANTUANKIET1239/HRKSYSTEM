namespace GAME.Domain.Entities;

public class HrkEquipmentDowngradeHistory
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public long PlayerId { get; set; }
    public long PlayerInventoryId { get; set; }
    public int OldEnhancement { get; set; }
    public int NewEnhancement { get; set; }
    public long RefundedGold { get; set; }
    public string RefundedMaterialsJson { get; set; } = "[]";
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
