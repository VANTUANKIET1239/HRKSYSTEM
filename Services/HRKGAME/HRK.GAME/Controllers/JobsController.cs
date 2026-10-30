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

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/jobs")]
public sealed class JobsController(IUnitOfWork unitOfWork, TowerOperationRunner runner,
    IOutboxWriter outbox, SchedulingOptions scheduling, IServiceProvider services) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await unitOfWork.ReadOnlyRepository<JobDefinition>()
        .Query().OrderBy(x => x.Id).ToListAsync(ct));

    [HttpGet("{id:long}/executions")]
    public async Task<IActionResult> Executions(long id, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        if (page is < 1 or > 100000 || pageSize is < 1 or > 100) return BadRequest("Invalid pagination.");
        return Ok(await unitOfWork.ReadOnlyRepository<JobExecution>().Query().Where(x => x.JobDefinitionId == id)
            .OrderByDescending(x => x.StartedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, UpdateJobRequest request, CancellationToken ct)
    {
        var auditId = Guid.NewGuid();
        try
        {
            var result = await runner.RunAsync("scheduling:catalog", async () =>
            {
                var definition = await unitOfWork.Repository<JobDefinition>().Query().SingleOrDefaultAsync(x => x.Id == id, ct);
                if (definition is null) return (IActionResult)NotFound();
                if (definition.ConfigVersion != request.ConfigVersion) return Conflict("Job configuration has changed. Reload before editing.");
                if (definition.HandlerKey == EventResetJobHandler.Key)
                {
                    _ = DailyEventSchedule.Parse(request.CronExpression);
                    _ = EventResetParameters.Parse(request.ParametersJson);
                    // Existing period boundaries are immutable. Changing their interpretation
                    // requires an explicit effective-date migration, not a mid-period edit.
                    var eventId = int.Parse(definition.ScopeKey, System.Globalization.CultureInfo.InvariantCulture);
                    if ((definition.CronExpression != request.CronExpression || definition.TimeZoneId != request.TimeZoneId) &&
                        await unitOfWork.ReadOnlyRepository<HrkEventPeriod>().Query().AnyAsync(x => x.EventId == eventId, ct))
                        return Conflict("Reset schedule is already in use. Changing it requires an effective-date migration.");
                }
                var before = new { definition.ConfigVersion, definition.CronExpression, definition.TimeZoneId,
                    ParametersHash = Hash(definition.ParametersJson), definition.IsEnabled, definition.MisfirePolicy, definition.MaxRetryAttempts,
                    definition.RetryDelaySeconds, definition.TimeoutSeconds, definition.RecoveryIntervalSeconds };
                definition.CronExpression = request.CronExpression;
                definition.TimeZoneId = request.TimeZoneId;
                definition.ParametersJson = request.ParametersJson;
                definition.IsEnabled = request.IsEnabled;
                definition.MisfirePolicy = request.MisfirePolicy;
                definition.MaxRetryAttempts = request.MaxRetryAttempts;
                definition.RetryDelaySeconds = request.RetryDelaySeconds;
                definition.TimeoutSeconds = request.TimeoutSeconds;
                definition.RecoveryIntervalSeconds = request.RecoveryIntervalSeconds;
                definition.ConfigVersion++;
                definition.UpdatedAtUtc = DateTime.UtcNow;
                definition.UpdatedBy = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
                JobDefinitionValidator.Validate(definition);
                var message = new PlayerActivityRecordedV1(auditId, "AdminConfigurationChanged", null, definition.UpdatedBy,
                    "JobDefinition", id.ToString(), "HRK.GAME", DateTimeOffset.UtcNow,
                    JsonSerializer.SerializeToElement(new
                    {
                        Before = before,
                        After = new { definition.ConfigVersion, definition.CronExpression, definition.TimeZoneId,
                            ParametersHash = Hash(definition.ParametersJson), definition.IsEnabled, definition.MisfirePolicy,
                            definition.MaxRetryAttempts, definition.RetryDelaySeconds, definition.TimeoutSeconds,
                            definition.RecoveryIntervalSeconds }
                    }));
                outbox.Add(message, ActivityContractNames.PlayerActivityRecordedV1, MessagingPublisherNames.GameEvents, ActivityContractNames.PlayerActivityRecordedV1);
                await unitOfWork.SaveChangesAsync(ct);
                return Ok(definition);
            }, ct);
            // The background synchronizer retries scheduler outages after the DB commit.
            return result;
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (JsonException) { return BadRequest("Invalid JSON parameters."); }
        catch (TimeZoneNotFoundException) { return BadRequest("Invalid time zone."); }
        catch (InvalidTimeZoneException) { return BadRequest("Invalid time zone."); }
    }

    [HttpPost("{id:long}/trigger")]
    public async Task<IActionResult> Trigger(long id, CancellationToken ct)
    {
        if (!scheduling.Enabled) return Conflict("Scheduling is disabled on this host.");
        var definition = await unitOfWork.ReadOnlyRepository<JobDefinition>().Query().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (definition is null) return NotFound();
        if (!definition.IsEnabled) return Conflict("Job is disabled.");
        await services.GetRequiredService<IJobScheduleManager>().TriggerAsync(id, ct);
        return Accepted();
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
