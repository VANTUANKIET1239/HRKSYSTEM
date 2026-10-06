using Microsoft.AspNetCore.Http;

namespace Oservability.Correlation;

// IHttpClientFactory caches handler scopes; resolve the current HTTP context at send time.
public sealed class CorrelationHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var value = accessor.HttpContext?.Items[CorrelationContext.HeaderName] as string;
        if (CorrelationContext.IsValid(value) && !request.Headers.Contains(CorrelationContext.HeaderName))
            request.Headers.TryAddWithoutValidation(CorrelationContext.HeaderName, value);
        return base.SendAsync(request, ct);
    }
}
