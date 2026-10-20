using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Core.Web.Compression;

/// <summary>Buffers only a bounded prefix, then streams into ASP.NET Core's compression body feature.</summary>
internal sealed class ThresholdResponseBodyFeature(IHttpResponseBodyFeature inner, HrkCompressionState state)
    : IHttpResponseBodyFeature, IDisposable
{
    private readonly ThresholdStream _stream = new(inner.Stream, state);
    private PipeWriter? _writer;
    private bool _completed;
    public Stream Stream => _stream;
    public PipeWriter Writer => _writer ??= new ThresholdPipeWriter(_stream);

    public void DisableBuffering()
    {
        _stream.BypassBuffering();
        inner.DisableBuffering();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        // An explicit Start/Flush commits the response: do not wait for more bytes to decide its headers.
        await _stream.CommitAsync(cancellationToken);
        await inner.StartAsync(cancellationToken);
    }

    public async Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
    {
        await _stream.CommitAsync(cancellationToken);
        await inner.SendFileAsync(path, offset, count, cancellationToken);
    }

    public async Task CompleteAsync()
    {
        await FinishAsync();
        await inner.CompleteAsync();
    }

    internal async Task FinishAsync(CancellationToken cancellationToken = default)
    {
        if (_completed) return;
        cancellationToken.ThrowIfCancellationRequested();
        _completed = true;
        if (_writer is not null) await _writer.CompleteAsync();
        await _stream.CommitAsync(cancellationToken);
    }

    internal async Task AbortAsync(Exception error)
    {
        if (_completed) return;
        _completed = true;
        if (_writer is not null) await _writer.CompleteAsync(error);
    }

    public void Dispose() => _stream.Dispose();

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
            try { return await _inner.FlushAsync(cancellationToken); }
            finally { stream.DeferEarlyFlush = false; }
        }
    }

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
            if (_decided) return;
            _decided = true;
            state.ThresholdReached = !state.IdentityAllowed;
        }

        private void Decide(bool reached)
        {
            if (_decided) return;
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
            if (buffer.IsEmpty) return;
            state.BodyBytes += buffer.Length;
            if (Buffer(buffer)) return;
            Drain();
            output.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (buffer.IsEmpty) return;
            state.BodyBytes += buffer.Length;
            if (Buffer(buffer.Span)) return;
            await DrainAsync(cancellationToken);
            await output.WriteAsync(buffer, cancellationToken);
        }

        private void Drain()
        {
            if (_count == 0) return;
            output.Write(_prefix!, 0, _count);
            _count = 0;
        }

        private async Task DrainAsync(CancellationToken cancellationToken)
        {
            if (_count == 0) return;
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
            if (DeferEarlyFlush && !_decided) return;
            await CommitAsync(cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (_prefix is not null) ArrayPool<byte>.Shared.Return(_prefix);
            _prefix = null;
            // The server owns the output stream.
            base.Dispose(disposing);
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
