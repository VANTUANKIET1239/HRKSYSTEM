namespace Core.Scheduling;

public sealed class SchedulingOptions
{
    public bool Enabled { get; set; }
    public string SchedulerName { get; set; } = "HRK";
    public string Group { get; set; } = "managed";
    public bool UsePersistentStore { get; set; } = true;
    public string? ConnectionString { get; set; }
    public int MaxConcurrency { get; set; } = 2;
    public int SynchronizationIntervalSeconds { get; set; } = 30;
}
