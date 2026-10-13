namespace Core.Common.Entity.MyCompany.Shared.Responses;

public sealed record ApiError(string Code, string Message, string Type,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public interface IApiResponse
{
    string? TraceId { get; set; }
    bool Success { get; set; }
    int StatusCode { get; set; }
    string? Message { get; set; }
    List<string>? Errors { get; set; }
    ApiError? Error { get; set; }
    void ClearData();
}
