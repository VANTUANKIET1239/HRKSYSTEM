namespace GAME.Domain.Entities;

public class HrkDungeonRun
{
    public long Id { get; set; }
    public string BattleId { get; set; } = null!;
    public string ClientRequestId { get; set; } = null!;
    public long PlayerId { get; set; }
    public int StageId { get; set; }
    public string Result { get; set; } = null!;
    public int StaminaSpent { get; set; }
    public int PlayerPower { get; set; }
    public int EnemyPower { get; set; }
    public string? FormationCode { get; set; }
    public string? FormationSnapshotJson { get; set; }
    public string? ParticipantHeroIdsJson { get; set; }
    public int RandomSeed { get; set; }
    public long GoldReward { get; set; }
    public int PlayerExpReward { get; set; }
    public int HeroExpReward { get; set; }
    public DateTime StartedOn { get; set; } = DateTime.UtcNow;
    public DateTime CompletedOn { get; set; } = DateTime.UtcNow;
    public virtual HrkPlayer Player { get; set; } = null!;
    public virtual HrkDungeonStage Stage { get; set; } = null!;
}
