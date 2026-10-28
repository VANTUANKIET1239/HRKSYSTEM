using Core.Common.Errors;
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
            throw new BusinessException("EVENT_CLOSED", "Sự kiện chưa mở hoặc đã kết thúc.", BusinessErrorKind.Conflict);
        if (playerLevel < ev.MinPlayerLevel)
            throw BusinessException.Localized("PLAYER_LEVEL_REQUIRED", $"Cần đạt cấp {ev.MinPlayerLevel} để tham gia.", BusinessErrorKind.Conflict);
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
            throw new BusinessException("TOWER_COMPLETED", "Đã hoàn thành tháp trong kỳ này.", BusinessErrorKind.Conflict);
        if (progress.RemainingLives > 0)
            throw new BusinessException("RUN_STILL_ACTIVE", "Lượt leo hiện tại vẫn còn mạng.", BusinessErrorKind.Conflict);
        if (ev.MaxDailyRuns.HasValue && progress.CurrentRunNumber >= ev.MaxDailyRuns.Value)
            throw new BusinessException("DAILY_RUN_LIMIT", "Đã đạt giới hạn lượt leo trong kỳ.", BusinessErrorKind.Conflict);
    }
}
