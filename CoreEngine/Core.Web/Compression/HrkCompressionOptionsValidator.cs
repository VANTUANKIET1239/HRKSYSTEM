using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Core.Web.Compression;

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
