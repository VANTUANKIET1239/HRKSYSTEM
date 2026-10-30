using Core.InternalApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oservability;
using Oservability.Correlation;
using Xunit;

namespace Oservability.Tests;
public sealed partial class InternalApiContextTests
{
    public sealed record Reply(int Value);
}
