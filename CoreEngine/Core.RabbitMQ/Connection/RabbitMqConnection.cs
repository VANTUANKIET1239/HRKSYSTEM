using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Core.RabbitMQ.Connection
{
    public sealed class RabbitMqConnection : IRabbitMqConnection
    {
        private readonly ConnectionFactory _factory;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private IConnection? _conn;


        public RabbitMqConnection(IOptions<RabbitMqOptions> options)
        {
            var o = options.Value;
            _factory = new ConnectionFactory
            {
                HostName = o.HostName,
                Port = o.Port,
                UserName = o.UserName,
                Password = o.Password,
                VirtualHost = o.VirtualHost
            };
        }

        public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
        {
            if (_conn is { IsOpen: true }) return _conn;
            await _gate.WaitAsync(ct);
            try
            {
                if (_conn is { IsOpen: true }) return _conn;
                _conn = await _factory.CreateConnectionAsync(ct);
                return _conn;
            }
            finally { _gate.Release(); }
        }

        public async ValueTask DisposeAsync()
        {
            if (_conn != null) { await _conn.DisposeAsync(); }
            _gate.Dispose();
        }
    }
}
