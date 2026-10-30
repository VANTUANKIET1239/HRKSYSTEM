using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Core.Web.Compression;

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
