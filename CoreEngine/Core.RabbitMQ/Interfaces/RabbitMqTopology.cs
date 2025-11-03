using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.RabbitMQ.Interfaces
{
    public interface IRabbitMqTopology
    {
        Task EnsureAsync(CancellationToken ct = default);
    }
}
