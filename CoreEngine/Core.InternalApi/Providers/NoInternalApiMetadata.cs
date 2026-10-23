namespace Core.InternalApi;

internal sealed class NoInternalApiMetadata : IInternalApiMetadataProvider
{
    public ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(CancellationToken cancellationToken) =>
        new(new Dictionary<string, string>());
}
