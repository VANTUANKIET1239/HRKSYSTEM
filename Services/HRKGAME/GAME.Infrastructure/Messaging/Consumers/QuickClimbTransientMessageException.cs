using Core.Messaging.Contracts;
using Core.RabbitMQ.Interfaces;
using GAME.Application.Common;
using GAME.Application.Features.Commands.QuickClimb;
using MediatR;

namespace GAME.Infrastructure.Messaging.Consumers;

public sealed class QuickClimbTransientMessageException(string message, Exception innerException) : Exception(message, innerException), ITransientMessageException;
