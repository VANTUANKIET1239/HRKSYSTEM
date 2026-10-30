using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;

namespace Core.Web.Compression;

/// <summary>Buffers only a bounded prefix, then streams into ASP.NET Core's compression body feature.</summary>
internal sealed partial class ThresholdResponseBodyFeature(IHttpResponseBodyFeature inner, HrkCompressionState state)
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
}
