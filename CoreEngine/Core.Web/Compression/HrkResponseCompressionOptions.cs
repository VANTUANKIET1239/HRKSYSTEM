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
