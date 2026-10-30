using System.Text.Json;
using Core.Persistence.Repositories;
using Core.Scheduling;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Events;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using GAME.Infrastructure.Scheduling;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GAME.Domain.Tests;
public partial class EventResetSqlTests
{
    private sealed class ProbeHandler(ProbeState state) : IScheduledJobHandler
    {
        public string HandlerKey => "SQL_PROBE";

        public Task<ScheduledJobResult> ExecuteAsync(ScheduledJobContext context, CancellationToken ct)
        {
            state.Completed.TrySetResult();
            return Task.FromResult(new ScheduledJobResult(1));
        }
    }
}
