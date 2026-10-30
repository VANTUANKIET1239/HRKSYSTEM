namespace GAME.Application.DTOs;

/// <summary>Identifies a stamina purchase so HTTP retries do not charge the player twice.</summary>
public sealed record PurchaseStaminaRequest(Guid RequestId);
