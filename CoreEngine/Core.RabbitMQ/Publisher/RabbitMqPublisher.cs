using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace Core.RabbitMQ.Publisher
{
    public sealed class MessagePublisher : IMessagePublisher
    {
        private readonly IRabbitMqChannel _channel;
        private readonly RabbitMqOptions _opt;
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public MessagePublisher(IOptions<RabbitMqOptions> options, IRabbitMqChannel channel)
        {
            _opt = options.Value;
            _channel = channel;
        }

        public async Task PublishJsonAsync<T>(T message, string? routingKey = null, CancellationToken ct = default)
        {
            var ch = await _channel.GetChannelAsync(ct);
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOpts));

            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await ch.BasicPublishAsync(
                exchange: _opt.Publisher.Exchange,
                routingKey: routingKey ?? _opt.Publisher.DefaultRoutingKey,
                mandatory: true,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            //// publisher confirms (enabled via CreateChannelOptions)
            //await ch.WaitForConfirmsAsync(TimeSpan.FromSeconds(5), ct);
        }
    }
}
