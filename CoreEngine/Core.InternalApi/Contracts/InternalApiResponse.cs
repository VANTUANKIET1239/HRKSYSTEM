using System.Net;

namespace Core.InternalApi;

public sealed record InternalApiResponse<TResponse>(HttpStatusCode StatusCode, TResponse Body);
