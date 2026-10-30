using System.Diagnostics;
using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;

namespace Core.Web.Compression;

internal sealed class HrkCompressionState(CompressionPolicySnapshot policy, string encoding, bool identityAllowed)
{
    public CompressionPolicySnapshot Policy { get; } = policy;
    public string Encoding { get; } = encoding;
    public bool IdentityAllowed { get; } = identityAllowed;
    public bool ThresholdReached { get; set; }
    public long CompressionStarted { get; set; }
    public long BodyBytes { get; set; }
}
