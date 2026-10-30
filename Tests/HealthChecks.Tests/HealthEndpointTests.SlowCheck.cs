using System.Net;
using System.Net.Http.Json;
using Core.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace HealthChecks.Tests;
public sealed partial class HealthEndpointTests
{
    public sealed class SlowCheck() : DependencyHealthCheck("Slow")
    {
        protected override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return true;
        }
    }
}
