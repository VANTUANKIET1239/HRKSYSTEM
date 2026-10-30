namespace GAME.Application.DTOs;

public class TowerFloorDetailDto : TowerFloorDto
{
    public int PlayerPower { get; set; }
    public int EnemyPower { get; set; }
    public List<TowerEnemyDto> Enemies { get; set; } = new();
    public int RemainingLives { get; set; }
    public int MaxLives { get; set; } = 3;
    public bool CanStart { get; set; }
    public string? LockedReason { get; set; }
}
