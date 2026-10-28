using System.Text.Json;
using System.Text.Json.Serialization;
using Core.HealthChecks.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Core.HealthChecks.Responses;

internal static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public static Task WriteAsync(HttpContext http, HealthReport report, string kind)
    {
        var identity = http.RequestServices.GetRequiredService<HealthServiceIdentity>();
        var result = new ServiceHealthReport
        {
            Service = identity.Service, InstanceId = identity.InstanceId, Kind = kind,
            Status = Status(report.Status), CheckedAtUtc = DateTimeOffset.UtcNow,
            DurationMs = report.TotalDuration.TotalMilliseconds,
            Checks = report.Entries.Select(entry => new ComponentHealth
            {
                Name = entry.Key, Status = Status(entry.Value.Status), DurationMs = entry.Value.Duration.TotalMilliseconds,
                ErrorCode = ErrorCode(entry.Value)
            }).ToArray()
        };
        // Never serialize exception messages, stacks, connection strings or arbitrary health-check data.
        return http.Response.WriteAsJsonAsync(result, Json, cancellationToken: http.RequestAborted);
    }

    private static string Status(HealthStatus status) => status == HealthStatus.Healthy ? HealthStatuses.Healthy : HealthStatuses.Unhealthy;

    private static string? ErrorCode(HealthReportEntry entry)
    {
        if (entry.Status == HealthStatus.Healthy) return null;
        if (entry.Exception is TimeoutException or OperationCanceledException) return "Timeout";
        if (entry.Data.TryGetValue("errorCode", out var value) && value is string code && code.Length is > 0 and <= 64 &&
            code.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.')) return code;
        return "CheckFailed";
    }
}
