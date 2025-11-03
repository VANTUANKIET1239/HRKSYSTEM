using Core.RabbitMQ.Interfaces;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.RabbitMQ.Channel
{
    public sealed class RabbitMqChannel : IRabbitMqChannel
    {
        private readonly IRabbitMqConnection _conn;
        private IChannel? _ch;

        public RabbitMqChannel(IRabbitMqConnection conn) => _conn = conn;

        public async Task<IChannel> GetChannelAsync(CancellationToken ct = default)
        {
            if (_ch is { IsOpen: true }) return _ch;
            var c = await _conn.GetConnectionAsync(ct);
            // Enable publisher confirms + tracking if you publish on this channel
            var opts = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);
            _ch = await c.CreateChannelAsync(opts, ct);
            return _ch;
        }

        public async ValueTask DisposeAsync()
        {
            if (_ch != null) { await _ch.DisposeAsync(); }
        }
    }
}
