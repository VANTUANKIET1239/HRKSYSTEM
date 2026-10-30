using GAME.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GAME.Application.DependencyInjection;

public static class PlayerPowerPipelineRegistration
{
    /// <summary>Register after AddCoreApplication so validation runs before the transaction.</summary>
    public static IServiceCollection AddPlayerPowerChangePipeline(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(PlayerPowerChangeBehavior<,>)));
        return services;
    }
}
