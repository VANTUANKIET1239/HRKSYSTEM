using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Core.Web.Compression;
internal sealed partial class ThresholdResponseBodyFeature
{
    // JSON's PipeWriter flushes are serialization checkpoints, not the end of the response.
    // Keep only the bounded prefix across them. Explicit StartAsync/DisableBuffering bypasses it.
    private sealed class ThresholdPipeWriter(ThresholdStream stream) : PipeWriter
    {
        private readonly PipeWriter _inner = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
        public override bool CanGetUnflushedBytes => _inner.CanGetUnflushedBytes;
        public override long UnflushedBytes => _inner.UnflushedBytes;

        public override void Advance(int bytes) => _inner.Advance(bytes);
        public override Memory<byte> GetMemory(int sizeHint = 0) => _inner.GetMemory(sizeHint);
        public override Span<byte> GetSpan(int sizeHint = 0) => _inner.GetSpan(sizeHint);
        public override void CancelPendingFlush() => _inner.CancelPendingFlush();
        public override void Complete(Exception? exception = null) => _inner.Complete(exception);
        public override ValueTask CompleteAsync(Exception? exception = null) => _inner.CompleteAsync(exception);
        public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            stream.DeferEarlyFlush = true;
            try
            {
                return await _inner.FlushAsync(cancellationToken);
            }
            finally
            {
                stream.DeferEarlyFlush = false;
            }
        }
    }
}
