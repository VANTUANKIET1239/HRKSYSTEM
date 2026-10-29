namespace GAME.Domain.Entities;

public sealed class HrkPlayerInitializationItem
{
    public long Id { get; set; }
    public long ConfigId { get; set; }
    public int ItemTemplateId { get; set; }
    public int Quantity { get; set; }
    public int DisplayOrder { get; set; }
}
