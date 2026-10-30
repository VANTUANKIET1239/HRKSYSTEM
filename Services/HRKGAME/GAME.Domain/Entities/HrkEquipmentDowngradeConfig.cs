namespace GAME.Domain.Entities;

public class HrkEquipmentDowngradeConfig
{
    public int Id { get; set; }
    public int FromLevel { get; set; }
    public int ToLevel { get; set; }
    public decimal GoldRefundPercent { get; set; }
    public decimal StoneRefundPercent { get; set; }
    public bool RefundCharm { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}
