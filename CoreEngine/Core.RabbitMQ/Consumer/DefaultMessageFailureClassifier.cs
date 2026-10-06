using System.Net.Http;
using System.Text.Json;
using Core.RabbitMQ.Entities;
using Core.RabbitMQ.Interfaces;

namespace Core.RabbitMQ.Consumer;

public sealed class DefaultMessageFailureClassifier : IMessageFailureClassifier
{
    public MessageFailureCategory Classify(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => MessageFailureCategory.Cancelled,
            IDuplicateMessageException => MessageFailureCategory.Duplicate,
            IPermanentMessageException => MessageFailureCategory.Permanent,
            ITransientMessageException => MessageFailureCategory.Transient,
            TimeoutException => MessageFailureCategory.Transient,
            HttpRequestException => MessageFailureCategory.Transient,
            JsonException => MessageFailureCategory.Permanent,
            ArgumentException => MessageFailureCategory.Permanent,
            _ => MessageFailureCategory.Unknown
        };
    }
}
