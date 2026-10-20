using System.Text.Json;

namespace Core.InternalApi;

public static class InternalApiClientNames
{
    public static string Rest(string serviceName)
    {
        ValidateServiceName(serviceName);
        return $"InternalApi.Rest:{serviceName.ToUpperInvariant()}";
    }

    internal static void ValidateServiceName(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName) || serviceName.Length > 120 ||
            serviceName.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '_' and not '-'))
            throw new ArgumentException("Service names must contain 1-120 ASCII letters, digits, dots, hyphens or underscores.");
    }
}

internal static class InternalApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
