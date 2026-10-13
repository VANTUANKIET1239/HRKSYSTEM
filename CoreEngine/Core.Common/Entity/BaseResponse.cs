namespace Core.Common.Entity.MyCompany.Shared.Responses;

/// <summary>HTTP envelope. Message/Errors remain for compatibility with existing clients.</summary>
public class BaseResponse<T> : IApiResponse
{
    public string? TraceId { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public bool HasErrors => Error != null || Errors is { Count: > 0 };
    public void ClearData() => Data = default;
    public static BaseResponse<T> SuccessResponse(T data, string? message = null, int statusCode = 200) => new()
    {
        Success = true, Data = data, Message = message ?? "Success.", StatusCode = statusCode
    };
    public static BaseResponse<T> UnauthorizedResponse(T data, string? message = null, int statusCode = 401) =>
        FailResponse(message ?? "Authentication required.", statusCode: statusCode, code: "UNAUTHORIZED", errorType: "auth");
    public static BaseResponse<T> FailResponse(List<string> errors, string? message = null, int statusCode = 400,
        string? code = null, string? errorType = null) => new()
    {
        Success = false, StatusCode = statusCode, Errors = errors,
        Message = message ?? errors.FirstOrDefault() ?? "Request failed.",
        Error = new ApiError(code ?? DefaultCode(statusCode), message ?? errors.FirstOrDefault() ?? "Request failed.",
            errorType ?? DefaultErrorType(statusCode))
    };
    public static BaseResponse<T> FailResponse(string error, string? message = null, int statusCode = 400,
        string? code = null, string? errorType = null) => FailResponse([error], message, statusCode, code, errorType);
    public static string DefaultCode(int statusCode) => statusCode switch
    {
        400 => "VALIDATION_ERROR", 401 => "UNAUTHORIZED", 403 => "FORBIDDEN", 404 => "NOT_FOUND",
        405 => "METHOD_NOT_ALLOWED", 408 => "REQUEST_TIMEOUT", 409 => "BUSINESS_CONFLICT",
        415 => "UNSUPPORTED_MEDIA_TYPE", 422 => "VALIDATION_ERROR", 429 => "RATE_LIMITED",
        >= 500 => "INTERNAL_SERVER_ERROR", _ => "REQUEST_FAILED"
    };
    public static string DefaultErrorType(int statusCode) => statusCode switch
    {
        400 or 422 => "validation", 401 or 403 => "auth", >= 500 => "system", _ => "business"
    };
}
