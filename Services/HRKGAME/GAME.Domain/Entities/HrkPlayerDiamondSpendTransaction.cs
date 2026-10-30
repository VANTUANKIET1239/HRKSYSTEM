namespace GAME.Domain.Entities;

public sealed class HrkPlayerDiamondSpendTransaction
{
    public long Id { get; set; }
    public long PlayerId { get; set; }
    public Guid RequestId { get; set; }
    public string Reason { get; set; } = "";
    public string Reference { get; set; } = "";
    public long Amount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
