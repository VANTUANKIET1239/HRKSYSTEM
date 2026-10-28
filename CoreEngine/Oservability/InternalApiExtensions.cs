using Core.InternalApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Oservability.Correlation;

namespace Oservability;

public static class InternalApiExtensions
{
    public static IServiceCollection AddHrkInternalApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IInternalApiMetadataProvider, HrkInternalApiMetadataProvider>();
        return services.AddInternalApi(configuration);
    }

    private sealed class HrkInternalApiMetadataProvider(IServiceProvider services) : IInternalApiMetadataProvider
    {
        public ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(CancellationToken cancellationToken)
        {
            var metadata = new Dictionary<string, string>();
            var correlation = services.GetService<ICorrelationContext>()?.CorrelationId;
            if (correlation is not null) metadata["x-correlation-id"] = correlation;
            return new(metadata);
        }
    }
}
