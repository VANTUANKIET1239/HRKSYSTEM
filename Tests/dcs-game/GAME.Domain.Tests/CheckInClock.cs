using System.Text.Json;
using System.Globalization;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Events;
using GAME.Application.Features.Queries.CheckIn;
using GAME.Domain.Entities;
using GAME.Infrastructure.Messaging;
using Xunit;

namespace GAME.Domain.Tests;

internal sealed class CheckInClock(DateTime utcNow) : TimeProvider
{
    public DateTime UtcNow { get; set; } = utcNow;
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(UtcNow, DateTimeKind.Utc));
}
