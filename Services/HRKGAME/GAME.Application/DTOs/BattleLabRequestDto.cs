namespace GAME.Application.DTOs;

public sealed class BattleLabRequestDto
{
    public List<BattleLabSlotDto> Left { get; set; } = [];
    public List<BattleLabSlotDto> Right { get; set; } = [];
    public int Seed { get; set; } = 1;
    public int Count { get; set; } = 1;
    public int MaxRounds { get; set; } = 100;
    public decimal? DefenseConstant { get; set; }
}
