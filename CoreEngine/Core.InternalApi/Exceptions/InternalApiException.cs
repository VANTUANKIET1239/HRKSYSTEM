using System.Net;

namespace Core.InternalApi;

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
