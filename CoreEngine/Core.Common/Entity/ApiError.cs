namespace Core.Common.Entity.MyCompany.Shared.Responses;

public sealed record ApiError(string Code, string Message, string Type,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
