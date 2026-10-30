using AUTH.Application.Interfaces;
using AUTH.Infrastructure.Configuration;
using AUTH.Infrastructure.Data;
using AUTH.Infrastructure.Identity;
using AUTH.Infrastructure.Services;
using Core.Persistence.Database.Extensions;
using Core.Persistence.Database.Options;
using Core.AUTH.DependencyInjection;
using Core.Web.DependencyInjection;
using Core.Persistence.DependencyInjection;
using Core.Application.DependencyInjection;
using Core.RabbitMQ.DependencyInjection;
using Core.RabbitMQ.Interfaces;
using Oservability;
using Core.Localization;
using Core.HealthChecks;
using HRK.AUTH.HealthChecks.Dependencies;
using Core.TransactionalMessaging.DependencyInjection;
using Core.Persistence.Repositories;

namespace HRK.AUTH.Configuration
{
    public static partial class DependencyInjection
    {
        public sealed class DemoMessageHandler : IMessageHandler<DemoMessage>
        {
            private readonly ILogger<DemoMessageHandler> _log;
            public DemoMessageHandler(ILogger<DemoMessageHandler> log) => _log = log;
            public Task HandleAsync(DemoMessage message, CancellationToken ct)
            {
                _log.LogInformation("Handled DemoMessage {Id} - {Name} at {At}", message.Id, message.Name, message.CreatedAtUtc);
                // do work...
                return Task.CompletedTask;
            }
        }
    }
}
