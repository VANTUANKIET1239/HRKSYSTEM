using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Activity.Application;
using Activity.Infrastructure;
using Core.Messaging.Contracts;
using Core.RabbitMQ.DependencyInjection;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Core.TransactionalMessaging.Entities;
using Core.TransactionalMessaging.Inbox;
using Core.TransactionalMessaging.Outbox;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Oservability;
using Oservability.Correlation;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace Oservability.Tests;

public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("HRK_RUN_INTEGRATION") != "1")
            Skip = "Set HRK_RUN_INTEGRATION=1 to run isolated SQL Server/RabbitMQ containers.";
    }
}
