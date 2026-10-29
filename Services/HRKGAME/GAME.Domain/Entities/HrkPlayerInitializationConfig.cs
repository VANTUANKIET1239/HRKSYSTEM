namespace GAME.Domain.Entities;

public sealed class HrkPlayerInitializationConfig
{
    public long Id { get; set; }
    public string Code { get; set; } = "DEFAULT";
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public int InitialLevel { get; set; } = 1;
    public int InitialStamina { get; set; }
    public int MaxStamina { get; set; }
    public long InitialGold { get; set; }
    public int InitialDiamonds { get; set; }
    public int InitialUpgradeMaterials { get; set; }
    public int InventoryCapacity { get; set; }
    public int DefaultFormationTemplateId { get; set; }
    public int? DefaultAvatarTemplateId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
