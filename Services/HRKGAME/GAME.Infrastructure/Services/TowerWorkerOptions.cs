namespace GAME.Infrastructure.Services;

public sealed class TowerWorkerOptions
{
    public int BetweenFloorsMs { get; set; } = 150;
    public int IdlePollMs { get; set; } = 1000;
    public int ErrorBackoffMs { get; set; } = 2000;
}
