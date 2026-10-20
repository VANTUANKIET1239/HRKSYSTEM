using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Data;

internal static class CheckInModelConfiguration
{
    public static void ConfigureCheckIn(this ModelBuilder model)
    {
        model.Entity<HrkCheckInCalendar>(entity =>
        {
            entity.ToTable("HRK_CheckInCalendars", table => table.HasTrigger("TR_HRK_CheckInCalendars_Published"));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Code, x.MonthKey }).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.MonthKey).HasMaxLength(7);
            entity.Property(x => x.TimeZoneId).HasMaxLength(128);
            entity.Property(x => x.ResetTime).HasColumnType("time(0)");
        });
        model.Entity<HrkCheckInRewardConfig>(entity =>
        {
            entity.ToTable("HRK_CheckInRewardConfigs", table => table.HasTrigger("TR_HRK_CheckInRewardConfigs_Published"));
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.CalendarId, x.DayOfMonth }).IsUnique();
            entity.HasOne<HrkCheckInCalendar>().WithMany().HasForeignKey(x => x.CalendarId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<HrkPlayerCheckInClaim>(entity =>
        {
            entity.ToTable("HRK_PlayerCheckInClaims");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.PlayerId, x.CalendarId, x.CheckInDate }).IsUnique();
            entity.HasIndex(x => new { x.PlayerId, x.RequestId }).IsUnique();
            entity.Property(x => x.CheckInDate).HasColumnType("date");
            entity.Property(x => x.ClaimedAtUtc).HasConversion(x => x, x => DateTime.SpecifyKind(x, DateTimeKind.Utc));
            entity.HasOne<HrkPlayer>().WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<HrkCheckInCalendar>().WithMany().HasForeignKey(x => x.CalendarId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
