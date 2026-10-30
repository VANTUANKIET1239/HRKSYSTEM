namespace GAME.Application.DTOs;

public sealed class DungeonStaminaDto
{
    public int Current { get; set; }
    public int Max { get; set; }
    public int NextRecoverySeconds { get; set; }
    public int PurchaseCount { get; set; }
    public int NextPurchaseCost { get; set; }
    public int PurchaseAmount { get; set; } = 50;
}
