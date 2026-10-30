using Core.Common.Errors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GAME.Domain.ValueObjects
{

    public sealed record CharmMaterialItem(
        long InventoryItemId,
        int ItemTemplateId,
        string Code,
        string Name,
        decimal SuccessRateBonus,
        bool PreventLevelDrop);
}
