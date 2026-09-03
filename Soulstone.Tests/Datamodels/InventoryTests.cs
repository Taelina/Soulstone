using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Soulstone.Datamodels;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    [Collection("NonParallel")]
    public class InventoryTests : IDisposable
    {
        private readonly string tempDirectory;

        public InventoryTests()
        {
            TestHelper.EnsureMockServices();
            tempDirectory = Path.Combine(Path.GetTempPath(), "SoulstoneInvTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            Plugin.dataLocation = tempDirectory;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory, true);
                }
            }
            catch { }
        }

        [Fact]
        public void CharacterSheet_DefaultConstructor_InitializesInventoryAndCustomTypes()
        {
            var sheet = new CharacterSheet();

            sheet.CharacterInventory.Should().NotBeNull().And.BeEmpty();
            sheet.CustomItemTypes.Should().NotBeNull().And.BeEmpty();
            sheet.CharacterPictureUrl.Should().BeEmpty();
            sheet.CustomInventoryCapacity.Should().Be(0);
        }

        [Fact]
        public void CharacterSheet_AddItemAndRemoveItem_WorksCorrectly()
        {
            var sheet = new CharacterSheet();
            var item1 = new Item("Sword of Valor", "A sharp steel blade", "+2 ATK", "Weapon");
            var item2 = new Item("Healing Herb", "Restores HP", "+15 HP", "Consumable");

            sheet.AddItem(item1);
            sheet.AddItem(item2);

            sheet.CharacterInventory.Should().HaveCount(2);
            sheet.CharacterInventory.Should().Contain(item1);
            sheet.CharacterInventory.Should().Contain(item2);

            var removed = sheet.RemoveItem(item1.Id);
            removed.Should().BeTrue();
            sheet.CharacterInventory.Should().HaveCount(1);
            sheet.CharacterInventory.Should().NotContain(item1);
            sheet.CharacterInventory.Should().Contain(item2);

            var removeNonExisting = sheet.RemoveItem("non-existing-id");
            removeNonExisting.Should().BeFalse();
        }

        [Fact]
        public void CharacterSheet_GetEffectiveInventoryCapacity_RespectsHierarchy()
        {
            var sheet = new CharacterSheet();
            var diceSystem = new DiceSystem
            {
                SystemHasInventoryLimit = true,
                InventoryMaxSlots = 25
            };

            // 1. When both system limit and custom capacity are default (no custom, unlimited system)
            var unlimitedSystem = new DiceSystem { SystemHasInventoryLimit = false };
            sheet.GetEffectiveInventoryCapacity(unlimitedSystem).Should().Be(0); // 0 = unlimited

            // 2. When system has limit and no custom capacity
            sheet.GetEffectiveInventoryCapacity(diceSystem).Should().Be(25);

            // 3. When custom capacity is explicitly set, it overrides system
            sheet.CustomInventoryCapacity = 50;
            sheet.GetEffectiveInventoryCapacity(diceSystem).Should().Be(50);
            sheet.GetEffectiveInventoryCapacity(unlimitedSystem).Should().Be(50);
            sheet.GetEffectiveInventoryCapacity(null).Should().Be(50);
        }

        [Fact]
        public void CharacterSheet_JsonSerialization_PreservesInventoryAndCustomTypes()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Alphinaud Leveilleur",
                CharacterPictureUrl = "https://images.soulstone/alphinaud.png",
                CustomInventoryCapacity = 40,
                CustomItemTypes = new List<string> { "Grimoire", "Diplomatic Gift" }
            };

            var item = new Item
            {
                Name = "Academic Grimoire",
                Description = "A tactical grimoire filled with equations",
                Effect = "Summons Moonstone Carbuncle",
                ItemType = "Grimoire",
                Quantity = 1,
                ImageUrl = "grimoire.png",
                Rarity = "Rare",
                CustomProperties = new Dictionary<string, string> { { "Binding", "Aetherial" } }
            };
            sheet.AddItem(item);

            var json = JsonSerializer.Serialize(sheet, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<CharacterSheet>(json);

            deserialized.Should().NotBeNull();
            deserialized!.CharacterFullName.Should().Be("Alphinaud Leveilleur");
            deserialized.CharacterPictureUrl.Should().Be("https://images.soulstone/alphinaud.png");
            deserialized.CustomInventoryCapacity.Should().Be(40);
            deserialized.CustomItemTypes.Should().Contain("Grimoire").And.Contain("Diplomatic Gift");
            deserialized.CharacterInventory.Should().HaveCount(1);

            var desItem = deserialized.CharacterInventory[0];
            desItem.Name.Should().Be("Academic Grimoire");
            desItem.Description.Should().Be("A tactical grimoire filled with equations");
            desItem.Effect.Should().Be("Summons Moonstone Carbuncle");
            desItem.ItemType.Should().Be("Grimoire");
            desItem.CustomProperties.Should().ContainKey("Binding").WhoseValue.Should().Be("Aetherial");
        }

        [Fact]
        public void CharacterSheet_SaveAndLoad_PreservesAllInventoryData()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Alisaie Leveilleur",
                CharacterPictureUrl = "C:/portraits/alisaie.png",
                CustomInventoryCapacity = 30
            };

            sheet.CustomItemTypes.Add("Rapier");
            sheet.AddItem(new Item("Estoc", "A finely crafted blade", "+5 Piercing", "Rapier", 1));

            CharacterSheet.SaveSheet(sheet);

            var loaded = CharacterSheet.LoadSheet("Alisaie Leveilleur", isFullPath: false);

            loaded.Should().NotBeNull();
            loaded!.CharacterFullName.Should().Be("Alisaie Leveilleur");
            loaded.CharacterPictureUrl.Should().Be("C:/portraits/alisaie.png");
            loaded.CustomInventoryCapacity.Should().Be(30);
            loaded.CustomItemTypes.Should().Contain("Rapier");
            loaded.CharacterInventory.Should().HaveCount(1);
            loaded.CharacterInventory[0].Name.Should().Be("Estoc");
            loaded.CharacterInventory[0].Effect.Should().Be("+5 Piercing");
        }
    }
}
