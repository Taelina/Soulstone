using System;
using System.Collections.Generic;
using System.Text.Json;
using Soulstone.Datamodels;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    public class BuffTests
    {
        [Fact]
        public void DefaultConstructor_InitializesDefaults()
        {
            var buff = new Buff();

            Assert.False(string.IsNullOrEmpty(buff.Id));
            Assert.Equal(string.Empty, buff.Name);
            Assert.Equal(string.Empty, buff.Description);
            Assert.Equal(1, buff.Duration);
            Assert.Equal(1, buff.InitialDuration);
            Assert.False(buff.IsDebuff);
            Assert.NotNull(buff.StatModifiers);
            Assert.Empty(buff.StatModifiers);
        }

        [Fact]
        public void SingleStatConstructor_InitializesCorrectly_Buff()
        {
            var buff = new Buff("Haste", 3, "Agility", 2, "Speed boost", false);

            Assert.Equal("Haste", buff.Name);
            Assert.Equal(3, buff.Duration);
            Assert.Equal(3, buff.InitialDuration);
            Assert.Equal("Speed boost", buff.Description);
            Assert.False(buff.IsDebuff);
            Assert.Equal(2, buff.GetStatModifier("Agility"));
            Assert.Equal(2, buff.GetStatModifier("agility")); // Case insensitive
        }

        [Fact]
        public void SingleStatConstructor_InitializesCorrectly_Debuff()
        {
            var buff = new Buff("Poison", 4, "Health", -5, "Poison damage per turn", true);

            Assert.Equal("Poison", buff.Name);
            Assert.Equal(4, buff.Duration);
            Assert.Equal(4, buff.InitialDuration);
            Assert.Equal("Poison damage per turn", buff.Description);
            Assert.True(buff.IsDebuff);
            Assert.Equal(-5, buff.GetStatModifier("Health"));
        }

        [Fact]
        public void DictionaryConstructor_InitializesCorrectly()
        {
            var dict = new Dictionary<string, int>
            {
                { "Strength", 3 },
                { "Athletics", 2 }
            };

            var buff = new Buff("Giant Might", 2, dict, "Strength of a giant");

            Assert.Equal("Giant Might", buff.Name);
            Assert.Equal(2, buff.Duration);
            Assert.Equal(3, buff.GetStatModifier("Strength"));
            Assert.Equal(2, buff.GetStatModifier("Athletics"));
            Assert.Equal(0, buff.GetStatModifier("Intelligence"));
        }

        [Fact]
        public void StatModifier_SetAndRemove()
        {
            var buff = new Buff("Custom", 1);
            buff.SetStatModifier("Attack", 4);

            Assert.Equal(4, buff.GetStatModifier("Attack"));

            bool removed = buff.RemoveStatModifier("Attack");
            Assert.True(removed);
            Assert.Equal(0, buff.GetStatModifier("Attack"));

            bool removedAgain = buff.RemoveStatModifier("NonExistent");
            Assert.False(removedAgain);
        }

        [Fact]
        public void GetFormattedModifiers_FormatsCorrectly()
        {
            var buff = new Buff("Bless", 3);
            buff.SetStatModifier("Attack", 2);
            buff.SetStatModifier("Defense", -1);

            string formatted = buff.GetFormattedModifiers();
            Assert.Contains("+2 Attack", formatted);
            Assert.Contains("-1 Defense", formatted);
        }

        [Fact]
        public void Tick_ReducesDuration_AndExpiresWhenZero()
        {
            var buff = new Buff("Shield", 2);

            bool expired1 = buff.Tick(1);
            Assert.False(expired1);
            Assert.Equal(1, buff.Duration);

            bool expired2 = buff.Tick(1);
            Assert.True(expired2);
            Assert.Equal(0, buff.Duration);
        }

        [Fact]
        public void Clone_CreatesIndependentCopy()
        {
            var original = new Buff("Haste", 3, "Speed", 5, "Fast", false);
            var clone = original.Clone();

            Assert.NotEqual(original.Id, clone.Id);
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.Duration, clone.Duration);
            Assert.Equal(5, clone.GetStatModifier("Speed"));

            clone.Duration = 1;
            clone.SetStatModifier("Speed", 10);

            Assert.Equal(3, original.Duration);
            Assert.Equal(5, original.GetStatModifier("Speed"));
        }

        [Fact]
        public void JsonSerialization_PreservesAllFields()
        {
            var buff = new Buff("Weakness", 3, "Strength", -4, "Curse effect", true);

            string json = JsonSerializer.Serialize(buff);
            var deserialized = JsonSerializer.Deserialize<Buff>(json);

            Assert.NotNull(deserialized);
            Assert.Equal(buff.Id, deserialized.Id);
            Assert.Equal(buff.Name, deserialized.Name);
            Assert.Equal(buff.Description, deserialized.Description);
            Assert.Equal(buff.Duration, deserialized.Duration);
            Assert.Equal(buff.InitialDuration, deserialized.InitialDuration);
            Assert.Equal(buff.IsDebuff, deserialized.IsDebuff);
            Assert.Equal(-4, deserialized.GetStatModifier("Strength"));
        }
    }
}
