using System.Net;

namespace Core.InternalApi;

public interface IInternalApiClient
{
    /// <summary>Reads a typed body for explicitly accepted HTTP statuses, including health reports with status 503.</summary>
    Task<InternalApiResponse<TResponse>> GetResponseAsync<TResponse>(string serviceName, string api,
        IReadOnlyCollection<HttpStatusCode> acceptedStatusCodes,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default);
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
