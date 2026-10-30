namespace GAME.Application.DTOs;

public sealed class BattleLabSkillMetricDto
{
    public long ActorId { get; set; }
    public string SkillId { get; set; } = "";
    public string School { get; set; } = "";
    public int Hits { get; set; }
    public long HpDamage { get; set; }
    public int CritHits { get; set; }
}
