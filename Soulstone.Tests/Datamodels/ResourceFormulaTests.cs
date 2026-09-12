using System;
using System.Collections.Generic;
using System.Text.Json;
using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Utils;
using Xunit;
using Attribute = Soulstone.Datamodels.Attribute;

namespace Soulstone.Tests.Datamodels
{
    public class ResourceFormulaTests
    {
        [Fact]
        public void StatFormulaEvaluator_BasicMathAndFunctions_EvaluatesCorrectly()
        {
            StatFormulaEvaluator.Evaluate("10 + 20").Should().Be(30);
            StatFormulaEvaluator.Evaluate("50 - 15 * 2").Should().Be(20);
            StatFormulaEvaluator.Evaluate("(10 + 20) * 3").Should().Be(90);
            StatFormulaEvaluator.Evaluate("100 / 4").Should().Be(25);
            StatFormulaEvaluator.Evaluate("10 % 3").Should().Be(1);
            StatFormulaEvaluator.Evaluate("2 ^ 3").Should().Be(8);
            StatFormulaEvaluator.Evaluate("-5 + 10").Should().Be(5);
            StatFormulaEvaluator.Evaluate("-(10 + 5)").Should().Be(-15);

            StatFormulaEvaluator.Evaluate("min(10, 20, 5, 30)").Should().Be(5);
            StatFormulaEvaluator.Evaluate("max(10, 20, 5, 30)").Should().Be(30);
            StatFormulaEvaluator.Evaluate("clamp(150, 0, 100)").Should().Be(100);
            StatFormulaEvaluator.Evaluate("clamp(-10, 0, 100)").Should().Be(0);
            StatFormulaEvaluator.Evaluate("clamp(50, 0, 100)").Should().Be(50);
            StatFormulaEvaluator.Evaluate("floor(14.9)").Should().Be(14);
            StatFormulaEvaluator.Evaluate("ceil(14.1)").Should().Be(15);
            StatFormulaEvaluator.Evaluate("round(14.6)").Should().Be(15);
            StatFormulaEvaluator.Evaluate("abs(-42)").Should().Be(42);
            StatFormulaEvaluator.Evaluate("sqrt(64)").Should().Be(8);
            StatFormulaEvaluator.Evaluate("mod(17, 5)").Should().Be(2);
        }

        [Fact]
        public void StatFormulaEvaluator_WithCharacterSheetAttributes_EvaluatesCorrectly()
        {
            var sheet = new CharacterSheet
            {
                CharacterLevel = 5,
                CharacterAttributes = new Dictionary<string, Attribute>
                {
                    { "Constitution", new Attribute("Constitution", 14) { TempBonus = 2 } }, // Total = 16
                    { "Strength", new Attribute("Strength", 18) },
                    { "Dexterity", new Attribute("Dexterity", 12) }
                }
            };

            // Full name
            StatFormulaEvaluator.Evaluate("10 + 2 * Constitution", sheet).Should().Be(42); // 10 + 2 * 16

            // Alias CON
            StatFormulaEvaluator.Evaluate("10 + 2 * CON", sheet).Should().Be(42);

            // Combination of attributes
            StatFormulaEvaluator.Evaluate("(Strength + Dexterity) / 2", sheet).Should().Be(15); // (18 + 12) / 2

            // Level and attributes
            StatFormulaEvaluator.Evaluate("Level * 10 + Constitution * 2", sheet).Should().Be(82); // 5 * 10 + 16 * 2
        }

        [Fact]
        public void StatFormulaEvaluator_WithSkillsAndAbilities_EvaluatesCorrectly()
        {
            var sheet = new CharacterSheet
            {
                CharacterLevel = 3,
                CharacterAttributes = new Dictionary<string, Attribute>
                {
                    { "Strength", new Attribute("Strength", 16) },
                    { "Intelligence", new Attribute("Intelligence", 14) }
                },
                CharacterSkills = new Dictionary<string, Skill>
                {
                    { "Athletics", new Skill { SkillName = "Athletics", SkillModifier = 4, LinkedAttribute = "Strength" } },
                    { "Arcana", new Skill { SkillName = "Arcana", SkillModifier = 6, LinkedAttribute = "Intelligence" } }
                },
                CharacterAbilities = new Dictionary<string, Ability>
                {
                    { "RageBonus", new Ability { AbilityName = "RageBonus", AbilityModifier = 5 } }
                }
            };

            var diceSystem = new DiceSystem { skillLinkedToOneAttribute = true };

            // Effective skill total = skillModifier (4) + linkedAttr Strength (16) = 20
            StatFormulaEvaluator.Evaluate("Athletics + 10", sheet, diceSystem).Should().Be(30);

            // Combination of attribute, skill, and ability
            // Strength (16) + Arcana (6 + 14 = 20) + RageBonus (5) = 41
            StatFormulaEvaluator.Evaluate("Strength + Arcana + RageBonus", sheet, diceSystem).Should().Be(41);
        }

        [Fact]
        public void StatFormulaEvaluator_WithGearBonusesOnAttributes_IncludesGearInFormula()
        {
            var sheet = new CharacterSheet
            {
                CharacterAttributes = new Dictionary<string, Attribute>
                {
                    { "Constitution", new Attribute("Constitution", 12) }
                }
            };

            var belt = new GearItem("Belt of Health", "Waist");
            belt.SetStatModifier("Constitution", 4);
            sheet.AddItem(belt);
            sheet.EquipGear(belt);

            // Effective Constitution is 12 + 4 = 16
            StatFormulaEvaluator.Evaluate("100 + Constitution * 5", sheet).Should().Be(180); // 100 + 16 * 5
        }

        [Fact]
        public void CharacterSheet_ResourceEffectiveMax_CalculatesFromFormula()
        {
            var sheet = new CharacterSheet
            {
                CharacterLevel = 4,
                CharacterAttributes = new Dictionary<string, Attribute>
                {
                    { "Constitution", new Attribute("Constitution", 14) }
                }
            };

            sheet.CharacterResources["Health"] = new CharacterResource("Health", 100, 100, formula: "50 + Level * 10 + Constitution * 3");

            // 50 + (4 * 10) + (14 * 3) = 50 + 40 + 42 = 132
            int effectiveMax = sheet.GetEffectiveResourceMax("Health");
            effectiveMax.Should().Be(132);

            // Add gear bonus to Health
            var armor = new GearItem("Plate Armor", "Body");
            armor.SetStatModifier("Max Health", 25);
            sheet.AddItem(armor);
            sheet.EquipGear(armor);

            // Effective max should be 132 + 25 = 157
            sheet.GetEffectiveResourceMax("Health").Should().Be(157);
        }

        [Fact]
        public void CharacterSheet_ResourceEffectiveMax_InheritsFormulaFromDiceSystem()
        {
            var diceSystem = new DiceSystem();
            diceSystem.AddResource(new ResourceDefinition("Mana", 100, 100, "#3498db", "Mana pool", formula: "Intelligence * 10 + 20"));

            var sheet = new CharacterSheet
            {
                CharacterAttributes = new Dictionary<string, Attribute>
                {
                    { "Intelligence", new Attribute("Intelligence", 16) }
                }
            };

            // GetEffectiveResources initializes Mana with definition's formula
            var resources = sheet.GetEffectiveResources(diceSystem);
            resources.Should().Contain(r => r.Name == "Mana");

            // 16 * 10 + 20 = 180
            sheet.GetEffectiveResourceMax("Mana", diceSystem).Should().Be(180);
        }

        [Fact]
        public void CharacterSheet_RecalculateResourceMax_UpdatesStoredMaxValue()
        {
            var sheet = new CharacterSheet
            {
                CharacterLevel = 2,
                CharacterAttributes = new Dictionary<string, Attribute>
                {
                    { "Constitution", new Attribute("Constitution", 10) }
                }
            };

            sheet.CharacterResources["Health"] = new CharacterResource("Health", 50, 50, formula: "100 + Constitution * 2");

            sheet.RecalculateResourceMax("Health");
            sheet.CharacterResources["Health"].MaxValue.Should().Be(120); // 100 + 10 * 2
            sheet.CharacterMaxHealthPoints.Should().Be(120);

            // Change Constitution and recalculate all
            sheet.CharacterAttributes["Constitution"].Value = 16;
            sheet.RecalculateAllResourceMaxes();
            sheet.CharacterResources["Health"].MaxValue.Should().Be(132); // 100 + 16 * 2
            sheet.CharacterMaxHealthPoints.Should().Be(132);
        }

        [Fact]
        public void StatFormulaEvaluator_ExtractVariablesAndTryEvaluate_WorkCorrectly()
        {
            var vars = StatFormulaEvaluator.ExtractVariables("max(10, Strength * 2 + Athletics - Level)");
            vars.Should().Contain("Strength");
            vars.Should().Contain("Athletics");
            vars.Should().Contain("Level");
            vars.Should().NotContain("max");

            // TryEvaluate valid
            bool success = StatFormulaEvaluator.TryEvaluate("10 + 5 * 2", null, null, out double res, out string? err);
            success.Should().BeTrue();
            res.Should().Be(20);
            err.Should().BeNull();

            // TryEvaluate invalid syntax
            bool failed = StatFormulaEvaluator.TryEvaluate("10 + * 2", null, null, out _, out string? failErr);
            failed.Should().BeFalse();
            failErr.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void ResourceDefinition_And_CharacterResource_FormulaSerialization()
        {
            var def = new ResourceDefinition("Energy", 100, 100, "#f1c40f", "Energy pool", formula: "Agility * 5 + 50");
            def.Formula.Should().Be("Agility * 5 + 50");

            var jsonDef = JsonSerializer.Serialize(def);
            var deserializedDef = JsonSerializer.Deserialize<ResourceDefinition>(jsonDef);
            deserializedDef.Should().NotBeNull();
            deserializedDef!.Formula.Should().Be("Agility * 5 + 50");

            var res = new CharacterResource("Energy", 80, 100, 10, "Agility * 5 + 50");
            res.Formula.Should().Be("Agility * 5 + 50");

            var jsonRes = JsonSerializer.Serialize(res);
            var deserializedRes = JsonSerializer.Deserialize<CharacterResource>(jsonRes);
            deserializedRes.Should().NotBeNull();
            deserializedRes!.Formula.Should().Be("Agility * 5 + 50");
        }

        [Fact]
        public void StatFormulaEvaluator_NewMathFunctionsAndHelpers_EvaluatesCorrectly()
        {
            StatFormulaEvaluator.Evaluate("pow(2, 4)").Should().Be(16);
            StatFormulaEvaluator.Evaluate("exp(0)").Should().Be(1);
            StatFormulaEvaluator.Evaluate("log10(1000)").Should().Be(3);
            StatFormulaEvaluator.Evaluate("log(e, e)", null, null, new Dictionary<string, double> { { "e", Math.E } }).Should().BeApproximately(1, 0.001);
            StatFormulaEvaluator.Evaluate("sign(-42)").Should().Be(-1);
            StatFormulaEvaluator.Evaluate("sign(42)").Should().Be(1);
            StatFormulaEvaluator.Evaluate("sign(0)").Should().Be(0);
            StatFormulaEvaluator.Evaluate("trunc(15.75)").Should().Be(15);
            StatFormulaEvaluator.Evaluate("dndmod(16)").Should().Be(3);
            StatFormulaEvaluator.Evaluate("statmod(9)").Should().Be(-1);
            StatFormulaEvaluator.Evaluate("if(1, 100, 200)").Should().Be(100);
            StatFormulaEvaluator.Evaluate("if(0, 100, 200)").Should().Be(200);
            StatFormulaEvaluator.Evaluate("cond(5 > 2, 42, 99)", null, null, new Dictionary<string, double> { { "5 > 2", 1 } }).Should().Be(42);
        }

        [Fact]
        public void StatFormulaEvaluator_DotNotationAndUniversalSheetProperties_EvaluatesCorrectly()
        {
            var sheet = new CharacterSheet
            {
                CharacterLevel = 6,
                CharacterExperiencePoints = 12500,
                characterAge = "24",
                characterHeight = "178",
                CharacterAttributes = new Dictionary<string, Attribute>
                {
                    { "Strength", new Attribute("Strength", 16, "Physical power") { TempBonus = 2 } }, // Base 16, Total 18
                    { "Dexterity", new Attribute("Dexterity", 14, "Agility and speed") }
                },
                CharacterSkills = new Dictionary<string, Skill>
                {
                    { "Athletics", new Skill("Athletics", 4, "Strength", "Physical feats") }
                },
                CharacterAbilities = new Dictionary<string, Ability>
                {
                    { "PowerStrike", new Ability("PowerStrike", 5, "Strength", null, "A crushing strike") }
                },
                CharacterResources = new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Health", new CharacterResource("Health", 75, 100) },
                    { "Mana", new CharacterResource("Mana", 30, 50) }
                }
            };

            // Add gear bonus
            var sword = new GearItem("Iron Sword", "Main Hand");
            sword.SetStatModifier("Strength", 2);
            sheet.AddItem(sword);
            sheet.EquipGear(sword);

            // Add buff
            var buff = new Buff("Battle Shout", 3, "Strength", 2);
            sheet.AddBuff(buff);

            // Level & Experience
            StatFormulaEvaluator.Evaluate("Level", sheet).Should().Be(6);
            StatFormulaEvaluator.Evaluate("Lvl", sheet).Should().Be(6);
            StatFormulaEvaluator.Evaluate("XP", sheet).Should().Be(12500);

            // Attribute dot notation
            StatFormulaEvaluator.Evaluate("Strength.Base", sheet).Should().Be(16);
            StatFormulaEvaluator.Evaluate("Strength.Temp", sheet).Should().Be(2);
            StatFormulaEvaluator.Evaluate("Strength.Gear", sheet).Should().Be(2);
            StatFormulaEvaluator.Evaluate("Strength.Buff", sheet).Should().Be(2);
            StatFormulaEvaluator.Evaluate("Strength.Effective", sheet).Should().Be(22); // 16 + 2 + 2 + 2 = 22
            StatFormulaEvaluator.Evaluate("Strength.Mod", sheet).Should().Be(6); // floor((22 - 10) / 2) = 6
            StatFormulaEvaluator.Evaluate("STR_Mod", sheet).Should().Be(6);
            StatFormulaEvaluator.Evaluate("STR.Mod", sheet).Should().Be(6);

            // Resource dot notation
            StatFormulaEvaluator.Evaluate("Health.Current", sheet).Should().Be(75);
            StatFormulaEvaluator.Evaluate("Health.Max", sheet).Should().Be(100);
            StatFormulaEvaluator.Evaluate("CurrentHealth", sheet).Should().Be(75);
            StatFormulaEvaluator.Evaluate("MaxHealth", sheet).Should().Be(100);

            // Gear and Buff direct prefixes
            StatFormulaEvaluator.Evaluate("Gear.Strength", sheet).Should().Be(2);
            StatFormulaEvaluator.Evaluate("Buff.Strength", sheet).Should().Be(2);

            // Inventory and collection counters
            StatFormulaEvaluator.Evaluate("InventoryCount", sheet).Should().Be(1);
            StatFormulaEvaluator.Evaluate("BuffCount", sheet).Should().Be(1);
            StatFormulaEvaluator.Evaluate("GearCount", sheet).Should().Be(1);

            // String numbers reflection fallback
            StatFormulaEvaluator.Evaluate("characterAge", sheet).Should().Be(24);
            StatFormulaEvaluator.Evaluate("characterHeight", sheet).Should().Be(178);

            // Variable syntax with @ and brackets
            StatFormulaEvaluator.Evaluate("@Strength + 10", sheet).Should().Be(32);
            StatFormulaEvaluator.Evaluate("[Strength] + 10", sheet).Should().Be(32);
            StatFormulaEvaluator.Evaluate("{Strength} + 10", sheet).Should().Be(32);
        }

        [Fact]
        public void Attribute_Skill_Ability_DescriptionsAndCloning_PreservesDescriptions()
        {
            var attr = new Attribute("Strength", 18, "Raw brute muscle");
            attr.Description.Should().Be("Raw brute muscle");
            var clonedAttr = attr.Clone();
            clonedAttr.Name.Should().Be("Strength");
            clonedAttr.Value.Should().Be(18);
            clonedAttr.Description.Should().Be("Raw brute muscle");

            var skill = new Skill("Stealth", 5, "Dexterity", "Moving unseen in shadows");
            skill.SkillDescription.Should().Be("Moving unseen in shadows");
            var clonedSkill = skill.Clone();
            clonedSkill.SkillName.Should().Be("Stealth");
            clonedSkill.SkillModifier.Should().Be(5);
            clonedSkill.LinkedAttribute.Should().Be("Dexterity");
            clonedSkill.SkillDescription.Should().Be("Moving unseen in shadows");

            var ability = new Ability("Sneak Attack", 6, "Dexterity", skill, "Deals bonus damage when unseen");
            ability.AbilityDescription.Should().Be("Deals bonus damage when unseen");
            var clonedAbility = ability.Clone();
            clonedAbility.AbilityName.Should().Be("Sneak Attack");
            clonedAbility.AbilityModifier.Should().Be(6);
            clonedAbility.AbilityDescription.Should().Be("Deals bonus damage when unseen");
            clonedAbility.LinkedSkill.Should().NotBeNull();
            clonedAbility.LinkedSkill!.SkillDescription.Should().Be("Moving unseen in shadows");

            // Apply template to CharacterSheet
            var system = new DiceSystem();
            system.SystemAttributes = new Dictionary<string, Attribute>
            {
                { "Wisdom", new Attribute("Wisdom", 15, "Perception and insight") }
            };
            system.SystemSkills = new Dictionary<string, Skill>
            {
                { "Insight", new Skill("Insight", 3, "Wisdom", "Reading motives") }
            };
            system.SystemAbilities = new Dictionary<string, Ability>
            {
                { "SenseMotive", new Ability("SenseMotive", 2, "Wisdom", null, "Sense danger") }
            };

            var sheet = new CharacterSheet();
            sheet.ApplyRulesetTemplate(system);

            sheet.CharacterAttributes["Wisdom"].Description.Should().Be("Perception and insight");
            sheet.CharacterSkills["Insight"].SkillDescription.Should().Be("Reading motives");
            sheet.CharacterAbilities["SenseMotive"].AbilityDescription.Should().Be("Sense danger");
        }
    }
}
