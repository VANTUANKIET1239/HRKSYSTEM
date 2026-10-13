using System;

namespace Core.Persistence.Repositories;

/// <summary>
/// Commit was attempted but its outcome could not be confirmed. Reconcile using the
/// operation's durable idempotency key before retrying; an execution strategy must not replay it.
/// </summary>
public sealed class TransactionCommitOutcomeUnknownException(Exception innerException)
    : Exception("The transaction commit outcome is unknown. Verify persisted operation state before retrying.", innerException);
