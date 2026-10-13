namespace Core.Common.Errors;

public enum BusinessErrorKind { Validation, NotFound, Conflict, Unauthorized, Forbidden }

/// <summary>An expected rejection. Throw through transaction cleanup before mapping at the HTTP boundary.</summary>
public class BusinessException(string code, string message, BusinessErrorKind kind = BusinessErrorKind.Conflict)
    : Exception(message)
{
    public string Code { get; } = code;
    public BusinessErrorKind Kind { get; } = kind;
}
