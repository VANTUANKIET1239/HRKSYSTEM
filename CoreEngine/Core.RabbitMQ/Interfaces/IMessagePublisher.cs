using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.RabbitMQ.Interfaces
{
    public interface IMessagePublisher
    {
        public Task PublishJsonAsync<T>(T message, string? routingKey = null, CancellationToken ct = default);
    }
}
