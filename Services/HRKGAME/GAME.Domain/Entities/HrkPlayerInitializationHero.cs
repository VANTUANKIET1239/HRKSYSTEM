namespace GAME.Domain.Entities;

public sealed class HrkPlayerInitializationHero
{
    public long Id { get; set; }
    public long ConfigId { get; set; }
    public int HeroTemplateId { get; set; }
    public int InitialLevel { get; set; } = 1;
    public int InitialStars { get; set; } = 1;
    public int? FormationPosition { get; set; }
    public int DisplayOrder { get; set; }
}
