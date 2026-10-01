using GAME.Domain.Entities;

namespace GAME.Application.Common;

public static class EventAccessPolicy
{
    public static bool IsOpen(HrkGameEvent ev, DateTime now) =>
        ev.IsOpen && (!ev.StartTimeUtc.HasValue || now >= ev.StartTimeUtc.Value) &&
        (!ev.EndTimeUtc.HasValue || now < ev.EndTimeUtc.Value);

    public static void EnsureCanEnter(HrkGameEvent ev, int playerLevel, DateTime now)
    {
        if (!IsOpen(ev, now))
            throw new InvalidOperationException("Sự kiện chưa mở hoặc đã kết thúc.");
        if (playerLevel < ev.MinPlayerLevel)
            throw new InvalidOperationException($"Cần đạt cấp {ev.MinPlayerLevel} để tham gia.");
        if (ev.InitialLives <= 0)
            throw new InvalidOperationException("Cấu hình số mạng phải lớn hơn 0.");
        if (ev.LifeConsumeMode is not ("ON_ENTRY" or "ON_DEFEAT"))
            throw new InvalidOperationException("Cấu hình tiêu hao mạng không hợp lệ.");
    }

    public static int LivesAfter(HrkGameEvent ev, int lives, bool victory) =>
        Math.Max(0, lives - (ev.LifeConsumeMode == "ON_ENTRY" || !victory ? 1 : 0));

    public static void EnsureNewRun(HrkGameEvent ev, HrkPlayerEventPeriodProgress progress)
    {
        if (progress.IsCompleted)
            throw new InvalidOperationException("Đã hoàn thành tháp trong kỳ này.");
        if (progress.RemainingLives > 0)
            throw new InvalidOperationException("Lượt leo hiện tại vẫn còn mạng.");
        if (ev.MaxDailyRuns.HasValue && progress.CurrentRunNumber >= ev.MaxDailyRuns.Value)
            throw new InvalidOperationException("Đã đạt giới hạn lượt leo trong kỳ.");
    }
}
