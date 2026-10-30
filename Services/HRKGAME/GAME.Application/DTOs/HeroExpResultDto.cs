namespace GAME.Application.DTOs;

public sealed class HeroExpResultDto
{
    public long PlayerHeroId { get; set; }
    public string HeroName { get; set; } = null!;
    public string Avatar { get; set; } = string.Empty;
    public int ExpGained { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public int OldExp { get; set; }
    public int NewExp { get; set; }
    public int OldMaxExp { get; set; }
    public int NewMaxExp { get; set; }
}
