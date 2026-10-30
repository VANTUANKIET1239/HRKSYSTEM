namespace GAME.Domain.Entities;

public sealed class HrkPlayerRankingStats
{
    public long PlayerId { get; set; }
    public long CurrentFormationPower { get; set; }
    public long PeakFormationPower { get; set; }
    public DateTime PeakPowerAchievedAtUtc { get; set; }
    public long TotalDiamondsSpent { get; set; }
    public DateTime DiamondsSpentAchievedAtUtc { get; set; }
    public DateTime TrackingStartedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public bool PowerInitialized { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public HrkPlayer Player { get; set; } = null!;

    public void RecordFormationPower(long power, DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(power);
        CurrentFormationPower = power;
        if (!PowerInitialized || power > PeakFormationPower)
        {
            PeakFormationPower = Math.Max(PeakFormationPower, power);
            PeakPowerAchievedAtUtc = nowUtc;
        }
        PowerInitialized = true;
        UpdatedAtUtc = nowUtc;
    }

    public void RecordDiamondSpend(long amount, DateTime nowUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        if (amount == 0) return;
        TotalDiamondsSpent = checked(TotalDiamondsSpent + amount);
        DiamondsSpentAchievedAtUtc = nowUtc;
        UpdatedAtUtc = nowUtc;
    }
}
