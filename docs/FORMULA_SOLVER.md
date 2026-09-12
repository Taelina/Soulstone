# Soulstone Formula Solver Documentation

The **Soulstone Formula Solver** (`StatFormulaEvaluator`) is a safe, expressive mathematical evaluation engine embedded within Soulstone. It allows players and Dungeon Masters to dynamically compute character resource maximums, initiative values, flat resource statistics, abilities, roll modifiers, and tabletop thresholds directly from character sheets and rulesets.

---

## 📑 Table of Contents
1. [Overview & Capabilities](#1-overview--capabilities)
2. [Grammar & Syntax Rules](#2-grammar--syntax-rules)
3. [Supported Operators & Precedence](#3-supported-operators--precedence)
4. [Dice Notation](#4-dice-notation)
5. [Character Variables & Identifiers](#5-character-variables--identifiers)
   - [Core & Identity Stats](#core--identity-stats)
   - [Attributes & Modifiers](#attributes--modifiers)
   - [Skills & Abilities](#skills--abilities)
   - [Resources (Bars, Counters, Flat Numbers)](#resources-bars-counters-flat-numbers)
   - [Equipment, Cyberware & Buff Bonuses](#equipment-cyberware--buff-bonuses)
   - [Inventory & Collections](#inventory--collections)
   - [Dot-Property Accessors](#dot-property-accessors)
   - [Universal Reflection Fallback](#universal-reflection-fallback)
6. [Mathematical Functions](#6-mathematical-functions)
7. [Practical Examples](#7-practical-examples)
8. [API & Programmatic Usage](#8-api--programmatic-usage)

---

## 1. Overview & Capabilities

The formula solver replaces hardcoded statistics with dynamic, reactive mathematical formulas that automatically update whenever character level, attributes, gear, buffs, or resources change.

### Key Highlights:
- **No External Scripting Engine**: Evaluated via an in-house recursive descent parser without runtime script injection risks.
- **Universal Character Binding**: Capable of resolving **any** character sheet statistic, including Level, Experience, Attributes, Skills, Abilities, Resources, Gear bonuses, Buffs, and physical sheet properties.
- **Tabletop Dice Support**: Seamlessly mixes dice rolls (e.g. `2d6 + 5`) with character stats and math functions.
- **Dot-Notation Properties**: Direct access to sub-properties like `.Mod`, `.Current`, `.Max`, `.Base`, `.Gear`, `.Buff`, and `.Effective`.
- **D&D / d20 Compatibility Helpers**: Built-in functions like `dndmod(score)` and `.Mod` to compute `floor((Score - 10) / 2)`.

---

## 2. Grammar & Syntax Rules

- **Whitespace Ignored**: Spaces and tabs between tokens are ignored (e.g. `10 + 2 * STR` equals `10+2*STR`).
- **Case-Insensitive**: All variable names and functions are case-insensitive (`level`, `LEVEL`, `Level` are identical).
- **Identifier Enclosures**: Multi-word stats or stats with special characters can be referenced directly or enclosed in brackets/quotes:
  - `@Strength` or `Strength` or `[Strength]` or `{Strength}` or `"Strength"`
  - `[Armor Class]` or `Gear.ArmorClass`
- **Unary Operators**: Supports positive and negative signs (`-5`, `+STR`, `-DEX.Mod`).
- **Nested Expressions**: Parentheses `(` `)` can be nested to arbitrary depths.

---

## 3. Supported Operators & Precedence

| Operator | Description | Associativity | Example |
| :--- | :--- | :--- | :--- |
| `()` | Grouping / Parentheses | N/A | `(STR + DEX) * 2` |
| `^` | Exponentiation / Power | Right-to-left | `2 ^ 3` (= 8) |
| `*` | Multiplication | Left-to-right | `Level * 5` |
| `/` | Floating-point Division | Left-to-right | `Constitution / 2` |
| `%` | Modulo (Remainder) | Left-to-right | `Level % 4` |
| `+` | Addition (or Unary Plus) | Left-to-right | `10 + Athletics` |
| `-` | Subtraction (or Unary Negation) | Left-to-right | `MaxHP - 15` |

---

## 4. Dice Notation

Dice expressions can be included directly in formulas. Each die is rolled independently using standard uniform distributions:

- **Standard Notation**: `[count]d[sides]` (e.g. `1d20`, `3d6`, `2d8`, `1d100`).
- **Implicit Count**: `d20` defaults to `1d20`, `d6` defaults to `1d6`.
- **Mixed Arithmetic**: `2d6 + STR.Mod + Level`

---

## 5. Character Variables & Identifiers

### Core & Identity Stats

| Identifier | Description |
| :--- | :--- |
| `Level`, `Lvl`, `CharacterLevel`, `CharLevel` | Current character level |
| `Experience`, `XP`, `Exp`, `CharacterExperiencePoints` | Current character experience points |
| `Initiative`, `Init`, `CombatInitiative` | Total effective initiative modifier |

### Attributes & Modifiers

The solver resolves attributes with all temporary, permanent, gear, and buff bonuses included:

| Identifier | Aliases | Description |
| :--- | :--- | :--- |
| `Strength` | `STR` | Effective Strength value |
| `Dexterity` | `DEX` | Effective Dexterity value |
| `Constitution` | `CON` | Effective Constitution value |
| `Intelligence` | `INT` | Effective Intelligence value |
| `Wisdom` | `WIS` | Effective Wisdom value |
| `Charisma` | `CHA` | Effective Charisma value |
| `Agility` | `AGI` | Effective Agility value |
| `Vitality` | `VIT` | Effective Vitality value |
| `Perception` | `PER` | Effective Perception value |
| `Willpower` | `WIL` | Effective Willpower value |
| `Endurance` | `END` | Effective Endurance value |
| `STR.Mod`, `STR_Mod`, `StrengthMod`, `StrengthModifier` | `dndmod(STR)` | D&D modifier: `floor((STR - 10) / 2)` |

### Skills & Abilities

- **Skills**: Any defined skill name (e.g. `Athletics`, `Stealth`, `Acrobatics`, `Perception`, `Arcana`). Returns the effective skill total (base value + linked attribute modifier + gear + buff).
- **Abilities**: Any defined ability name (e.g. `Fireball`, `SneakAttack`, `SecondWind`). Returns the effective ability modifier.

### Resources (Bars, Counters, Flat Numbers)

- Any resource name (e.g. `Health`, `Mana`, `Stamina`, `Grit`, `Ki`, `SpellSlots`): Returns the effective max value (or current value if max is 0).
- `Max<Resource>` (e.g. `MaxHealth`, `MaxMana`, `MaxStamina`): Explicit effective maximum.
- `Current<Resource>` or `Cur<Resource>` (e.g. `CurrentHealth`, `CurrentMana`, `CurStamina`): Current resource value.

### Equipment, Cyberware & Buff Bonuses

| Identifier | Description |
| :--- | :--- |
| `Gear.<StatName>`, `GearBonus.<StatName>` | Total passive bonus granted to `<StatName>` by equipped gear |
| `Buff.<StatName>`, `BuffBonus.<StatName>` | Total active bonus granted to `<StatName>` by active buffs/debuffs |
| `GearCount`, `EquippedGearCount` | Number of currently equipped gear items |
| `AugmentationsCount`, `CyberwareCount` | Number of installed cyberware / augmentations |
| `BuffCount`, `ActiveBuffsCount` | Number of active status effects on the character |

### Inventory & Collections

| Identifier | Description |
| :--- | :--- |
| `InventoryCapacity`, `InventoryMaxSlots` | Max inventory capacity (or system slot limit) |
| `InventoryCount`, `ItemCount` | Number of unique items currently in inventory |
| `InventoryWeight`, `TotalWeight` | Total weight of all items in inventory (weight × quantity) |

### Dot-Property Accessors

The solver supports deep property access using `<Target>.<Property>` syntax:

- **Attributes**:
  - `Strength.Base`: Raw base attribute value (excluding gear/buffs/temp).
  - `Strength.Temp`: Temporary bonus.
  - `Strength.Perm`: Permanent bonus.
  - `Strength.Epic`: Epic bonus (raw successes).
  - `Strength.Gear`: Gear bonus only.
  - `Strength.Buff`: Buff bonus only.
  - `Strength.Total`: Total base + temp + perm.
  - `Strength.Effective`: Complete effective attribute.
  - `Strength.Mod`: Floored tabletop modifier `floor((Effective - 10) / 2)`.
- **Resources**:
  - `Health.Current`: Current value.
  - `Health.Max`: Base maximum value.
  - `Health.Temp`: Temporary max bonus.
  - `Health.EffectiveMax`: Total calculated effective max.
  - `Health.Fraction`: Current fraction between `0.0` and `1.0`.
  - `Health.Percent`: Current percentage between `0` and `100`.
- **Skills & Abilities**:
  - `Athletics.Base`: Base skill modifier.
  - `Athletics.Gear`: Gear bonus to skill.
  - `Athletics.Buff`: Buff bonus to skill.
  - `Athletics.Total`: Effective skill total.
- **Base Extraction**:
  - `Base.<StatName>`: Directly retrieves the base unmodified score of any attribute, skill, ability, or resource.

### Universal Reflection Fallback

If an identifier is not caught by specific domain handlers, the solver reflects across public fields and properties of `CharacterSheet`:
- Numeric fields (`int`, `double`, `float`, `long`, `short`, `byte`): Evaluated directly.
- Boolean fields (`bool`): Evaluated as `1` (true) or `0` (false).
- Collection fields (`List`, `Dictionary`): Evaluated as their `.Count`.
- String fields containing numbers (e.g. `characterAge = "25"`, `characterHeight = "175"`): Parsed and returned as numeric values.

---

## 6. Mathematical Functions

| Function | Signature | Description | Example |
| :--- | :--- | :--- | :--- |
| `min` | `min(a, b, ...)` | Returns the smallest value among arguments | `min(Level, 10)` |
| `max` | `max(a, b, ...)` | Returns the largest value among arguments | `max(STR.Mod, 1)` |
| `clamp` | `clamp(val, min, max)` | Constrains `val` between `min` and `max` | `clamp(DEX.Mod, 0, 2)` |
| `floor` | `floor(x)` | Rounds down to the nearest integer | `floor(Level / 2)` |
| `ceil`, `ceiling` | `ceil(x)` | Rounds up to the nearest integer | `ceil(Level / 3)` |
| `round` | `round(x, [digits])` | Rounds to the nearest integer or decimal places | `round(Constitution * 1.5)` |
| `trunc`, `truncate` | `trunc(x)` | Truncates decimal digits toward zero | `trunc(XP / 1000)` |
| `abs` | `abs(x)` | Returns the absolute value of `x` | `abs(Buff.Strength)` |
| `sqrt` | `sqrt(x)` | Returns the square root of `x` | `sqrt(Level) * 5` |
| `mod` | `mod(a, b)` | Returns `a` modulo `b` | `mod(Level, 5)` |
| `pow` | `pow(base, exp)` | Returns `base` raised to the power of `exp` | `pow(Level, 2)` |
| `exp` | `exp(x)` | Returns `e` raised to the power of `x` | `exp(Level * 0.1)` |
| `log`, `ln` | `log(x, [base])` | Natural log, or log with specified base | `log(XP)` |
| `log10` | `log10(x)` | Base-10 logarithm | `log10(XP)` |
| `sign` | `sign(x)` | Returns `-1`, `0`, or `1` based on the sign of `x` | `sign(STR.Mod)` |
| `dndmod`, `statmod` | `dndmod(score)` | Calculates tabletop modifier: `floor((score - 10) / 2)` | `dndmod(Strength)` |
| `if`, `cond`, `choose` | `if(cond, trueVal, falseVal)` | Returns `trueVal` if `cond != 0`, otherwise `falseVal` | `if(Level > 5, 20, 10)` |

---

## 7. Practical Examples

### 1. D&D 5e Health Points (Barbarian)
```text
12 + CON.Mod + ((Level - 1) * (7 + CON.Mod))
```
- Level 1 with 16 CON (+3): `12 + 3 + (0 * 10) = 15 HP`
- Level 5 with 16 CON (+3): `12 + 3 + (4 * 10) = 55 HP`

### 2. Mana Pool based on Intelligence & Level
```text
50 + (INT * 5) + (Level * 10)
```

### 3. Armor Class (AC) with Dexterity Cap (Medium Armor)
```text
14 + clamp(DEX.Mod, 0, 2) + Gear.Shield
```

### 4. Flat Resource: Spell Save DC
```text
8 + floor((Level + 7) / 4) + INT.Mod
```

### 5. Counter Resource: Ki Points / Sorcery Points
```text
max(1, Level)
```

### 6. Counter Resource: Spell Slots (Level 1)
```text
if(Level >= 3, 4, if(Level == 2, 3, 2))
```

### 7. Initiative Modifier with Half-Proficiency
```text
DEX.Mod + floor(Level / 4)
```

### 8. Dynamic Ability Damage
```text
2d8 + STR.Mod + Gear.WeaponBonus
```

---

## 8. API & Programmatic Usage

For developer integration within the C# plugin code:

```csharp
using Soulstone.Utils;

// 1. Basic Evaluation
double result = StatFormulaEvaluator.Evaluate("10 + 2 * CON.Mod", characterSheet, diceSystem);

// 2. Evaluate directly to integer
int maxHp = StatFormulaEvaluator.EvaluateToInt("10 + Level * 5", characterSheet, diceSystem, defaultValue: 100);

// 3. Safe TryEvaluate with error messages
if (StatFormulaEvaluator.TryEvaluate(formula, sheet, diceSystem, out double val, out string? error))
{
    // Success
}
else
{
    PluginLog.Warning($"Formula error: {error}");
}

// 4. Extract variable dependencies
List<string> vars = StatFormulaEvaluator.ExtractVariables("10 + STR.Mod + Athletics + Gear.Bonus");
// Returns: ["STR.Mod", "Athletics", "Gear.Bonus"]
```
