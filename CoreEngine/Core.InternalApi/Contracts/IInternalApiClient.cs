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
