namespace Core.Common.Errors;

public enum BusinessErrorKind { Validation, NotFound, Conflict, Unauthorized, Forbidden }

/// <summary>An expected rejection. Throw through transaction cleanup before mapping at the HTTP boundary.</summary>
public class BusinessException(string code, string message, BusinessErrorKind kind = BusinessErrorKind.Conflict)
    : Exception(message)
{
    public string Code { get; } = code;
    public BusinessErrorKind Kind { get; } = kind;
    public string? MessageKey { get; init; }
    public string? MessageTemplate { get; init; }
    public IReadOnlyDictionary<string, object?>? Arguments { get; init; }

    /// <summary>Preserves a literal template and its values before interpolation for localization at the HTTP boundary.</summary>
    public static BusinessException Localized(string code, FormattableString message,
        BusinessErrorKind kind = BusinessErrorKind.Conflict)
    {
        var template = System.Text.RegularExpressions.Regex.Replace(message.Format,
            @"(?<!\{)\{(\d+)(?:,[^}:]+)?(?::[^}]+)?\}(?!\})", match => "{{p" + match.Groups[1].Value + "}}");
        return new(code, message.ToString(System.Globalization.CultureInfo.CurrentCulture), kind)
        {
            MessageTemplate = template,
            Arguments = message.GetArguments().Select((value, index) => (value, index))
                .ToDictionary(item => "p" + item.index, item => item.value)
        };
    }
}
