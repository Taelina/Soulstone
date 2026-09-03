using System;
using System.IO;
using System.Text.Json;
using FluentAssertions;
using Soulstone.Datamodels;
using Xunit;

namespace Soulstone.Tests.Datamodels
{
    [Collection("NonParallel")]
    public class DiceSystemInitiativeTests : IDisposable
    {
        private readonly string tempDirectory;

        public DiceSystemInitiativeTests()
        {
            TestHelper.EnsureMockServices();
            tempDirectory = Path.Combine(Path.GetTempPath(), "SoulstoneTests_" + Guid.NewGuid().ToString("N"));
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
        public void DefaultConstructor_InitiativeDefaultsToNone()
        {
            var system = new DiceSystem();

            system.InitiativeStatType.Should().Be(InitiativeStatType.None);
            system.initiativeStatType.Should().Be(InitiativeStatType.None);
            system.InitiativeStatName.Should().BeEmpty();
            system.initiativeStatName.Should().BeEmpty();
        }

        [Fact]
        public void InitiativeProperties_SetAndGet_WorksCorrectly()
        {
            var system = new DiceSystem
            {
                InitiativeStatType = InitiativeStatType.Attribute,
                InitiativeStatName = "Dexterity"
            };

            system.InitiativeStatType.Should().Be(InitiativeStatType.Attribute);
            system.initiativeStatType.Should().Be(InitiativeStatType.Attribute);
            system.InitiativeStatName.Should().Be("Dexterity");
            system.initiativeStatName.Should().Be("Dexterity");
        }

        [Fact]
        public void JsonSerialization_PreservesInitiativeConfiguration()
        {
            var original = new DiceSystem
            {
                SystemName = "InitiativeSystem",
                InitiativeStatType = InitiativeStatType.Skill,
                InitiativeStatName = "Acrobatics"
            };

            DiceSystem.SaveDiceSystem(original);
            var loaded = DiceSystem.LoadDiceSystem("initiativesystem");

            loaded.Should().NotBeNull();
            loaded!.InitiativeStatType.Should().Be(InitiativeStatType.Skill);
            loaded.InitiativeStatName.Should().Be("Acrobatics");
        }
    }
}
