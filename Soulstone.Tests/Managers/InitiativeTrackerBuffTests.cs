using System;
using System.Collections.Generic;
using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Xunit;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallel")]
    public class InitiativeTrackerBuffTests : IDisposable
    {
        private readonly InitiativeTrackerManager manager;

        public InitiativeTrackerBuffTests()
        {
            TestHelper.EnsureMockServices();
            manager = InitiativeTrackerManager.Instance;
            manager.FullReset();
        }

        public void Dispose()
        {
            manager.FullReset();
        }

        [Fact]
        public void Participant_AddAndRemoveBuff()
        {
            var p = new InitiativeParticipant("Fighter", 18);
            var buff = new Buff("Shield of Faith", 3, "Armor Class", 2);

            p.AddBuff(buff);
            p.Buffs.Should().HaveCount(1);
            p.GetBuffStatBonus("Armor Class").Should().Be(2);

            bool removed = p.RemoveBuff(buff.Id);
            removed.Should().BeTrue();
            p.Buffs.Should().BeEmpty();
            p.GetBuffStatBonus("Armor Class").Should().Be(0);
        }

        [Fact]
        public void Participant_MultipleBuffsAndDebuffs()
        {
            var p = new InitiativeParticipant("Wizard", 12);
            p.AddBuff(new Buff("Bless", 3, "Attack", 1));
            p.AddBuff(new Buff("Bane", 2, "Attack", -1, isDebuff: true));
            p.AddBuff(new Buff("Haste", 3, "Dexterity", 2));

            p.GetBuffStatBonus("Attack").Should().Be(0);
            p.GetBuffStatBonus("Dexterity").Should().Be(2);

            var allBonuses = p.GetAllBuffStatBonuses();
            allBonuses["Attack"].Should().Be(0);
            allBonuses["Dexterity"].Should().Be(2);
        }

        [Fact]
        public void NextTurn_TicksActiveParticipantBuffs()
        {
            var p1 = manager.AddParticipant("P1", 20);
            var p2 = manager.AddParticipant("P2", 15);

            var buff1 = new Buff("Haste", 2, "Speed", 10);
            var buff2 = new Buff("Bless", 3, "Attack", 1);
            p1.AddBuff(buff1);
            p2.AddBuff(buff2);

            // Turn starts with P1
            manager.ActiveParticipant!.Name.Should().Be("P1");

            // Advance turn: P1's buffs tick (duration 2 -> 1)
            manager.NextTurn();
            p1.Buffs[0].Duration.Should().Be(1);
            p2.Buffs[0].Duration.Should().Be(3); // P2 not ticked yet
            manager.ActiveParticipant!.Name.Should().Be("P2");

            // Advance turn: P2's buffs tick (duration 3 -> 2)
            manager.NextTurn();
            p1.Buffs[0].Duration.Should().Be(1);
            p2.Buffs[0].Duration.Should().Be(2);
            manager.ActiveParticipant!.Name.Should().Be("P1");

            // Advance turn: P1's buffs tick again (duration 1 -> 0 -> expires and removed!)
            manager.NextTurn();
            p1.Buffs.Should().BeEmpty();
            manager.ActiveParticipant!.Name.Should().Be("P2");
        }

        [Fact]
        public void Manager_AddAndRemoveBuffFromParticipant()
        {
            var p = manager.AddParticipant("Rogue", 16);
            var buff = new Buff("Invisibility", 2, "Stealth", 5);

            manager.AddBuffToParticipant(p.Id, buff);
            p.Buffs.Should().HaveCount(1);
            p.GetBuffStatBonus("Stealth").Should().Be(5);

            bool removed = manager.RemoveBuffFromParticipant(p.Id, buff.Id);
            removed.Should().BeTrue();
            p.Buffs.Should().BeEmpty();
        }

        [Fact]
        public void Manager_TickParticipantBuffs_Directly()
        {
            var p = manager.AddParticipant("Cleric", 14);
            var buff = new Buff("Sanctuary", 1, "Defense", 3);
            p.AddBuff(buff);

            var expired = manager.TickParticipantBuffs(p.Id, 1);
            expired.Should().HaveCount(1);
            expired[0].Name.Should().Be("Sanctuary");
            p.Buffs.Should().BeEmpty();
        }

        [Fact]
        public void Manager_TickAllBuffs_TicksEveryParticipant()
        {
            var p1 = manager.AddParticipant("P1", 20);
            var p2 = manager.AddParticipant("P2", 10);

            p1.AddBuff(new Buff("Buff1", 1, "Stat", 1));
            p2.AddBuff(new Buff("Buff2", 2, "Stat", 2));

            var expiredMap = manager.TickAllBuffs(1);

            expiredMap.Should().ContainKey(p1.Id);
            expiredMap[p1.Id].Should().HaveCount(1);
            p1.Buffs.Should().BeEmpty();

            p2.Buffs.Should().HaveCount(1);
            p2.Buffs[0].Duration.Should().Be(1);
        }

        [Fact]
        public void CharacterSheet_BuffsAutomaticallySyncWithInitiativeTracker()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Hero Protagonist"
            };
            CharacterManager.Instance.CharacterSheet = sheet;

            // Add participant with matching name
            var participant = manager.AddParticipant("Hero Protagonist", 18, 2, isCurrentChar: true);

            // Add buff on character sheet -> automatically reflected on participant
            var haste = new Buff("Haste", 2, "Agility", 3);
            sheet.AddBuff(haste);

            participant.Buffs.Should().HaveCount(1);
            participant.Buffs[0].Name.Should().Be("Haste");
            participant.Buffs[0].Duration.Should().Be(2);

            // Initiative turn advances -> participant buff ticks and sheet reflects the new duration
            manager.NextTurn(); // hero's turn ticks
            participant.Buffs[0].Duration.Should().Be(1);
            sheet.ActiveBuffs[0].Duration.Should().Be(1);

            // Turn advances again -> buff expires and removes from both
            manager.NextTurn();
            participant.Buffs.Should().BeEmpty();
            sheet.ActiveBuffs.Should().BeEmpty();
        }

        [Fact]
        public void InitiativeTracker_BuffsAutomaticallySyncWithCharacterSheet()
        {
            var sheet = new CharacterSheet
            {
                CharacterFullName = "Paladin"
            };
            CharacterManager.Instance.CharacterSheet = sheet;

            var participant = manager.AddParticipant("Paladin", 14, 1, isCurrentChar: true);

            // Add buff via tracker
            var shield = new Buff("Shield", 3, "Armor Class", 2);
            manager.AddBuffToParticipant(participant.Id, shield);

            sheet.ActiveBuffs.Should().HaveCount(1);
            sheet.ActiveBuffs[0].Name.Should().Be("Shield");
            sheet.GetBuffStatBonus("Armor Class").Should().Be(2);

            // Remove buff via tracker
            manager.RemoveBuffFromParticipant(participant.Id, shield.Id);
            sheet.ActiveBuffs.Should().BeEmpty();
            sheet.GetBuffStatBonus("Armor Class").Should().Be(0);
        }
    }
}
