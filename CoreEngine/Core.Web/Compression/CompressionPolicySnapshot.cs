using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Core.Web.Compression;

// Copy configuration once per request so a reload never changes a response midway through writing.
internal sealed record CompressionPolicySnapshot(string Name, CompressionLevel Level, int MinimumBodyBytes,
    int SlowCompressionMilliseconds);
