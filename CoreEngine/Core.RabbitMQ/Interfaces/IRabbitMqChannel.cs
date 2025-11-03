using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.RabbitMQ.Interfaces
{
    public interface IRabbitMqChannel : IAsyncDisposable
    {
        Task<IChannel> GetChannelAsync(CancellationToken ct = default);
    }
}
