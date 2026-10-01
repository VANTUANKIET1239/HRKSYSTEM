using System;
using System.Collections.Generic;
using System.Linq;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests
{
    public class TowerClimbSystemTests
    {
        private readonly EventPeriodService _periodService;

        public TowerClimbSystemTests()
        {
            // UnitOfWork is not needed for pure calculation tests
            _periodService = new EventPeriodService(null!);
        }

        #region 1. Reset Period & TimeZone Tests

        [Fact]
        public void CalculatePeriodBoundaries_At1159AM_BelongsToPreviousDayPeriod()
        {
            // 2026-10-02 04:59:59 UTC = 2026-10-02 11:59:59 Vietnam Time (UTC+7)
            var utcNow = new DateTime(2026, 10, 2, 4, 59, 59, DateTimeKind.Utc);
            var (startUtc, endUtc, periodKey) = _periodService.CalculatePeriodBoundaries(
                utcNow, "Asia/Ho_Chi_Minh", TimeSpan.FromHours(12));

            // Period should start at 12:00 PM on 2026-10-01 (05:00:00 UTC)
            var expectedStartUtc = new DateTime(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc);
            // Period should end at 12:00 PM on 2026-10-02 (05:00:00 UTC)
            var expectedEndUtc = new DateTime(2026, 10, 2, 5, 0, 0, DateTimeKind.Utc);

            Assert.Equal("2026-10-01", periodKey);
            Assert.Equal(expectedStartUtc, startUtc);
            Assert.Equal(expectedEndUtc, endUtc);
        }

        [Fact]
        public void CalculatePeriodBoundaries_At1200PM_StartsNewPeriod()
        {
            // 2026-10-02 05:00:00 UTC = 2026-10-02 12:00:00 Vietnam Time (UTC+7)
            var utcNow = new DateTime(2026, 10, 2, 5, 0, 0, DateTimeKind.Utc);
            var (startUtc, endUtc, periodKey) = _periodService.CalculatePeriodBoundaries(
                utcNow, "Asia/Ho_Chi_Minh", TimeSpan.FromHours(12));

            // Period should start at 12:00 PM on 2026-10-02 (05:00:00 UTC)
            var expectedStartUtc = new DateTime(2026, 10, 2, 5, 0, 0, DateTimeKind.Utc);
            // Period should end at 12:00 PM on 2026-10-03 (05:00:00 UTC)
            var expectedEndUtc = new DateTime(2026, 10, 3, 5, 0, 0, DateTimeKind.Utc);

            Assert.Equal("2026-10-02", periodKey);
            Assert.Equal(expectedStartUtc, startUtc);
            Assert.Equal(expectedEndUtc, endUtc);
        }

        [Fact]
        public void CalculatePeriodBoundaries_At120001PM_BelongsToNewPeriod()
        {
            // 2026-10-02 05:00:01 UTC = 2026-10-02 12:00:01 Vietnam Time (UTC+7)
            var utcNow = new DateTime(2026, 10, 2, 5, 0, 1, DateTimeKind.Utc);
            var (startUtc, endUtc, periodKey) = _periodService.CalculatePeriodBoundaries(
                utcNow, "Asia/Ho_Chi_Minh", TimeSpan.FromHours(12));

            Assert.Equal("2026-10-02", periodKey);
            Assert.Equal(new DateTime(2026, 10, 2, 5, 0, 0, DateTimeKind.Utc), startUtc);
            Assert.Equal(new DateTime(2026, 10, 3, 5, 0, 0, DateTimeKind.Utc), endUtc);
        }

        #endregion

        // Gameplay service regression coverage is in TowerReliabilityTests.
        // Do not duplicate production logic inside tests: exercise the actual policy/services instead.
        [Fact]
        public void ResetTime_IsConfigurable()
        {
            var now = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc);
            var (_, end, _) = _periodService.CalculatePeriodBoundaries(
                now, "Asia/Ho_Chi_Minh", TimeSpan.FromHours(18));
            Assert.Equal(new DateTime(2026, 10, 2, 11, 0, 0, DateTimeKind.Utc), end);
        }

        [Fact]
        public void InvalidTimeZone_DoesNotSilentlyUseUtc()
        {
            Assert.Throws<InvalidOperationException>(() => _periodService.GetEventTimeZone("invalid/timezone"));
        }
    }
}
