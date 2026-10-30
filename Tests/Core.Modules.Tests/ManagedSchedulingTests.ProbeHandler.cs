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
    private sealed class ProbeHandler(ProbeState state) : IScheduledJobHandler
    {
        private readonly Guid _scope = Guid.NewGuid();
        public string HandlerKey => "PROBE";

        public async Task<ScheduledJobResult> ExecuteAsync(ScheduledJobContext context, CancellationToken ct)
        {
            state.Scopes.Enqueue(_scope);
            int call = Interlocked.Increment(ref state.Calls);
            if (state.FailFirst && call == 1)
                throw new InvalidOperationException("Retry probe");
            if (state.WaitForCancellation)
                await Task.Delay(Timeout.Infinite, ct);
            state.Completed.TrySetResult();
            return new(1);
        }
    }
}
