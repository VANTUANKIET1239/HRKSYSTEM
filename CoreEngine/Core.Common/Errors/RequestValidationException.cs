namespace Core.Common.Errors;

public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : BusinessException("VALIDATION_ERROR", "Dữ liệu yêu cầu không hợp lệ.", BusinessErrorKind.Validation)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
