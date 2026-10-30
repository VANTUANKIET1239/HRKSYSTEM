using Core.Common.Errors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GAME.Domain.ValueObjects
{
    public sealed record StoneMaterialItem(
        long InventoryItemId,
        int ItemTemplateId,
        string Code,
        string Name,
        int Quantity,
        decimal SuccessRateBonus);
}
