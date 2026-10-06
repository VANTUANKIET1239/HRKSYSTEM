using Core.Common.Constants;
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
    public static class DependencyInjection
    {
        public static IServiceCollection AddRabbitMqMessaging(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.TryAddScoped<CorrelationContext>();
            services.TryAddScoped<ICorrelationContext>(sp => sp.GetRequiredService<CorrelationContext>());
            services.AddScoped<MessageContext>();
            services.Configure<RabbitMqOptions>(
                configuration.GetSection(Common.Constants.Common.Constants.RABBITMQ.RABBITMQ_OPTIONS));
            services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
            services.AddSingleton<IRabbitMqChannel, RabbitMqChannel>();
            services.AddSingleton<IRabbitMqTopology, RabbitMqTopology>();
            services.AddSingleton<IMessagePublisher, MessagePublisher>();
            services.AddSingleton<IMessageFailureClassifier, DefaultMessageFailureClassifier>();
            return services;
        }

        /// Ensures exchanges/queues at startup.
        public static IServiceCollection EnsureRabbitTopology(this IServiceCollection services)
        {
            services.AddHostedService<TopologyHostedService>();
            return services;
        }

        public static IServiceCollection AddRabbitConsumer<TMessage, THandler>(
            this IServiceCollection services,
            string queue)
            where THandler : class, IMessageHandler<TMessage>
        {
            return AddConsumer<TMessage, THandler>(services, queue);
        }

        public static IServiceCollection AddNamedRabbitConsumer<TMessage, THandler>(
            this IServiceCollection services,
            string consumerName)
            where THandler : class, IMessageHandler<TMessage>
        {
            return AddConsumer<TMessage, THandler>(services, consumerName);
        }

        private static IServiceCollection AddConsumer<TMessage, THandler>(
            IServiceCollection services,
            string consumerName)
            where THandler : class, IMessageHandler<TMessage>
        {
            services.AddScoped<THandler>();
            services.AddHostedService(sp =>
                new ConsumerBackgroundService<TMessage, THandler>(
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<
                        ConsumerBackgroundService<TMessage, THandler>>>(),
                    sp.GetRequiredService<IRabbitMqChannel>(),
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>(),
                    sp,
                    sp.GetRequiredService<IMessageFailureClassifier>(),
                    consumerName));
            return services;
        }

        private sealed class TopologyHostedService : IHostedService
        {
            private readonly IRabbitMqTopology _topology;

            public TopologyHostedService(IRabbitMqTopology topology) => _topology = topology;

            public Task StartAsync(CancellationToken cancellationToken) => _topology.EnsureAsync(cancellationToken);

            public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }
    }
}
