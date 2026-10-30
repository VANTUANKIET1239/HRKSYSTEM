using System.Diagnostics;
using System.IO.Compression;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Net.Http.Headers;

namespace Core.Web.Compression;
internal sealed partial class HrkCompressionProvider
{
    private sealed class SelectedCompressionProvider(string encoding, CompressionLevel level) : ICompressionProvider
    {
        public string EncodingName => encoding;
        public bool SupportsFlush => true;

        public Stream CreateStream(Stream outputStream) => encoding == "gzip" ? new GZipStream(outputStream, level, leaveOpen: true) : new BrotliStream(outputStream, level, leaveOpen: true);
    }
}
