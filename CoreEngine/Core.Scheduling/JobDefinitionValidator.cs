using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Core.Scheduling;

public static class JobDefinitionValidator
{
    public static void Validate(JobDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.JobCode) || definition.JobCode.Length > 150 ||
            string.IsNullOrWhiteSpace(definition.HandlerKey) || definition.HandlerKey.Length > 100 ||
            definition.ScopeKey.Length > 100 || definition.Name.Length > 200 || definition.Description.Length > 1000 ||
            definition.CronExpression.Length > 120 || !CronExpression.IsValidExpression(definition.CronExpression) ||
            definition.TimeZoneId.Length > 80 || definition.ConfigVersion < 1 ||
            definition.MaxRetryAttempts is < 0 or > 10 || definition.RetryDelaySeconds is < 0 or > 3600 ||
            definition.TimeoutSeconds is < 1 or > 86400 ||
            (definition.RecoveryIntervalSeconds != 0 && definition.RecoveryIntervalSeconds is < 30 or > 86400) ||
            definition.MisfirePolicy is not ("FireOnceNow" or "DoNothing"))
            throw new ArgumentException("Invalid scheduled job configuration.");
        _ = TimeZoneInfo.FindSystemTimeZoneById(definition.TimeZoneId);
        using var json = JsonDocument.Parse(definition.ParametersJson);
        if (json.RootElement.ValueKind != JsonValueKind.Object || definition.ParametersJson.Length > 16384)
            throw new ArgumentException("Job parameters must be a JSON object up to 16 KB.");
    }
}
