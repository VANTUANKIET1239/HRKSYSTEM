using Core.InternalApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Oservability.Correlation;

namespace Oservability;
public static partial class InternalApiExtensions
{
    private sealed class HrkInternalApiMetadataProvider(IServiceProvider services) : IInternalApiMetadataProvider
    {
        public ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(CancellationToken cancellationToken)
        {
            var metadata = new Dictionary<string, string>();
            var correlation = services.GetService<ICorrelationContext>()?.CorrelationId;
            if (correlation is not null)
                metadata["x-correlation-id"] = correlation;
            return new(metadata);
        }
    }
}
