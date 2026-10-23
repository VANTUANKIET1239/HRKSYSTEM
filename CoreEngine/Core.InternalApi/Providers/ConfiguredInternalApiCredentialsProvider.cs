using Microsoft.Extensions.Options;

namespace Core.InternalApi;

// Default implementation. Dynamic token acquisition/refresh can replace this provider through DI.
public sealed class ConfiguredInternalApiCredentialsProvider(IOptions<InternalApiOptions> options)
    : IInternalApiCredentialsProvider
{
    public ValueTask<string?> GetAuthorizationAsync(string serviceName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!options.Value.Services.TryGetValue(serviceName, out var service))
            throw new InvalidOperationException($"Missing InternalApi service configuration for '{serviceName}'.");
        return new(service.Authorization);
    }
}
