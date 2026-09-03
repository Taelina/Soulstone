using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Soulstone.Datamodels;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    public class ItemTests
    {
        [Fact]
        public void DefaultConstructor_InitializesDefaultValues()
        {
            var item = new Item();

            item.Id.Should().NotBeNullOrWhiteSpace();
            item.Name.Should().BeEmpty();
            item.Description.Should().BeEmpty();
            item.Effect.Should().BeEmpty();
            item.ItemType.Should().Be("Miscellaneous");
            item.Quantity.Should().Be(1);
            item.MaxStack.Should().Be(99);
            item.ImageUrl.Should().BeEmpty();
            item.Weight.Should().Be(0.0f);
            item.Rarity.Should().Be("Common");
            item.CustomProperties.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void ParameterizedConstructor_SetsExpectedProperties()
        {
            var item = new Item(
                "Health Potion",
                "Restores a small amount of HP",
                "Restores 50 HP",
                "Consumable",
                5,
                "https://example.com/potion.png"
            );

            item.Id.Should().NotBeNullOrWhiteSpace();
            item.Name.Should().Be("Health Potion");
            item.Description.Should().Be("Restores a small amount of HP");
            item.Effect.Should().Be("Restores 50 HP");
            item.ItemType.Should().Be("Consumable");
            item.Quantity.Should().Be(5);
            item.ImageUrl.Should().Be("https://example.com/potion.png");
            item.CustomProperties.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void Properties_SetAndGet_UpdatesCorrectly()
        {
            var item = new Item
            {
                Id = "custom-id-123",
                Name = "Excalibur",
                Description = "A legendary holy blade",
                Effect = "+10 Holy Damage, Sheds light",
                ItemType = "Weapon",
                Quantity = 1,
                MaxStack = 1,
                ImageUrl = "C:/images/excalibur.png",
                Weight = 4.5f,
                Rarity = "Legendary",
                CustomProperties = new Dictionary<string, string>
                {
                    { "Element", "Holy" },
                    { "Durability", "100/100" }
                }
            };

            item.Id.Should().Be("custom-id-123");
            item.Name.Should().Be("Excalibur");
            item.Description.Should().Be("A legendary holy blade");
            item.Effect.Should().Be("+10 Holy Damage, Sheds light");
            item.ItemType.Should().Be("Weapon");
            item.Quantity.Should().Be(1);
            item.MaxStack.Should().Be(1);
            item.ImageUrl.Should().Be("C:/images/excalibur.png");
            item.Weight.Should().Be(4.5f);
            item.Rarity.Should().Be("Legendary");
            item.CustomProperties.Should().ContainKey("Element").WhoseValue.Should().Be("Holy");
            item.CustomProperties.Should().ContainKey("Durability").WhoseValue.Should().Be("100/100");
        }

        [Fact]
        public void JsonSerialization_PreservesAllProperties()
        {
            var item = new Item
            {
                Id = "item-uuid-777",
                Name = "Phoenix Down",
                Description = "Revives a fallen ally",
                Effect = "Revives with 10% HP",
                ItemType = "Consumable",
                Quantity = 3,
                MaxStack = 10,
                ImageUrl = "https://images.soulstone/phoenix.png",
                Weight = 0.2f,
                Rarity = "Rare",
                CustomProperties = new Dictionary<string, string>
                {
                    { "UsageLimit", "CombatOnly" }
                }
            };

            var json = item.ToJson();
            var deserialized = Item.FromJson(json);

            deserialized.Should().NotBeNull();
            deserialized!.Id.Should().Be("item-uuid-777");
            deserialized.Name.Should().Be("Phoenix Down");
            deserialized.Description.Should().Be("Revives a fallen ally");
            deserialized.Effect.Should().Be("Revives with 10% HP");
            deserialized.ItemType.Should().Be("Consumable");
            deserialized.Quantity.Should().Be(3);
            deserialized.MaxStack.Should().Be(10);
            deserialized.ImageUrl.Should().Be("https://images.soulstone/phoenix.png");
            deserialized.Weight.Should().Be(0.2f);
            deserialized.Rarity.Should().Be("Rare");
            deserialized.CustomProperties.Should().ContainKey("UsageLimit").WhoseValue.Should().Be("CombatOnly");
        }

        [Fact]
        public void Clone_CreatesIndependentDeepCopy()
        {
            var original = new Item
            {
                Id = "orig-id",
                Name = "Magic Ring",
                Description = "Increases mana",
                Effect = "+50 Max MP",
                ItemType = "Accessory",
                Quantity = 2,
                MaxStack = 5,
                ImageUrl = "ring.png",
                Weight = 0.1f,
                Rarity = "Epic",
                CustomProperties = new Dictionary<string, string>
                {
                    { "Attunement", "Required" }
                }
            };

            var clone = original.Clone();

            clone.Id.Should().NotBe(original.Id); // New ID for cloned instance
            clone.Name.Should().Be(original.Name);
            clone.Description.Should().Be(original.Description);
            clone.Effect.Should().Be(original.Effect);
            clone.ItemType.Should().Be(original.ItemType);
            clone.Quantity.Should().Be(original.Quantity);
            clone.MaxStack.Should().Be(original.MaxStack);
            clone.ImageUrl.Should().Be(original.ImageUrl);
            clone.Weight.Should().Be(original.Weight);
            clone.Rarity.Should().Be(original.Rarity);
            clone.CustomProperties.Should().ContainKey("Attunement").WhoseValue.Should().Be("Required");

            // Verify mutation on clone does not affect original
            clone.CustomProperties["Attunement"] = "None";
            original.CustomProperties["Attunement"].Should().Be("Required");
        }

        [Fact]
        public void FromJson_WithNullOrEmpty_ReturnsNull()
        {
            Item.FromJson("").Should().BeNull();
            Item.FromJson("   ").Should().BeNull();
            Item.FromJson(null!).Should().BeNull();
        }

        #region Usable & Formula Tests

        [Fact]
        public void Item_UsableAndFormula_DefaultsAndClonesCorrectly()
        {
            var item = new Item("Healing Potion", isUsable: true, useFormula: "2d4+2");
            item.IsUsable.Should().BeTrue();
            item.UseFormula.Should().Be("2d4+2");

            var clone = item.Clone();
            clone.IsUsable.Should().BeTrue();
            clone.UseFormula.Should().Be("2d4+2");
        }

        [Theory]
        [InlineData("2d4+2", 4, 10)]
        [InlineData("1d8+3", 4, 11)]
        [InlineData("3d6", 3, 18)]
        [InlineData("10", 10, 10)]
        [InlineData("+5", 5, 5)]
        [InlineData("-2", -2, -2)]
        public void EvaluateUseFormula_WithValidFormulas_ReturnsInRange(string formula, int min, int max)
        {
            var result = Item.EvaluateUseFormula(formula);
            result.Success.Should().BeTrue();
            result.Total.Should().BeInRange(min, max);
            result.Details.Should().NotBeNullOrEmpty();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void EvaluateUseFormula_WithEmpty_ReturnsUnsuccessful(string formula)
        {
            var result = Item.EvaluateUseFormula(formula);
            result.Success.Should().BeFalse();
        }

        [Fact]
        public void Use_WhenNotUsable_ReturnsFalseAndDoesNotDecrement()
        {
            var item = new Item("Rock") { Quantity = 5, IsUsable = false };
            var result = item.Use();

            result.Success.Should().BeFalse();
            item.Quantity.Should().Be(5);
        }

        [Fact]
        public void Use_WhenUsable_DecrementsQuantityAndEvaluatesFormula()
        {
            var item = new Item("Health Potion", effect: "Heals wounds")
            {
                Quantity = 3,
                IsUsable = true,
                UseFormula = "2d4+2"
            };

            var result = item.Use();

            result.Success.Should().BeTrue();
            result.RemainingQuantity.Should().Be(2);
            item.Quantity.Should().Be(2);
            result.FormulaResult.Should().BeInRange(4, 10);
            result.Message.Should().Contain("Health Potion");
        }

        [Fact]
        public void Use_WhenLastQuantity_RemovesFromCharacterSheet()
        {
            var sheet = new CharacterSheet();
            var item = new Item("Elixir") { Quantity = 1, IsUsable = true, UseFormula = "10" };
            sheet.AddItem(item);

            var result = item.Use(sheet);

            result.Success.Should().BeTrue();
            item.Quantity.Should().Be(0);
            sheet.CharacterInventory.Should().NotContain(i => i.Id == item.Id);
        }

        #endregion

        #region Import Tests

        [Fact]
        public void ImportFromJson_WithSingleItem_ReturnsSingleItemList()
        {
            var json = "{\"name\": \"Super Potion\", \"quantity\": 3, \"isUsable\": true, \"useFormula\": \"3d6+5\"}";
            var items = Item.ImportFromJson(json);

            items.Should().HaveCount(1);
            items[0].Name.Should().Be("Super Potion");
            items[0].Quantity.Should().Be(3);
            items[0].IsUsable.Should().BeTrue();
            items[0].UseFormula.Should().Be("3d6+5");
        }

        [Fact]
        public void ImportFromJson_WithItemArray_ReturnsAllItems()
        {
            var json = @"[
                {""name"": ""Item 1"", ""quantity"": 2},
                {""name"": ""Item 2"", ""isUsable"": true, ""useFormula"": ""1d20""}
            ]";

            var items = Item.ImportFromJson(json);

            items.Should().HaveCount(2);
            items[0].Name.Should().Be("Item 1");
            items[1].Name.Should().Be("Item 2");
            items[1].IsUsable.Should().BeTrue();
        }

        [Fact]
        public void ImportFromJson_WithCharacterInventoryWrapper_ReturnsItems()
        {
            var json = @"
            {
                ""characterFullName"": ""Test Hero"",
                ""characterInventory"": [
                    { ""name"": ""Sword"", ""itemType"": ""Weapon"" },
                    { ""name"": ""Shield"", ""itemType"": ""Armor"" }
                ]
            }";

            var items = Item.ImportFromJson(json);

            items.Should().HaveCount(2);
            items[0].Name.Should().Be("Sword");
            items[1].Name.Should().Be("Shield");
        }

        [Fact]
        public void TryImportFromJson_WithValidAndInvalidInputs_ReturnsExpected()
        {
            var valid = Item.TryImportFromJson("{\"name\": \"Potion\"}", out var validItems, out var validErr);
            valid.Should().BeTrue();
            validItems.Should().HaveCount(1);
            validErr.Should().BeEmpty();

            var empty = Item.TryImportFromJson("", out var emptyItems, out var emptyErr);
            empty.Should().BeFalse();
            emptyItems.Should().BeEmpty();

            var invalid = Item.TryImportFromJson("not a json", out var invalidItems, out var invalidErr);
            invalid.Should().BeFalse();
        }

        #endregion
    }
}
