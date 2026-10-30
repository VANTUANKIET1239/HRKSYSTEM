using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Outbox;
using GAME.Application.Common;
using GAME.Application.Events;
using GAME.Application.Features.Commands.CheckIn;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Data;
using GAME.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAME.Domain.Tests;
public sealed partial class CheckInSqlTests
{
    private sealed class FailActivity : IPlayerActivityEvents
    {
        public void Raise(PlayerActivityEvent activity) => throw new InvalidOperationException("simulated activity staging failure");
    }
}
