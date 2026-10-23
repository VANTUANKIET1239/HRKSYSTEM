using System.Net;
using Polly;

namespace Core.InternalApi;

internal static class InternalApiResilience
{
    public static readonly HttpRequestOptionsKey<bool> RetryAllowed = new("HRK.InternalApi.RetryAllowed");

    public static bool IsReadOnly(HttpMethod method) => method == HttpMethod.Get || method == HttpMethod.Head || method == HttpMethod.Options;

    public static bool IsTransientHttp(Outcome<HttpResponseMessage> outcome) =>
        outcome.Exception is HttpRequestException || outcome.Result?.StatusCode is
            HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError or
            HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout;
}
