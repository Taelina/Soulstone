# Soulstone - Technical & Code Documentation

Comprehensive architectural and codebase documentation for **Soulstone**, a Dalamud companion plugin for **Final Fantasy XIV** designed for roleplay character management, tabletop stats, cyberware augmentations, inventory, initiative tracking, and custom dice rolling systems.

---

## 📑 Table of Contents
1. [Architectural Overview](#1-architectural-overview)
2. [Project Structure](#2-project-structure)
3. [Core Datamodels](#3-core-datamodels)
4. [Managers & State Architecture](#4-managers--state-architecture)
5. [Windows & UI Presentation Layer](#5-windows--ui-presentation-layer)
6. [Utilities & Helper Subsystems](#6-utilities--helper-subsystems)
7. [Testing Architecture & Coverage](#7-testing-architecture--coverage)
8. [Extensibility & Developer Guide](#8-extensibility--developer-guide)

---

## 1. Architectural Overview

Soulstone follows a modular, reactive desktop-in-game architecture designed specifically for the **Dalamud** plugin framework and **ImGui** immediate-mode rendering pipeline.

```
┌────────────────────────────────────────────────────────────────────────┐
│                             Dalamud API                                │
│        (CommandManager, ChatGui, PluginInterface, WindowSystem)        │
└──────────────────────────────────┬─────────────────────────────────────┘
                                   │
┌──────────────────────────────────▼─────────────────────────────────────┐
│                           Plugin Lifecycle                             │
│                  (Plugin.cs, Configuration.cs)                         │
└─────────┬────────────────────────┬───────────────────────┬─────────────┘
          │                        │                       │
┌─────────▼───────────┐  ┌─────────▼──────────┐  ┌─────────▼─────────────┐
│  Managers & State   │  │   UI & Windows     │  │  Utility Subsystems   │
│ - CharacterManager  │  │ - MainWindow       │  │ - StatFormulaEvaluator│
│ - DiceSystemManager │  │ - CharacterWindow  │  │ - DiceRoll            │
│ - InitiativeTracker │  │ - CharStatsWindow  │  │ - Messages / Chat     │
│ - LocalizationMgr   │  │ - InventoryWindow  │  │ - UiUtils / Raii      │
│                     │  │ - GearWindow       │  │ - ImageHelper         │
│                     │  │ - AugmentationsWin │  │ - ImGuiFileWindow     │
│                     │  │ - InitiativeWin    │  │                       │
│                     │  │ - DiceWindow       │  │                       │
│                     │  │ - DiceSystemWindow │  │                       │
│                     │  │ - ConfigWindow     │  │                       │
└─────────┬───────────┘  └─────────┬──────────┘  └─────────┬─────────────┘
          │                        │                       │
          └────────────────────────┼───────────────────────┘
                                   │
                    ┌──────────────▼──────────────┐
                    │      Domain Datamodels      │
                    │ - CharacterSheet            │
                    │ - DiceSystem                │
                    │ - Attribute, Skill, Ability │
                    │ - CharacterResource         │
                    │ - ResourceDefinition        │
                    │ - Item, GearItem            │
                    │ - InitiativeParticipant     │
                    └─────────────────────────────┘
```

### Key Design Principles:
- **Immediate-Mode UI Safety**: All UI rendering is wrapped using RAII wrappers (`ImRaii.Group`, `ImRaii.Child`, `ImRaii.PushColor`, `ImRaii.PushId`) to guarantee clean stack unwinding and avoid ImGui stack corruption.
- **Dynamic Math Evaluation**: Stat and resource formulas use an in-house recursive-descent mathematical parser (`StatFormulaEvaluator`) that safely evaluates variable bindings (e.g. `@STR * 2 + 10`) without runtime reflection or external script injection risks.
- **Robust Schema Migration**: JSON serialization for datamodels includes backward-compatibility fallbacks, ensuring sheets created in previous plugin versions load seamlessly.
- **Thread-safe Managers**: State managers adhere to single-instance patterns with atomic operations and UI thread affinity.

---

## 2. Project Structure

```
Soulstone/
├── Configuration.cs                 # Plugin user preferences & persistence
├── Plugin.cs                        # Plugin entry point & command registrations
├── Datamodels/
│   ├── Ability.cs                   # Character abilities with dice/modifier bindings
│   ├── Attribute.cs                 # Core character attributes
│   ├── CharacterResource.cs         # Dynamic character resource instances (HP/MP/custom)
│   ├── CharacterSheet.cs            # Complete character profile, stats, inventory & gear
│   ├── DiceSystem.cs                # Tabletop dice system rules & thresholds
│   ├── GearItem.cs                  # Equipment slots and item associations
│   ├── InitiativeParticipant.cs     # Combat encounter participant
│   ├── Item.cs                      # Generic inventory items and cyberware
│   ├── ResourceDefinition.cs        # Resource template defined by dice systems
│   └── Skill.cs                     # Skills linked to primary attributes
├── Managers/
│   ├── CharacterManager.cs          # Active character sheet lifecycle & operations
│   ├── DiceSystemManager.cs         # Active dice system ruleset provider
│   ├── InitiativeTrackerManager.cs  # Turn order, combat rounds & participant tracking
│   └── LocalizationManager.cs       # Bilingual string provider (English/French)
├── Utils/
│   ├── DiceRoll.cs                  # Expression parser, dice roller & result formatting
│   ├── ImageHelper.cs               # Texture loading, caching & rounded avatar rendering
│   ├── ImGuiFileWindow.cs           # Standalone file picker with bookmarks & drive browsing
│   ├── Messages.cs                  # In-game chat injection & channel formatting
│   ├── StatFormulaEvaluator.cs      # Lexer & recursive descent formula parser
│   └── UiUtils.cs                   # Custom ImGui widgets: cards, badges, modal dialogs
└── Windows/
    ├── AugmentationsWindow.cs       # Cyberware installations & body slot management
    ├── CharacterWindow.cs           # RP profile, appearance, hooks & social relationships
    ├── CharStatsWindow.cs           # Dynamic stats, resources, attributes & abilities
    ├── ConfigWindow.cs              # Plugin configuration & preferences
    ├── DiceSystemWindow.cs          # Dice system rule editor & resource templates
    ├── DiceWindow.cs                # Quick dice rolling tool & expression calculator
    ├── GearWindow.cs                # Equipment loadout and passive bonus inspector
    ├── InitiativeTrackerWindow.cs   # Combat tracker interface & turn cycler
    ├── InventoryWindow.cs           # Item management, categories, search & detail viewer
    └── MainWindow.cs                # Central hub window & tab coordinator
```

---

## 3. Core Datamodels

### 3.1 `CharacterSheet`
The aggregate root for a player's character data.
- **RP & Identity**: `characterName`, `characterAge`, `characterGender`, `characterRace`, `characterClan`, `characterJob`, `characterPronouns`, `characterBuild`, `characterHeight`, `characterWeight`, `characterEyeColor`, `characterHairColor`, `characterSkinColor`, `characterScars`, `characterTattoos`, `characterQuirks`.
- **Out of Character (OOC)**: `playerTimezone`, `playerAvailability`, `oocNotes`.
- **Hooks & Lore**: `characterQuickLooks` (5 custom glance hooks), `characterBirthplace`, `characterOrigin`, `characterAffiliation`, `characterOccupation`, `characterBackground`, `characterReputation`.
- **Social Network**: `characterFamily`, `characterFriends`, `characterEnemies`, `characterAllies`.
- **Stat System**:
  - `attributes`: List of `Attribute` objects.
  - `skills`: List of `Skill` objects linked to attributes.
  - `abilities`: List of `Ability` objects linked to skills/attributes.
  - `resources`: List of `CharacterResource` dynamic pools.
- **Inventory & Gear**:
  - `inventory`: List of `Item` objects.
  - `equippedGear`: Dictionary mapping `GearSlot` to `Item?`.
  - `augmentations`: List of cybernetic `Item` objects.
- **Calculations**:
  - `GetEffectiveAttributeValue(name)`: Computes base attribute value plus bonuses from active gear, cyberware, and temporary modifiers.
  - `GetEffectiveResourceMax(resourceName, diceSystem)`: Evaluates dynamic formula using effective attribute values.

### 3.2 `DiceSystem`
Defines the active tabletop rule engine.
- **System Types**:
  - `Standard`: d20 or dX roll + modifier vs DC.
  - `DicePool`: Multi-dice roll counting successes above `successThreshold`.
  - `Percentile`: d100 roll-under system with degree of success/failure calculations.
- **Features & Flags**:
  - `diceType`: d4, d6, d8, d10, d12, d20, d100.
  - `systemHasAugmentations`: Enables Cyberware/Augmentations tab.
  - `systemHasAdvantage`: Enables Advantage/Disadvantage mechanics.
  - `systemHasThresholds`: Enables Critical/Success/Failure/Fumble tiers.
  - `resourceDefinitions`: Default resource pool templates with dynamic formulas.

### 3.3 `Item` & `GearItem`
- **`ItemType`**: `Generic`, `Consumable`, `Equipment`, `Augmentation`, `Quest`, `Valuable`.
- **`ItemRarity`**: `Common`, `Uncommon`, `Rare`, `Epic`, `Legendary`, `Artifact`.
- **Properties**: `id`, `name`, `description`, `category`, `rarity`, `quantity`, `weight`, `value`, `statModifiers`, `bodySlot`, `essenceCost`.
- **`GearSlot`**: `MainHand`, `OffHand`, `Head`, `Body`, `Hands`, `Legs`, `Feet`, `Neck`, `Ears`, `Wrists`, `RightRing`, `LeftRing`, `Accessory`.

### 3.4 `CharacterResource` & `ResourceDefinition`
- Dynamic tracking for resource pools with current, base minimum, base maximum, and optional formula.
- Formulas support referencing any attribute: e.g. `@CON * 10 + 20`.

### 3.5 `InitiativeParticipant`
- Represents a combatant in an encounter.
- Fields: `id`, `name`, `initiativeValue`, `tieBreaker`, `currentHp`, `maxHp`, `armorClass`, `statusEffects`, `isNpc`, `isCurrentTurn`, `isDefeated`.

---

## 4. Managers & State Architecture

### 4.1 `CharacterManager`
- Manages the active `CharacterSheet` instance.
- Handles saving and loading `.json` profile files to disk.
- Provides fallback initialization when starting fresh.

### 4.2 `DiceSystemManager`
- Manages the active `DiceSystem` instance.
- Loads default D&D 5e / d20 rule system on first start.
- Synchronizes default resources defined in the ruleset into the character sheet.

### 4.3 `InitiativeTrackerManager`
- Manages active combat encounter state: participants list, round count, active turn index.
- Methods:
  - `AddParticipant(name, initiative, hp, maxHp, isNpc)`
  - `RemoveParticipant(id)`
  - `SortByInitiative()`
  - `NextTurn()` / `PreviousTurn()`
  - `ResetCombat()`

### 4.4 `LocalizationManager`
- Provides localized strings via `GetLocalizedString(key)` and parameterized formatting via `GetLocalizedString(key, args...)`.
- Automatically loads embedded JSON translation files (`Soulstone/Localizations/en.json`, `Soulstone/Localizations/fr.json`).
- Supports hot-loading external community translation files from `<DataLocation>/Localizations/*.json`.
- Supports instant language switching between English and French without requiring plugin restart.
- Thread-safe dictionary lookups with automatic fallback to English if a key is missing in French, and fallback to key name if missing entirely.

---

## 5. Windows & UI Presentation Layer

All windows inherit from Dalamud's `Window` class and are managed through the Dalamud `WindowSystem`.

| Window | Responsibility |
| :--- | :--- |
| `MainWindow` | Tab coordinator providing top bar status, navigation tabs, and system indicators. |
| `CharacterWindow` | Identity, appearance, background lore, customizable quick looks, and categorized relationship manager. |
| `CharStatsWindow` | Dynamic resource bars, attribute cards, skill tree, and ability cards with click-to-roll buttons. |
| `InventoryWindow` | Searchable item list, rarity badges, category filtering, weight/value summary, and item detail/editor modals. |
| `GearWindow` | Interactive equipment paper doll loadout, equip slot selectors, and passive modifier summary. |
| `AugmentationsWindow` | Cyberware body slot layout, installed cybernetics inspector, and essence/humanity tracker. |
| `InitiativeTrackerWindow` | Combat tracker with initiative sorting, turn cycling, quick damage/heal buttons, and condition badges. |
| `DiceWindow` | Freeform dice expression calculator, advantage toggles, and chat output broadcast. |
| `DiceSystemWindow` | Rule engine editor for system type, thresholds, dice types, and dynamic resource definitions. |
| `ConfigWindow` | Settings window for language selection, chat channels, detailed roll output, and UI options. |
| `ImGuiFileWindow` | Standalone modal file picker with drive navigation, bookmarks, and extension filtering. |

---

## 6. Utilities & Helper Subsystems

### 6.1 `StatFormulaEvaluator`
- Robust recursive-descent math parser supporting:
  - Binary operators: `+`, `-`, `*`, `/`, `^`, `%`.
  - Parentheses: `( ... )`.
  - Variables: `@ATTRIBUTE_NAME` (case-insensitive lookup from attribute dictionaries).
  - Unary operators: `+`, `-`.
- Gracefully handles division by zero (returns `0`) and malformed expressions (returns `0` with safe error handling).

### 6.2 `DiceRoll`
- Evaluates tabletop dice expressions such as `1d20+5`, `3d6`, `2d8-1`.
- Implements:
  - Standard Rolls with critical hit / fumble detection.
  - Advantage & Disadvantage mechanics (`2d20kh1`, `2d20kl1`).
  - Dice Pool success counting against target thresholds.
  - Percentile d100 roll-under with degree of success levels (Critical Success, Extreme, Hard, Regular, Failure, Critical Fumble).

### 6.3 `UiUtils`
- Standardized UI widgets:
  - `Card`: Renders modern framed cards with background color and border rounding.
  - `Badge`: Renders colored status badges.
  - `IconButton`: Renders FontAwesome icon buttons with proper spacing.
  - `ConfirmationModal`: Modal confirmation dialogs for destructive actions.

### 6.4 `ImageHelper`
- Manages avatar texture loading from local file paths.
- Caches textures in memory and renders circular or rounded avatar portraits.

### 6.5 `Messages`
- Injects formatted roll results directly into Final Fantasy XIV chat channels (`/say`, `/party`, or standard chat log).

---

## 7. Testing Architecture & Coverage

The `Soulstone.Tests` project provides automated unit testing using **xUnit** and **FluentAssertions**.

### Test Suite Structure:
- **`Datamodels/`**:
  - `CharacterSheetTests`: Sheet serialization, attribute calculations, relationship operations.
  - `DiceSystemTests`: Rule set creation, threshold validation, dice type mapping.
  - `ItemTests` & `GearItemTests`: Inventory operations, slot equipping, stat modifier calculations.
  - `GenericResourceTests` & `ResourceFormulaTests`: Dynamic formula evaluation, attribute binding, max calculations.
  - `InitiativeParticipantTests` & `CharacterSheetInitiativeTests`: Combatant stats and status effects.
  - `AugmentationAndGenericGearTests`: Cyberware slots and essence constraints.
- **`Managers/`**:
  - `CharacterManagerTests`: Profile save/load and state isolation.
  - `DiceSystemManagerTests`: Ruleset persistence and resource synchronization.
  - `InitiativeTrackerManagerTests`: Turn order sorting, round cycling, participant state.
  - `LocalizationManagerTests`: English and French key parity, missing key fallbacks.
- **`Utils/`**:
  - `DiceRollTests`: Standard, Dice Pool, and Percentile roll arithmetic and string formatting.
  - `MessagesTests`: Chat payload generation.
  - `ImageHelperTests` & `FileBrowserWindowTests`: Path resolution and file helper logic.

All tests run sequentially or under `[Collection("NonParallelCollection")]` where static singleton state is involved.

---

## 8. Extensibility & Developer Guide

### Adding a New Localization String
1. Open `Soulstone/Localizations/fr.json` and add the translation:
   ```json
   "MyNewKey": "Mon texte en français"
   ```
2. Open `Soulstone/Localizations/en.json` and add the translation:
   ```json
   "MyNewKey": "My English text"
   ```
3. Access the localized string anywhere in UI via:
   ```csharp
   LocalizationManager.Instance.GetLocalizedString("MyNewKey");
   // Or with format arguments:
   LocalizationManager.Instance.GetLocalizedString("MyFormattedKey", arg1, arg2);
   ```

### Overriding / Community Translations
Drop custom `<language_code>.json` (e.g. `de.json`, `es.json`, `fr.json`) into the plugin's `Localizations/` directory located inside the plugin data folder. `LocalizationManager` automatically merges and overrides localized strings at runtime.

### Adding a Custom Dynamic Resource Formula
Formulas use `@` prefixed attribute names:
```csharp
var formula = "@CON * 3 + @STR / 2 + 10";
int result = StatFormulaEvaluator.Evaluate(formula, sheet.GetAttributeMap());
```

### Adding a New Equipment or Augmentation Slot
1. Extend `GearSlot` or `BodySlot` enum in `Soulstone/Datamodels/GearItem.cs` or `Item.cs`.
2. Add corresponding UI slot selector in `GearWindow.cs` or `AugmentationsWindow.cs`.
3. Add localized slot names in `LocalizationManager.cs`.
