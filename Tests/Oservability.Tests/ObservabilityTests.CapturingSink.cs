using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Activity.Application;
using Core.Messaging.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Oservability;
using Oservability.Correlation;
using Oservability.Logging;
using Oservability.Tracing;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Oservability.Tests;
public partial class ObservabilityTests
{
    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
