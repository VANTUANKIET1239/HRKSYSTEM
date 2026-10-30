using Core.InternalApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Oservability.Correlation;

namespace Oservability;

public static partial class InternalApiExtensions
{
    public static IServiceCollection AddHrkInternalApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddScoped<IInternalApiMetadataProvider, HrkInternalApiMetadataProvider>();
        return services.AddInternalApi(configuration);
    }
}
