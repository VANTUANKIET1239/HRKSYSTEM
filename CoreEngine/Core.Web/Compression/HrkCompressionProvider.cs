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

internal sealed class HrkCompressionProvider : IResponseCompressionProvider
{
    public bool CheckRequestAcceptsCompression(HttpContext context) => context.Features.Get<HrkCompressionState>() is not null;

    public bool ShouldCompressResponse(HttpContext context)
    {
        var state = context.Features.Get<HrkCompressionState>();
        var response = context.Response;
        var type = response.ContentType?.Split(';', 2)[0].Trim();
        return state?.ThresholdReached == true && response.StatusCode is >= 200 and < 300 and not 204 and not 205 and not 206 &&
            !HttpMethods.IsHead(context.Request.Method) && !response.Headers.ContainsKey(HeaderNames.ContentEncoding) &&
            !response.Headers.ContainsKey(HeaderNames.ContentRange) && !response.Headers.ContainsKey(HeaderNames.ContentDisposition) &&
            (string.Equals(type, "application/json", StringComparison.OrdinalIgnoreCase) ||
             type?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) == true) &&
            response.GetTypedHeaders().CacheControl?.NoTransform != true;
    }

    public ICompressionProvider? GetCompressionProvider(HttpContext context)
    {
        var state = context.Features.Get<HrkCompressionState>()!;
        state.CompressionStarted = Stopwatch.GetTimestamp();
        return new SelectedCompressionProvider(state.Encoding, state.Policy.Level);
    }

    private sealed class SelectedCompressionProvider(string encoding, CompressionLevel level) : ICompressionProvider
    {
        public string EncodingName => encoding;
        public bool SupportsFlush => true;
        public Stream CreateStream(Stream outputStream) => encoding == "gzip"
            ? new GZipStream(outputStream, level, leaveOpen: true)
            : new BrotliStream(outputStream, level, leaveOpen: true);
    }

    // Explicit q=0 overrides '*'. A higher explicit identity quality opts out of compression.
    internal static (string? Encoding, bool IdentityAllowed) Negotiate(HttpContext context, HrkCompressionPolicy policy)
    {
        if (!StringWithQualityHeaderValue.TryParseList(context.Request.Headers.AcceptEncoding, out var entries) || entries.Count == 0)
            return (null, true);
        double? Quality(string value)
        {
            var matches = entries.Where(entry => string.Equals(entry.Value.Value, value, StringComparison.OrdinalIgnoreCase)).ToArray();
            return matches.Length == 0 ? null : matches.Max(entry => entry.Quality ?? 1);
        }
        var wildcard = Quality("*");
        var identity = Quality("identity");
        var identityAllowed = (identity ?? (wildcard == 0 ? 0 : 1)) > 0;
        var candidate = policy.AllowedEncodings!
            .Select(encoding => (Encoding: encoding, Quality: Quality(encoding) ?? wildcard ?? 0))
            .Where(entry => entry.Quality > 0)
            .OrderByDescending(entry => entry.Quality)
            .ThenBy(entry => entry.Encoding == policy.PreferredEncoding ? 0 : 1)
            .FirstOrDefault();
        if (identity > candidate.Quality)
            return (null, true);
        return (candidate.Encoding, identityAllowed);
    }
}
