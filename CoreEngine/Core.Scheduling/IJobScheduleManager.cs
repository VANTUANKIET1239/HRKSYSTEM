namespace Core.Scheduling;

public interface IJobScheduleManager
{
    Task SynchronizeAsync(CancellationToken ct = default);
    Task TriggerAsync(long definitionId, CancellationToken ct = default);
}
