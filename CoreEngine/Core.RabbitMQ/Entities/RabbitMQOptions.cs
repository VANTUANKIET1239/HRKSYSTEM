using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.RabbitMQ.Entities
{
    public sealed class RabbitMqOptions
    {
        public string HostName { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";

        public PublisherOptions Publisher { get; set; } = new();
        public ConsumerOptions Consumer { get; set; } = new();

        public sealed class PublisherOptions
        {
            public string Exchange { get; set; } = "app.exchange";
            public string ExchangeType { get; set; } = "direct"; // direct|fanout|topic|headers
            public string DefaultRoutingKey { get; set; } = "app.default";
            public bool Durable { get; set; } = true;
        }

        public sealed class ConsumerOptions
        {
            public string Queue { get; set; } = "app.queue";
            public ushort PrefetchCount { get; set; } = 20;

            // Retry + dead-letter
            public string RetryExchange { get; set; } = "app.retry.exchange";
            public string RetryQueue { get; set; } = "app.retry.queue";
            public int RetryDelayMs { get; set; } = 15000;

            public string DlqExchange { get; set; } = "app.dlq.exchange";
            public string DlqQueue { get; set; } = "app.dlq.queue";
        }
    }
}
