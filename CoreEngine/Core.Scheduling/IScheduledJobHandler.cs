namespace Core.Scheduling;

public interface IScheduledJobHandler
{
    string HandlerKey { get; }
    Task<ScheduledJobResult> ExecuteAsync(ScheduledJobContext context, CancellationToken ct);
}
