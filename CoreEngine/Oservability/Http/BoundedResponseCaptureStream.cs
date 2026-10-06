namespace Oservability.Http;

// Copies at most limit + 1 bytes for diagnostics; all writes go straight to the client.
// Never owns/disposes the original response stream.
internal sealed class BoundedResponseCaptureStream(Stream inner, int limit) : Stream
{
    private readonly MemoryStream _capture = new();
    public byte[] Captured => _capture.ToArray();
    public bool Oversized => _capture.Length > limit;
    private void Capture(ReadOnlySpan<byte> data)
    {
        var remaining = limit + 1 - (int)_capture.Length;
        if (remaining > 0) _capture.Write(data[..Math.Min(remaining, data.Length)]);
    }
    public override void Write(byte[] buffer, int offset, int count)
    {
        inner.Write(buffer, offset, count);
        Capture(buffer.AsSpan(offset, count));
    }
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        await inner.WriteAsync(buffer, ct);
        Capture(buffer.Span);
    }
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
        WriteAsync(buffer.AsMemory(offset, count), ct).AsTask();
    public override void Flush() => inner.Flush();
    public override Task FlushAsync(CancellationToken ct) => inner.FlushAsync(ct);
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => inner.CanWrite;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    protected override void Dispose(bool disposing)
    {
        if (disposing) _capture.Dispose();
        base.Dispose(disposing);
    }
}
