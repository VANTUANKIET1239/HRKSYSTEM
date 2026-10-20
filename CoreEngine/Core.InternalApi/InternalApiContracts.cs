using System.Net;

namespace Core.InternalApi;

public interface IInternalApiClient
{
    Task<TResponse> GetAsync<TResponse>(string serviceName, string api,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default);
    Task<TResponse> PostAsync<TRequest, TResponse>(string serviceName, string api, TRequest request,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default);
    Task<TResponse> PutAsync<TRequest, TResponse>(string serviceName, string api, TRequest request,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default);
    Task<TResponse> PatchAsync<TRequest, TResponse>(string serviceName, string api, TRequest request,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default);
    Task<TResponse> DeleteAsync<TResponse>(string serviceName, string api,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default);
    Task<TResponse> SendAsync<TResponse>(string serviceName, HttpMethod method, string api, object? request = null,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default);
}

// Implementations can use scoped context or a service-token provider. End-user credentials are not forwarded automatically.
public interface IInternalApiCredentialsProvider
{
    ValueTask<string?> GetAuthorizationAsync(string serviceName, CancellationToken cancellationToken);
}

public interface IInternalApiMetadataProvider
{
    ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(CancellationToken cancellationToken);
}

public enum InternalApiFailure { HttpError, Unavailable, Timeout, CircuitOpen, InvalidResponse }

public sealed class InternalApiException : Exception
{
    public string ServiceName { get; }
    public string Api { get; }
    public InternalApiFailure Failure { get; }
    public HttpStatusCode? HttpStatusCode { get; }

    internal InternalApiException(string serviceName, string api, InternalApiFailure failure,
        Exception? inner = null, HttpStatusCode? httpStatusCode = null)
        : base($"Internal HTTP call to '{serviceName}' failed ({failure}).", inner)
    {
        ServiceName = serviceName;
        Api = api;
        Failure = failure;
        HttpStatusCode = httpStatusCode;
    }
}

internal sealed class NoInternalApiMetadata : IInternalApiMetadataProvider
{
    public ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(CancellationToken cancellationToken) =>
        new(new Dictionary<string, string>());
}
