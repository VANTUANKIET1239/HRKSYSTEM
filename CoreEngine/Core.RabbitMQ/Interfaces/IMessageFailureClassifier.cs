using Core.RabbitMQ.Entities;

namespace Core.RabbitMQ.Interfaces;

public interface IMessageFailureClassifier
{
    MessageFailureCategory Classify(Exception exception);
}

public interface ITransientMessageException;

public interface IPermanentMessageException;

public interface IDuplicateMessageException;
