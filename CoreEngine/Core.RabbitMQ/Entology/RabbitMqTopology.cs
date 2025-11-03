using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.RabbitMQ.Entology
{
    public sealed class RabbitMqTopology : IRabbitMqTopology
    {
        private readonly RabbitMqOptions _opt;
        private readonly IRabbitMqChannel _channel;

        public RabbitMqTopology(IOptions<RabbitMqOptions> opts, IRabbitMqChannel channel)
        {
            _opt = opts.Value;
            _channel = channel;
        }

        public async Task EnsureAsync(CancellationToken ct = default)
        {
            var ch = await _channel.GetChannelAsync(ct);

            // main exchange + queue
            await ch.ExchangeDeclareAsync(_opt.Publisher.Exchange, _opt.Publisher.ExchangeType,
                durable: _opt.Publisher.Durable, autoDelete: false, cancellationToken: ct);

            await ch.QueueDeclareAsync(_opt.Consumer.Queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);

            await ch.QueueBindAsync(_opt.Consumer.Queue, _opt.Publisher.Exchange,
                routingKey: _opt.Publisher.DefaultRoutingKey, cancellationToken: ct);

            // DLQ
            await ch.ExchangeDeclareAsync(_opt.Consumer.DlqExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
            await ch.QueueDeclareAsync(_opt.Consumer.DlqQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
            await ch.QueueBindAsync(_opt.Consumer.DlqQueue, _opt.Consumer.DlqExchange, routingKey: "dlq", cancellationToken: ct);

            // Retry (TTL → dead-letter back to main)
            await ch.ExchangeDeclareAsync(_opt.Consumer.RetryExchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
            await ch.QueueDeclareAsync(_opt.Consumer.RetryQueue, durable: true, exclusive: false, autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-message-ttl"] = _opt.Consumer.RetryDelayMs,
                    ["x-dead-letter-exchange"] = _opt.Publisher.Exchange,
                    ["x-dead-letter-routing-key"] = _opt.Publisher.DefaultRoutingKey
                }, cancellationToken: ct);
            await ch.QueueBindAsync(_opt.Consumer.RetryQueue, _opt.Consumer.RetryExchange, routingKey: "retry", cancellationToken: ct);
        }
    }
}
