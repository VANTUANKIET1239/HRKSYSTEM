using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Core.Application.Validation;
using Core.Application.CQRS;

namespace Core.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddCoreApplication(this IServiceCollection services, Assembly assembly)
    {
        services.AddMediatR(options => options.RegisterServicesFromAssembly(assembly));
        services.TryAddScoped<ICqrsDispatcher, CqrsDispatcher>();
        services.AddValidatorsFromAssembly(assembly);
        services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)));
        return services;
    }
}
