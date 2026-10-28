namespace Core.InternalApi;

public interface IInternalApiMetadataProvider
{
    ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(CancellationToken cancellationToken);
}
