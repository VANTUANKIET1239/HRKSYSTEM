using Core.Application.CQRS;
using Core.Messaging.Contracts;
using Core.RabbitMQ.Interfaces;
using GAME.Application.Features.Commands.Player;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Messaging.Consumers;

public sealed class PlayerInitializationTransientException(Exception inner)
    : Exception("Player initialization storage is temporarily unavailable.", inner), ITransientMessageException;
