using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Core.Web.Compression;

public static class HrkResponseCompressionExtensions
{
    public static IServiceCollection AddHrkResponseCompression(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HrkResponseCompressionOptions>()
            .Bind(configuration.GetSection(HrkResponseCompressionOptions.SectionName)).ValidateOnStart();
        services.PostConfigure<HrkResponseCompressionOptions>(options =>
        {
            if (options.Policies is null) return;
            foreach (var policy in options.Policies.Values)
                if (policy is not null) policy.AllowedEncodings ??= ["br", "gzip"];
        });
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<HrkResponseCompressionOptions>, HrkCompressionOptionsValidator>());
        return services;
    }

    /// <summary>Call after UseRouting and before authentication/authorization and endpoints.</summary>
    public static IApplicationBuilder UseHrkResponseCompression(this IApplicationBuilder app) =>
        app.UseMiddleware<HrkResponseCompressionMiddleware>();
}
