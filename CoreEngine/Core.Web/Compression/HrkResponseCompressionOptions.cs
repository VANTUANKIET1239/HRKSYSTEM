using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Core.Web.Compression;

public sealed class HrkResponseCompressionOptions
{
    public const string SectionName = "ResponseCompression";
    public bool Enabled { get; set; } = true;
    public bool EnableForHttps { get; set; }
    public Dictionary<string, HrkCompressionPolicy> Policies { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Default"] = new()
    };
}

public sealed class HrkCompressionPolicy
{
    public bool Enabled { get; set; } = true;
    // Filled after binding: nonempty array defaults would be appended to configured arrays by the binder.
    public string[]? AllowedEncodings { get; set; }
    public string PreferredEncoding { get; set; } = "br";
    public CompressionLevel Level { get; set; } = CompressionLevel.Fastest;
    public int MinimumBodyBytes { get; set; } = 16 * 1024;
    /// <summary>Elapsed compression phase, including downstream IO; this is not a hard timeout.</summary>
    public int SlowCompressionMilliseconds { get; set; } = 100;
}

internal sealed class HrkCompressionOptionsValidator : IValidateOptions<HrkResponseCompressionOptions>
{
    public ValidateOptionsResult Validate(string? name, HrkResponseCompressionOptions options)
    {
        var errors = new List<string>();
        if (options.Policies is null)
            return ValidateOptionsResult.Fail("ResponseCompression:Policies is required.");
        foreach (var (key, policy) in options.Policies)
        {
            var path = $"ResponseCompression:Policies:{key}";
            if (string.IsNullOrWhiteSpace(key) || policy is null)
            {
                errors.Add($"{path} must have a name and policy.");
                continue;
            }
            if (policy.AllowedEncodings is not { Length: > 0 } ||
                policy.AllowedEncodings.Any(value => value is not ("gzip" or "br")) ||
                policy.AllowedEncodings.Distinct(StringComparer.OrdinalIgnoreCase).Count() != policy.AllowedEncodings.Length)
                errors.Add($"{path}:AllowedEncodings must contain unique gzip/br values.");
            if (policy.AllowedEncodings?.Contains(policy.PreferredEncoding) != true)
                errors.Add($"{path}:PreferredEncoding must be allowed.");
            if (policy.Level is not (CompressionLevel.Fastest or CompressionLevel.Optimal or CompressionLevel.SmallestSize))
                errors.Add($"{path}:Level must be Fastest, Optimal or SmallestSize.");
            if (policy.MinimumBodyBytes is < 0 or > 1024 * 1024)
                errors.Add($"{path}:MinimumBodyBytes must be between 0 and 1048576.");
            if (policy.SlowCompressionMilliseconds < 0)
                errors.Add($"{path}:SlowCompressionMilliseconds must be nonnegative (0 disables slow logging).");
        }
        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}

// Copy configuration once per request so a reload never changes a response midway through writing.
internal sealed record CompressionPolicySnapshot(string Name, CompressionLevel Level, int MinimumBodyBytes,
    int SlowCompressionMilliseconds);
