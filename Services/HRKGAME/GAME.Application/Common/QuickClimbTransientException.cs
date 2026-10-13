namespace GAME.Application.Common;

public sealed class QuickClimbTransientException(string message, Exception innerException) : Exception(message, innerException);
