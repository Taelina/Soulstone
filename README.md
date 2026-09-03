<p align="center">
  <img src="https://www.aht.li/3945121/Soulstone.png" alt="Soulstone Logo" width="200" height="200">
</p>

<h1 align="center">Soulstone</h1>

<p align="center">
  <b>An all-in-one Roleplay, Character Sheet & Tabletop companion plugin for Final Fantasy XIV (Dalamud)</b>
</p>

<p align="center">
  <a href="https://discord.gg/6hkvbXbPRF"><img src="https://img.shields.io/badge/Discord-Join%20Community-5865F2?logo=discord&logoColor=white" alt="Discord"></a>
  <a href="https://github.com/Taelina/Soulstone"><img src="https://img.shields.io/badge/GitHub-Repository-181717?logo=github&logoColor=white" alt="GitHub"></a>
  <img src="https://img.shields.io/badge/License-MIT-blue.svg" alt="License">
  <img src="https://img.shields.io/badge/.NET-10.0--windows-512BD4?logo=dotnet&logoColor=white" alt=".NET 10.0">
</p>

---

**Soulstone** is a Dalamud plugin for **Final Fantasy XIV** designed to centralize rich roleplay profiles, tabletop character sheets, dynamic stat and resource systems, inventory and gear management, cyberware augmentations, initiative tracking, and dice rolling into one intuitive, modern in-game interface.

Whether you run casual tavern RP, elaborate tabletop campaigns in Eorzea, or participate in complex TTRPG systems (D&D 5e, Pathfinder, Shadowrun, Call of Cthulhu, Cyberpunk, custom homebrews), Soulstone provides everything you need to manage your character without leaving the game.

---

## ✨ Features

### 🎭 Character RP Sheets & Profiles
- **Identity & Demographics**: Full name, nickname, race/species, clan/sub-race, job/class, sex, gender, pronouns, and age.
- **OOC / HRP Info**: Player timezone, availability, roleplay preferences, and out-of-character notes.
- **Detailed Appearance**: Height, weight, build, eye color, hair color, skin tone, scars, tattoos, and distinctive quirks.
- **Quick Look Hooks**: 5 customizable quick glance descriptors for immediate roleplay hooks visible to fellow players.
- **Background & Lore**: Birthplace, origin, affiliation, occupation, social reputation (with custom tooltips), and full background biography.
- **Social Network & Relationships**: Categorized relationship lists (**Family**, **Friends**, **Enemies**, **Allies**) with customizable descriptions and statuses.

### 📊 Dynamic Stats, Attributes & Abilities
- **Custom Attributes**: Define core attributes (e.g. Strength, Dexterity, Intelligence) with base values and bonuses.
- **Hierarchical Skills & Abilities**:
  - Create **Skills** linked directly to primary attributes.
  - Create **Abilities** linked to specific skills and attributes with custom dice formulas and modifier calculations.
- **Stat Formula Evaluator**: Evaluate mathematical stat expressions dynamically referencing attributes (e.g. `@STR * 2 + @DEX / 2`).
- **Interactive Stat Rolls**: Single-click rolling for any attribute, skill, or ability with automatic modifiers and chat output.

### 🧪 Custom Dynamic Resource Pools
- **Configurable Resource Definitions**: Create arbitrary resource pools (HP, MP, Stamina, Resolve, Sanity, Ki, Spell Slots, Ammo, etc.).
- **Dynamic Max Value Formulas**: Resources can use static caps or dynamic formulas evaluated against character attributes (e.g. `@CON * 5 + 10`).
- **Real-time Value Tracking**: Current and maximum tracking with visual progress bars, quick increment/decrement buttons, and visual health indicators.
- **Dice System Resource Templates**: Custom dice systems can define default resources that automatically synchronize with character sheets.

### ⚔️ Initiative & Combat Tracker
- **Encounter Management**: Track initiative order for players, companions, NPCs, and enemies.
- **Turn & Round Cycling**: Step through combat rounds with next/previous turn controls and current turn indicators.
- **In-Tracker Stats**: View and update participant HP, MP, resources, armor class, and status conditions directly from the tracker window.
- **One-Click Initiative Rolling**: Roll initiative using character attributes/skills with automatic list re-sorting.
- **Fast Participant Addition**: Quickly add existing characters or create ad-hoc monsters/NPCs on the fly.

### 🎒 Inventory & Item Management
- **Full Inventory System**: Track items, equipment, consumables, quest items, and valuables.
- **Rich Item Metadata**: Name, description, category, rarity tiers (Common, Uncommon, Rare, Epic, Legendary, Artifact), quantity, weight, and gold/currency value.
- **Custom Categories & Sorting**: Filter items by category, search by keyword, and sort by name, rarity, or value.
- **Contextual Actions**: Equip, use, split, duplicate, and discard items with built-in confirmation safety.

### 🛡️ Gear & Equipment Loadout
- **Equipment Slots**: Main Hand, Off Hand, Head, Body, Hands, Legs, Feet, Neck, Ears, Wrists, Rings, and custom accessories.
- **Stat & Attribute Modifiers**: Gear items can grant passive bonuses to attributes, skills, and resource maximums.
- **Dynamic Gear Equipping**: Equipping and unequipping items automatically updates your effective character stats in real-time.

### 🦾 Cyberware & Augmentations
- **Cybernetics / Augmentation Engine**: Dedicated system for sci-fi, cyberpunk, or magitek campaigns.
- **Body Part Slots**: Neural, Ocular, Cranial, Torso, Arms, Legs, Subdermal, and Internal systems.
- **Essence / Humanity / Cost Tracking**: Balance powerful cybernetic upgrades against character resource constraints.
- **Active & Passive Buffs**: Augmentations can grant custom abilities, stat multipliers, and resistance perks.

### 🎲 Tabletop Dice Systems & Rule Engine
- **Multiple Tabletop Resolution Systems**:
  - **Standard / D&D System**: d20/dX roll with attribute bonuses, modifiers, critical hits, and fumbles.
  - **Dice Pool System**: Roll multiple dice against a configurable success threshold (e.g. Shadowrun, World of Darkness).
  - **Percentile System (d100)**: Roll-under target system with degree of success/failure calculations based on intervals (e.g. Call of Cthulhu).
- **Supported Dice Types**: d4, d6, d8, d10, d12, d20, d100, and arbitrary multi-dice expressions (e.g. `4d6k3`, `2d8+5`).
- **Customizable System Rules**: Configure Advantage/Disadvantage, temporary & permanent bonuses, epic attributes, and success thresholds.

### 💬 Chat Integration & Broadcasting
- **In-Game Chat Broadcast**: Automatically outputs formatted roll results, calculations, and ability descriptions to the in-game `/say`, `/party`, or custom chat channels.
- **Detailed Roll Breakdown**: Optional mode to display individual dice rolls, modifiers, and step-by-step arithmetic.
- **Advantage & Disadvantage**: Full native support for advantage, disadvantage, and keep-highest/lowest mechanics.

### 📁 Profile Management & Data Persistence
- **Modern ImGui File Browser**: Custom-built, fully localized file picker with quick-access bookmarks (Documents, Desktop, Game folder), drive selection, path navigation, file creation, and deletion.
- **JSON Import & Export**:
  - Save, load, duplicate, and share **Character Sheets** (`.json`).
  - Save, load, and swap custom **Dice Systems** (`.json`).
  - Robust migration and backward-compatibility layers ensure old sheets load seamlessly.

### 🌐 Localization & Customization
- **Bilingual Interface**: Full localization support for both **English** and **French (Français)** with dynamic instant switching.
- **Configurable Settings**: Customizable chat outputs, window toggles, font scaling, confirmation dialogs, and display options.

---

## 🕹️ Usage & Commands

| Command | Description |
| :--- | :--- |
| `/soulstone` | Opens or toggles the main Soulstone hub window. |

You can also access Soulstone windows, configuration, and tools through the **Dalamud Plugin Installer** or title bar shortcuts.

---

## 🚀 Navigation & Windows

| Window / Tab | Description |
| :--- | :--- |
| **RP Sheet (`CharacterWindow`)** | Character identity, appearance, biography, quick glance hooks, and relationships. |
| **Stat Sheet (`CharStatsWindow`)** | Dynamic HP/MP/resources, attributes, skills, abilities, and quick roll cards. |
| **Inventory (`InventoryWindow`)** | Item management, categories, weight, value, search, and item inspection. |
| **Gear (`GearWindow`)** | Equipment slots, equipped items, and passive stat bonuses. |
| **Augmentations (`AugmentationsWindow`)** | Cyberware and magitek installations, slot allocations, and essence tracking. |
| **Initiative Tracker (`InitiativeTrackerWindow`)** | Turn-based combat tracking, rounds, health management, and order sorting. |
| **Dice Rolling (`DiceWindow`)** | Quick dice roller with expression evaluator, advantage toggles, and chat broadcast. |
| **Dice System (`DiceSystemWindow`)** | Tabletop RPG rule engine configuration, thresholds, and default resource setups. |
| **Settings (`ConfigWindow`)** | Plugin preferences, localization selection, and chat formatting options. |

---

## 🏗️ Architecture & Development

Soulstone is developed in C# targeting **.NET 10.0 (Windows)** and built on top of **Dalamud**.

- **Architecture Documentation**: For detailed technical documentation on classes, design patterns, datamodels, and subsystems, see [`docs/DOCUMENTATION.md`](docs/DOCUMENTATION.md).
- **Unit Test Suite**: Fully covered by unit tests using **xUnit** and **FluentAssertions** in `Soulstone.Tests`.

To build the project locally:
```powershell
dotnet build Soulstone.sln
dotnet test
```

---

## 💬 Community & Support

Join the community Discord for discussion, feature requests, sharing custom character sheets, and dice systems:

[<img src="https://upload.wikimedia.org/wikipedia/fr/4/4f/Discord_Logo_sans_texte.svg" width="20" height="20" alt="Discord Logo"> **Join the Community Discord**](https://discord.gg/6hkvbXbPRF)

---

## 📄 License

This project is licensed under the terms of the [MIT License](LICENSE.md).
