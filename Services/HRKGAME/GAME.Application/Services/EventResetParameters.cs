using System.Globalization;
using System.Text.Json;
using Core.Persistence.Repositories;
using Core.Scheduling;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using Core.TransactionalMessaging.Entities;
using Microsoft.Extensions.Logging;

namespace GAME.Application.Services;

public sealed record EventResetParameters(int BatchSize = 100)
{
    public static EventResetParameters Parse(string json)
    {
        var value = JsonSerializer.Deserialize<EventResetParameters>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Invalid event reset parameters.");
        if (value.BatchSize is < 1 or > 1000) throw new ArgumentException("BatchSize must be between 1 and 1000.");
        return value;
    }
}
