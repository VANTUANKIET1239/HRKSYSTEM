using RabbitMQ.Client;


namespace Core.RabbitMQ.Interfaces
{
    public interface IRabbitMqConnection : IAsyncDisposable
    {
        Task<IConnection> GetConnectionAsync(CancellationToken ct = default);
    }
}
