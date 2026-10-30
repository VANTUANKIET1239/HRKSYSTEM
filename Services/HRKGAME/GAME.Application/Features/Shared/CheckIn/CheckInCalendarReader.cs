using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Shared.CheckIn;

internal static class CheckInCalendarReader
{
    public static async Task<CurrentCheckInCalendar?> LoadAsync(IUnitOfWork unitOfWork, DateTime now, CancellationToken ct)
    {
        var calendars = await unitOfWork.ReadOnlyRepository<HrkCheckInCalendar>().Query()
            .Where(x => x.Code == CheckInCalendarPolicy.Code && x.IsPublished && x.IsActive && x.StartAtUtc <= now && x.EndAtUtc > now)
            .Take(2).ToListAsync(ct);
        if (calendars.Count == 0) return null;
        if (calendars.Count != 1) throw new InvalidOperationException("Overlapping published check-in calendars.");
        var calendar = calendars[0];
        var period = CheckInCalendarPolicy.Resolve(calendar, now);
        var configs = await unitOfWork.ReadOnlyRepository<HrkCheckInRewardConfig>().Query()
            .Where(x => x.CalendarId == calendar.Id).ToListAsync(ct);
        if (configs.Count != period.Days || configs.Select(x => x.DayOfMonth).Distinct().Count() != period.Days ||
            configs.Any(x => x.DayOfMonth < 1 || x.DayOfMonth > period.Days))
            throw new InvalidOperationException("Publish a reward for every valid day of the check-in month.");
        var days = configs.ToDictionary(x => x.DayOfMonth, x => (x, ParseRewards(x.RewardsJson)));
        return new(calendar, period.GameDate, period.NextResetUtc, days);
    }

    private static List<GenericRewardItemDto> ParseRewards(string json)
    {
        var rewards = GameRewardGranter.ParseRewards(json);
        if (rewards.Count is < 1 or > 10 || rewards.Any(x => x.Type is not
                ("GOLD" or "DIAMOND" or "ITEM" or "MATERIAL" or "EQUIPMENT" or "HERO_STONE" or "ENHANCEMENT_STONE" or "CHARM" or "UNIVERSAL_STAR_STONE")))
            throw new InvalidOperationException("Check-in supports 1-10 currency/item rewards per day; hero and experience rewards are not supported.");
        return rewards;
    }
}
