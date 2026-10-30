namespace GAME.Domain.Entities;

public sealed class HrkCheckInRewardConfig
{
    public long Id { get; set; }
    public long CalendarId { get; set; }
    public int DayOfMonth { get; set; }
    public string RewardsJson { get; set; } = "[]";
}
