using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;

namespace Core.InternalApi;

internal sealed class InternalApiClient(IHttpClientFactory clients, IOptions<InternalApiOptions> configuration,
    IInternalApiCredentialsProvider credentials, IInternalApiMetadataProvider metadataProvider,
    ILogger<InternalApiClient> logger) : IInternalApiClient
{
    public Task<TResponse> GetAsync<TResponse>(string serviceName, string api,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(serviceName, HttpMethod.Get, api, options: options, cancellationToken: cancellationToken);

    public Task<TResponse> PostAsync<TRequest, TResponse>(string serviceName, string api, TRequest request,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(serviceName, HttpMethod.Post, api, request, options, cancellationToken);

    public Task<TResponse> PutAsync<TRequest, TResponse>(string serviceName, string api, TRequest request,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(serviceName, HttpMethod.Put, api, request, options, cancellationToken);

    public Task<TResponse> PatchAsync<TRequest, TResponse>(string serviceName, string api, TRequest request,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(serviceName, HttpMethod.Patch, api, request, options, cancellationToken);

    public Task<TResponse> DeleteAsync<TResponse>(string serviceName, string api,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default) =>
        SendAsync<TResponse>(serviceName, HttpMethod.Delete, api, options: options, cancellationToken: cancellationToken);

    public async Task<TResponse> SendAsync<TResponse>(string serviceName, HttpMethod method, string api, object? request = null,
        InternalCallOptions? options = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(method);
        InternalApiClientNames.ValidateServiceName(serviceName);
        if (!configuration.Value.Services.TryGetValue(serviceName, out var service))
            throw new InvalidOperationException($"Missing InternalApi service configuration for '{serviceName}'.");
        var uri = ResolveApiUri(new Uri(service.BaseAddress!), api);
        options ??= new InternalCallOptions();
        var timeout = options.TimeoutSeconds ?? service.TimeoutSeconds;
        if (!InternalApiRegistration.ValidTimeout(timeout))
            throw new ArgumentException("Invalid internal API call timeout.", nameof(options));
        var retry = options.RetryEnabled ?? (InternalApiResilience.IsReadOnly(method) && service.Retry.Enabled);
        if (retry && service.Retry.MaxRetries == 0)
            throw new ArgumentException("Retry requires MaxRetries greater than zero in the service configuration.", nameof(options));
        if (retry && !InternalApiResilience.IsReadOnly(method) && string.IsNullOrWhiteSpace(options.IdempotencyKey))
            throw new ArgumentException("Retrying a write requires an explicit opt-in and an idempotency key.", nameof(options));
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromSeconds(timeout));
        try
        {
            var metadata = await BuildMetadataAsync(options, budget.Token);
            var authorization = options.Authorization ?? await credentials.GetAuthorizationAsync(serviceName, budget.Token);
            if (authorization is not null && (!AuthenticationHeaderValue.TryParse(authorization, out _) ||
                authorization.Length > 4096 || authorization.Any(c => c < 32 || c > 126)))
                throw new ArgumentException("Invalid internal API authorization header.");
            budget.Token.ThrowIfCancellationRequested();
            using var client = clients.CreateClient(InternalApiClientNames.Rest(serviceName));
            using var message = new HttpRequestMessage(method, uri);
            message.Options.Set(InternalApiResilience.RetryAllowed, retry);
            foreach (var (key, value) in metadata) message.Headers.Add(key, value);
            if (authorization is not null) message.Headers.Authorization = AuthenticationHeaderValue.Parse(authorization);
            if (request is not null)
            {
                message.Content = JsonContent.Create(request, request.GetType(), options: InternalApiJson.Options);
                await message.Content.LoadIntoBufferAsync(budget.Token);
            }
            using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, budget.Token);
            if (!response.IsSuccessStatusCode)
                throw new InternalApiException(serviceName, api, InternalApiFailure.HttpError, httpStatusCode: response.StatusCode);
            if (typeof(TResponse) == typeof(InternalApiEmpty)) return (TResponse)(object)new InternalApiEmpty();
            var result = await response.Content.ReadFromJsonAsync<TResponse>(InternalApiJson.Options, budget.Token);
            return result ?? throw new InternalApiException(serviceName, api, InternalApiFailure.InvalidResponse);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException ex)
        {
            throw Failure(InternalApiFailure.Timeout, ex, serviceName, method, api);
        }
        catch (HttpRequestException ex) { throw Failure(InternalApiFailure.Unavailable, ex, serviceName, method, api); }
        catch (BrokenCircuitException ex) { throw Failure(InternalApiFailure.CircuitOpen, ex, serviceName, method, api); }
        catch (JsonException ex) { throw Failure(InternalApiFailure.InvalidResponse, ex, serviceName, method, api); }
    }

    private InternalApiException Failure(InternalApiFailure failure, Exception inner, string serviceName, HttpMethod method, string api)
    {
        // Do not log payloads, metadata, credentials or remote response bodies.
        logger.LogWarning("Internal API {Service} {Method} failed: {Failure}", serviceName, method.Method, failure);
        return new(serviceName, api, failure, inner);
    }

    private static Uri ResolveApiUri(Uri baseAddress, string api)
    {
        if (string.IsNullOrWhiteSpace(api) || api != api.Trim() || api.Any(char.IsControl) ||
            api.Contains('\\') || api.Contains('#') || api.StartsWith("//", StringComparison.Ordinal))
            throw new ArgumentException("API must be a relative path or an absolute HTTP(S) URL of the configured service.", nameof(api));
        // Leading '/' is a root-relative URL, even on platforms where Uri also recognizes it as a file URI.
        var uri = api.StartsWith('/') ? new Uri(baseAddress, api) :
            Uri.TryCreate(api, UriKind.Absolute, out var absolute) ? absolute : new Uri(baseAddress, api);
        if (uri.Scheme != baseAddress.Scheme || !string.Equals(uri.IdnHost, baseAddress.IdnHost, StringComparison.OrdinalIgnoreCase) ||
            uri.Port != baseAddress.Port || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("API URL must have the same scheme, host and port as the configured service.", nameof(api));
        return uri;
    }

    private async Task<Dictionary<string, string>> BuildMetadataAsync(InternalCallOptions options, CancellationToken ct)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        Add(await metadataProvider.GetMetadataAsync(ct));
        Add(options.Metadata);
        if (options.CorrelationId is not null) result["x-correlation-id"] = options.CorrelationId;
        if (!result.ContainsKey("x-correlation-id"))
            result["x-correlation-id"] = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        ValidateIdentifier(result["x-correlation-id"], "correlation ID");
        if (options.IdempotencyKey is not null)
        {
            ValidateIdentifier(options.IdempotencyKey, "idempotency key");
            result["idempotency-key"] = options.IdempotencyKey;
        }
        if (result.Count > 32 || result.Sum(x => x.Key.Length + x.Value.Length) > 8192)
            throw new ArgumentException("Internal API metadata exceeds the allowed size.");
        return result;

        void Add(IReadOnlyDictionary<string, string> source)
        {
            foreach (var (key, value) in source)
            {
                var normalized = key.ToLowerInvariant();
                if (!normalized.StartsWith("x-", StringComparison.Ordinal) || normalized.Length is < 3 or > 64 ||
                    normalized.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-') ||
                    normalized.StartsWith("x-forwarded-", StringComparison.Ordinal) || normalized == "x-http-method-override" ||
                    value is null || value.Length > 1024 || value.Any(c => c < 32 || c > 126))
                    throw new ArgumentException("Internal API metadata must use application x-* headers and printable ASCII values (max 1024 characters).");
                result[normalized] = value;
            }
        }
    }

    private static void ValidateIdentifier(string value, string name)
    {
        if (value.Length is < 1 or > 128 || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '-' and not '_' and not '.' and not ':'))
            throw new ArgumentException($"Invalid internal API {name}.");
    }
}
