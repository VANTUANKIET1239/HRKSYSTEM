using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Core.Web.Compression;
internal sealed partial class ThresholdResponseBodyFeature
{
    private sealed class ThresholdStream(Stream output, HrkCompressionState state) : Stream
    {
        private byte[]? _prefix;
        private int _count;
        private bool _decided;
        public bool DeferEarlyFlush { get; set; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public void BypassBuffering()
        {
            if (_decided)
                return;
            _decided = true;
            state.ThresholdReached = !state.IdentityAllowed;
        }

        private void Decide(bool reached)
        {
            if (_decided)
                return;
            _decided = true;
            state.ThresholdReached = reached || !state.IdentityAllowed;
        }

        private bool Buffer(ReadOnlySpan<byte> bytes)
        {
            if (!_decided && (long)_count + bytes.Length < state.Policy.MinimumBodyBytes)
            {
                _prefix ??= ArrayPool<byte>.Shared.Rent(state.Policy.MinimumBodyBytes);
                bytes.CopyTo(_prefix.AsSpan(_count));
                _count += bytes.Length;
                return true;
            }

            Decide(reached: true);
            return false;
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IsEmpty)
                return;
            state.BodyBytes += buffer.Length;
            if (Buffer(buffer))
                return;
            Drain();
            output.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (buffer.IsEmpty)
                return;
            state.BodyBytes += buffer.Length;
            if (Buffer(buffer.Span))
                return;
            await DrainAsync(cancellationToken);
            await output.WriteAsync(buffer, cancellationToken);
        }

        private void Drain()
        {
            if (_count == 0)
                return;
            output.Write(_prefix!, 0, _count);
            _count = 0;
        }

        private async Task DrainAsync(CancellationToken cancellationToken)
        {
            if (_count == 0)
                return;
            await output.WriteAsync(_prefix!.AsMemory(0, _count), cancellationToken);
            _count = 0;
        }

        internal async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Decide(reached: false);
            await DrainAsync(cancellationToken);
        }

        public override void Flush()
        {
            Decide(reached: false);
            Drain();
            output.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DeferEarlyFlush && !_decided)
                return;
            await CommitAsync(cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (_prefix is not null)
                ArrayPool<byte>.Shared.Return(_prefix);
            _prefix = null;
            // The server owns the output stream.
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
