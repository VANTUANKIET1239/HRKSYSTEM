using System.Collections.Concurrent;
using System.Text.Json;
using Core.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using Xunit;

namespace Core.Modules.Tests;
public partial class ManagedSchedulingTests
{
    private sealed class ProbeState
    {
        public bool FailFirst;
        public bool WaitForCancellation;
        public int Calls;
        public ConcurrentQueue<Guid> Scopes { get; } = new();
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
