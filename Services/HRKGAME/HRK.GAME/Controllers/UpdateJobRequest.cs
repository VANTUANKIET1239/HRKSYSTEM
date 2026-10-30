using Core.Persistence.Repositories;
using Core.Scheduling;
using GAME.Application.Services;
using GAME.Domain.Entities;
using GAME.Infrastructure.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;

namespace HRK.GAME.Controllers;

public sealed record UpdateJobRequest(long ConfigVersion, string CronExpression, string TimeZoneId,
    string ParametersJson, bool IsEnabled, string MisfirePolicy, int MaxRetryAttempts,
    int RetryDelaySeconds, int TimeoutSeconds, int RecoveryIntervalSeconds);
