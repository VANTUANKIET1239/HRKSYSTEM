using GAME.Domain.Entities;

namespace GAME.Application.Features.Shared.PlayerHeroes;

public abstract class PlayerHeroesHandlerSupport
{
    protected PlayerHeroesHandlerSupport()
    {
    }

    protected static long? GetSlotItemId(HrkPlayerEquipment equipment, string slotCode)
    {
        return slotCode switch
        {
            "WEAPON" => equipment.WeaponId,
            "ARMOR" => equipment.ArmorId,
            "HELMET" => equipment.HelmetId,
            "BOOTS" => equipment.BootsId,
            "RING" => equipment.RingId,
            "ARTIFACT" => equipment.ArtifactId,
            _ => null
        };
    }

    protected static void SetSlotItemId(HrkPlayerEquipment equipment, string slotCode, long? itemId)
    {
        switch (slotCode)
        {
            case "WEAPON":
                equipment.WeaponId = itemId;
                break;
            case "ARMOR":
                equipment.ArmorId = itemId;
                break;
            case "HELMET":
                equipment.HelmetId = itemId;
                break;
            case "BOOTS":
                equipment.BootsId = itemId;
                break;
            case "RING":
                equipment.RingId = itemId;
                break;
            case "ARTIFACT":
                equipment.ArtifactId = itemId;
                break;
        }
    }
}
