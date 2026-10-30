using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Oservability.Correlation;
using Oservability.Http;
using Oservability.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Oservability.Tests;
public partial class ApiLoggingTests
{
    private sealed class Monitor(ApiLoggingOptions value) : IOptionsMonitor<ApiLoggingOptions>
    {
        public ApiLoggingOptions CurrentValue => value;

        public ApiLoggingOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<ApiLoggingOptions, string?> listener) => null;
    }
}
