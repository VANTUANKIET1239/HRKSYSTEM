using Core.RabbitMQ.Channel;
using Core.RabbitMQ.Consumer;
using Core.RabbitMQ.Connection;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Entology;
using Core.RabbitMQ.Interfaces;
using Core.RabbitMQ.Publisher;
using Hrk.Messaging.RabbitMq.Consuming;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Oservability.Correlation;

namespace Core.RabbitMQ.DependencyInjection
{
    public static partial class DependencyInjection
    {
        private sealed class TopologyHostedService : IHostedService
        {
            private readonly IRabbitMqTopology _topology;
            public TopologyHostedService(IRabbitMqTopology topology) => _topology = topology;
            public Task StartAsync(CancellationToken cancellationToken) => _topology.EnsureAsync(cancellationToken);
            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
