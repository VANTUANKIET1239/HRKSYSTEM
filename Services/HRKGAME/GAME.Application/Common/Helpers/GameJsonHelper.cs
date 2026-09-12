using GAME.Application.DTOs;
using System.Text.Json;

namespace GAME.Application.Common.Helpers
{
    public static class GameJsonHelper
    {
        private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Parse chuỗi JSON thành object linh hoạt, nếu lỗi parse trả về chuỗi gốc hoặc null.
        /// </summary>
        public static object? ParseJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<object>(json);
            }
            catch
            {
                return json;
            }
        }

        /// <summary>
        /// Parse chuỗi PhaseDurations của kỹ năng thành PhaseDurationDto.
        /// </summary>
        public static PhaseDurationDto? ParsePhaseDurations(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<PhaseDurationDto>(json, CaseInsensitiveOptions);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deserialize an toàn kiểu generic T, không bắn exception nếu chuỗi JSON không đúng định dạng.
        /// </summary>
        public static T? Deserialize<T>(string? json, JsonSerializerOptions? options = null) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(json, options ?? CaseInsensitiveOptions);
            }
            catch
            {
                return null;
            }
        }
    }
}
