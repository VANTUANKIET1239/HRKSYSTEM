namespace Core.InternalApi;

public enum InternalApiFailure
{
    HttpError,
    Unavailable,
    Timeout,
    CircuitOpen,
    InvalidResponse
}
