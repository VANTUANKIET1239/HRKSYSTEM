using Activity.Application;
using Core.Messaging.Contracts;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;
using Microsoft.Data.SqlClient;

namespace Activity.Infrastructure;
public sealed partial class ActivityMessageHandler
{
    private sealed class ActivityPersistenceException(Exception inner) : Exception("Activity persistence is temporarily unavailable.", inner), ITransientMessageException;
}
