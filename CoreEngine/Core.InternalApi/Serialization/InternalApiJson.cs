using System.Text.Json;

namespace Core.InternalApi;

internal static class InternalApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
