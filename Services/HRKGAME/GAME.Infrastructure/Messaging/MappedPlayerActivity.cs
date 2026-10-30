using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Core.Messaging.Contracts;
using GAME.Application.Events;

namespace GAME.Infrastructure.Messaging;

public sealed record MappedPlayerActivity(PlayerActivityRecordedV1 Message, string? PartitionKey, long? Sequence);
