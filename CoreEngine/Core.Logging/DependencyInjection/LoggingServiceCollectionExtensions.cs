using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Logging.DependencyInjection;

public static class LoggingServiceCollectionExtensions
{
    public static IServiceCollection AddCoreLogging(this IServiceCollection services)
    {
        services.TryAddScoped(typeof(ILoggerService<>), typeof(LoggerService<>));
        return services;
    }
}
