using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Oservability.Http;

internal static class ApiBodySanitizer
{
    internal static object Describe(byte[] bytes, bool oversized, string? contentType, ApiLoggingOptions options)
    {
        if (oversized) return new { OmittedReason = "BodyTooLarge", LimitBytes = options.MaxBodyBytes };
        if (bytes.Length == 0) return new { OmittedReason = "EmptyBody" };
        var mediaType = contentType?.Split(';')[0].Trim();
        if (mediaType is null || !(mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)))
            return new { OmittedReason = "NonJsonBody" };
        try
        {
            var node = JsonNode.Parse(bytes);
            var allowed = new HashSet<string>(options.AllowedBodyFields, StringComparer.OrdinalIgnoreCase);
            var sanitized = Sanitize(node, allowed, false)?.ToJsonString() ?? "null";
            if (Encoding.UTF8.GetByteCount(sanitized) > options.MaxBodyBytes)
                return new { OmittedReason = "SanitizedBodyTooLarge", LimitBytes = options.MaxBodyBytes };
            return new { Body = sanitized, Redacted = true };
        }
        catch (JsonException) { return new { OmittedReason = "InvalidJsonBody" }; }
    }

    private static JsonNode? Sanitize(JsonNode? node, HashSet<string> allowed, bool allowValue)
    {
        if (node is JsonObject obj)
        {
            var result = new JsonObject();
            foreach (var field in obj)
            {
                // Never permit credential fields, even if mistakenly allowlisted.
                var key = new string(field.Key.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
                var sensitive = key.Contains("password") || key.Contains("token") || key.Contains("secret")
                    || key.Contains("authorization") || key.Contains("cookie") || key.Contains("apikey");
                result[field.Key] = sensitive ? JsonValue.Create("[REDACTED]")
                    : Sanitize(field.Value, allowed, allowed.Contains(field.Key));
            }
            return result;
        }
        if (node is JsonArray array)
            return new JsonArray(array.Select(item => Sanitize(item, allowed, allowValue)).ToArray());
        return allowValue ? node?.DeepClone() : JsonValue.Create("[REDACTED]");
    }
}
