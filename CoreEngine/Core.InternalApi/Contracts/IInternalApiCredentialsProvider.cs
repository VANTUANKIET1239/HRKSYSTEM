namespace Core.InternalApi;

// Implementations can use scoped context or a service-token provider. End-user credentials are not forwarded automatically.
public interface IInternalApiCredentialsProvider
{
    ValueTask<string?> GetAuthorizationAsync(string serviceName, CancellationToken cancellationToken);
}
