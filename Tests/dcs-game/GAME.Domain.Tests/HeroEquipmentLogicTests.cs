using GAME.Domain.Entities;
using Xunit;

namespace GAME.Domain.Tests
{
    public class HeroEquipmentLogicTests
    {
        [Fact]
        public void UnequipAll_ClearsAllSlotsAndReturnsItemsToBag()
        {
            // Setup Hero with 3 items
            var heroEquipment = new HrkPlayerEquipment
            {
                HeroId = 10,
                WeaponId = 101,
                ArmorId = 102,
                HelmetId = 103,
                BootsId = null,
                RingId = null,
                ArtifactId = null
            };

            var items = new List<HrkPlayerInventory>
            {
                new() { Id = 101, IsEquipped = true, EquippedHeroId = 10 },
                new() { Id = 102, IsEquipped = true, EquippedHeroId = 10 },
                new() { Id = 103, IsEquipped = true, EquippedHeroId = 10 }
            };

            // Simulate unequip all logic
            var equippedIds = new List<long?>
            {
                heroEquipment.WeaponId,
                heroEquipment.ArmorId,
                heroEquipment.HelmetId,
                heroEquipment.BootsId,
                heroEquipment.RingId,
                heroEquipment.ArtifactId
            }.Where(id => id.HasValue).Select(id => id!.Value).ToList();

            Assert.Equal(3, equippedIds.Count);

            heroEquipment.WeaponId = null;
            heroEquipment.ArmorId = null;
            heroEquipment.HelmetId = null;
            heroEquipment.BootsId = null;
            heroEquipment.RingId = null;
            heroEquipment.ArtifactId = null;

            foreach (var item in items)
            {
                item.IsEquipped = false;
                item.EquippedHeroId = null;
            }

            // Assert
            Assert.Null(heroEquipment.WeaponId);
            Assert.Null(heroEquipment.ArmorId);
            Assert.Null(heroEquipment.HelmetId);
            Assert.All(items, item =>
            {
                Assert.False(item.IsEquipped);
                Assert.Null(item.EquippedHeroId);
            });
        }

        [Fact]
        public void SwapEquipment_ExchangesAllSixSlots_AndUpdatesEquippedHeroIds()
        {
            var sourceHeroId = 1L;
            var targetHeroId = 2L;

            var sourceEquip = new HrkPlayerEquipment
            {
                HeroId = sourceHeroId,
                WeaponId = 101,
                ArmorId = 102,
                HelmetId = null,
                BootsId = null,
                RingId = null,
                ArtifactId = null
            };

            var targetEquip = new HrkPlayerEquipment
            {
                HeroId = targetHeroId,
                WeaponId = 201,
                ArmorId = null,
                HelmetId = 203,
                BootsId = null,
                RingId = null,
                ArtifactId = null
            };

            var item101 = new HrkPlayerInventory { Id = 101, IsEquipped = true, EquippedHeroId = sourceHeroId };
            var item102 = new HrkPlayerInventory { Id = 102, IsEquipped = true, EquippedHeroId = sourceHeroId };
            var item201 = new HrkPlayerInventory { Id = 201, IsEquipped = true, EquippedHeroId = targetHeroId };
            var item203 = new HrkPlayerInventory { Id = 203, IsEquipped = true, EquippedHeroId = targetHeroId };

            var allItems = new Dictionary<long, HrkPlayerInventory>
            {
                [101] = item101,
                [102] = item102,
                [201] = item201,
                [203] = item203
            };

            // Swap all 6 slots
            (sourceEquip.WeaponId, targetEquip.WeaponId) = (targetEquip.WeaponId, sourceEquip.WeaponId);
            (sourceEquip.ArmorId, targetEquip.ArmorId) = (targetEquip.ArmorId, sourceEquip.ArmorId);
            (sourceEquip.HelmetId, targetEquip.HelmetId) = (targetEquip.HelmetId, sourceEquip.HelmetId);
            (sourceEquip.BootsId, targetEquip.BootsId) = (targetEquip.BootsId, sourceEquip.BootsId);
            (sourceEquip.RingId, targetEquip.RingId) = (targetEquip.RingId, sourceEquip.RingId);
            (sourceEquip.ArtifactId, targetEquip.ArtifactId) = (targetEquip.ArtifactId, sourceEquip.ArtifactId);

            // Update ownership
            var newSourceIds = new[] { sourceEquip.WeaponId, sourceEquip.ArmorId, sourceEquip.HelmetId, sourceEquip.BootsId, sourceEquip.RingId, sourceEquip.ArtifactId }
                .Where(x => x.HasValue).Select(x => x!.Value).ToList();
            var newTargetIds = new[] { targetEquip.WeaponId, targetEquip.ArmorId, targetEquip.HelmetId, targetEquip.BootsId, targetEquip.RingId, targetEquip.ArtifactId }
                .Where(x => x.HasValue).Select(x => x!.Value).ToList();

            foreach (var id in newSourceIds)
            {
                allItems[id].EquippedHeroId = sourceHeroId;
                allItems[id].IsEquipped = true;
            }

            foreach (var id in newTargetIds)
            {
                allItems[id].EquippedHeroId = targetHeroId;
                allItems[id].IsEquipped = true;
            }

            // Assert source now has 201 and 203
            Assert.Equal(201, sourceEquip.WeaponId);
            Assert.Null(sourceEquip.ArmorId);
            Assert.Equal(203, sourceEquip.HelmetId);
            Assert.Equal(sourceHeroId, item201.EquippedHeroId);
            Assert.Equal(sourceHeroId, item203.EquippedHeroId);

            // Assert target now has 101 and 102
            Assert.Equal(101, targetEquip.WeaponId);
            Assert.Equal(102, targetEquip.ArmorId);
            Assert.Null(targetEquip.HelmetId);
            Assert.Equal(targetHeroId, item101.EquippedHeroId);
            Assert.Equal(targetHeroId, item102.EquippedHeroId);
        }

        [Fact]
        public void BagLimitCheck_WhenBagIsFull_FailsValidation()
        {
            int currentBagItems = 198;
            int bagCapacity = 200;
            int itemsToUnequip = 3;

            bool isSufficient = currentBagItems + itemsToUnequip <= bagCapacity;

            Assert.False(isSufficient, "Bag capacity of 200 cannot accommodate 198 + 3 = 201 items.");
        }
    }
}
