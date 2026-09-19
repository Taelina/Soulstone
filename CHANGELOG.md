# Changelog

All notable changes to the Soulstone project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-09-19

### Added
- **Feats & Traits Management**:
  - Added dedicated `Feat` data model supporting custom names, descriptions, categories/types (Traits, Perks, Flaws, Feats), and customizable stat/ability modifiers.
  - Added `FeatsWindow` with full creation, editing, removal, search, category filtering, and modifier configuration tools.
  - Seamlessly integrated Feats collections into `CharacterSheet` with full JSON persistence and backward compatibility.
- **Character Cloud Registry & Remote Sheet Inspection**:
  - Added RESTful character sheet publishing and lookup API in `Soulstone.SyncServer` via `CharacterSheetRegistry` (`/api/characters`, `/api/characters/{characterId}`).
  - Added `CharacterApiClient` for asynchronous HTTP communication between the plugin and the synchronization server.
  - Added `CharacterInspectWindow` allowing players and Dungeon Masters to inspect remote player character sheets, profiles, and vitals without requiring an active WebSocket party session.
- **Dice Roll History & Audit Logging**:
  - Added `DiceHistoryManager` providing a comprehensive, persistent audit trail for all dice rolls with timestamps, rolling player names, formulas, notation, breakdowns, and final totals.
  - Integrated roll history review and filtering directly within `DiceWindow`.
- **Field-Level Character Sheet Visibility & Privacy Toggles**:
  - Implemented granular field-level privacy controls (`IsFieldHidden`, `ToggleFieldHidden`, `HiddenFields`) across `CharacterSheet`.
  - Added intuitive visibility toggle buttons (eye/eye-slash) to character identity, appearance, OOC notes, quick-look hooks, and background/relationship sections in `CharacterWindow`.
- **Modern Card-Based UI Redesign & Enhanced Widgets**:
  - Redesigned UI presentation in `UiUtils` with modern card containers, accent stripes, styled collapsing headers, pill badges, and interactive sectioned counters.
  - Overhauled layout and responsiveness across `CharacterWindow`, `CharStatsWindow`, `GroupWindow`, `GearWindow`, `InventoryWindow`, `AugmentationsWindow`, `DiceWindow`, and `DiceSystemWindow`.
- **Item & Gear Enhancements**:
  - Extended `Item` and `GearItem` datamodels with item quality tiers, slot validations, enhanced formula bonuses, and durability tracking.

### Changed
- **Version Manifests & Metadata**:
  - Bumped version to `1.1.0.0` across `Soulstone.csproj`, `Soulstone.json`, and `SoulstoneRep.json`.
- **Localization**:
  - Added complete English and French dictionaries (`en.json` and `fr.json`) for Feats, Character Inspection, Dice History, and Privacy Toggles.
- **Documentation**:
  - Updated `README.md`, `docs/DOCUMENTATION.md`, `Soulstone.SyncServer/README.md`, `docs/DEPLOYMENT.md`, and `docs/FORMULA_SOLVER.md` reflecting all 1.1.0 capabilities and architecture additions.

---

## [1.0.5] - 2026-09-13

### Added
- **Stat, Resource & Slot Reordering Controls**:
  - Added full reordering controls (Move Up / Move Down / Move Left / Move Right) for resources, attributes, skills, abilities, equipment slots, and augmentation slots in character sheets and dice systems.
  - Implemented underlying reordering helpers (`MoveResource`, `MoveAttribute`, `MoveSkill`, `MoveAbility`, `MoveEquipmentSlot`, `MoveAugmentationSlot`) across `CharacterSheet` and `DiceSystem`.
- **Universal Resource Deletion & Dynamic Resource Flexibility**:
  - Unlocked deletion for any resource, including default "Health" and "Mana", across custom dice systems and character sheets.
  - Removed forced injection/respawning of "Health" and "Mana" resources.
  - Updated character stat sheet, tactical grid, and party group management interfaces to gracefully handle characters and rulesets with no Health or Mana resources without rendering broken empty gauges.

### Fixed
- **Formula Solver Recursion & Circular Reference Protection**:
  - Added recursive dependency cycle tracking (`resolvingStats`) and stack depth limit enforcement (`MaxRecursionDepth = 32`) in `StatFormulaEvaluator`.
  - Recursive self-referencing formulas now safely log detailed diagnostic error call chains and return safe default values instead of crashing the game client with a stack overflow.

### Changed
- **Version Manifests & Metadata**:
  - Bumped version to `1.0.5.0` across `Soulstone.csproj`, `Soulstone.json`, and `SoulstoneRep.json`.
- **Localization**:
  - Added localized tooltips for reordering controls in English and French dictionaries (`en.json` and `fr.json`).

---

## [1.0.4] - 2026-09-12

### Added
- **Configurable Dice Pool Max Roll Successes**:
  - Added `DicePoolMaxSuccessCount` setting in `DiceSystem` allowing dice pool rulesets to count multiple successes when rolling the maximum face on a die (e.g. counting 10s as 2 successes).
  - Added dice pool max success configuration field in `DiceSystemWindow` threshold settings.
- **Linked Dice System Management**:
  - Added editable `linkedDiceSystem` configuration to both Character Identity and Character Stat Sheet interfaces.
  - Automatically activates a character's linked dice system upon loading their sheet (when not participating in an active DM session).

### Fixed
- **Character Sheet Creation**:
  - Fixed an issue where the "Create new character sheet" modal failed to open when no character sheet was currently loaded.
- **Ruleset Resource and Stat Scoping**:
  - Fixed character stat sheets, party presence sync, and initiative tracker displaying attributes, skills, abilities, and resources from all previously loaded systems by scoping them strictly to the active ruleset.

### Changed
- **Version Manifests & Metadata**:
  - Bumped version to `1.0.4.0` across `Soulstone.csproj`, `Soulstone.json`, and `SoulstoneRep.json`.

---

## [1.0.3] - 2026-09-12

### Added
- **Enhanced Resource Pool Types (Bar, Counter, Flat Number)**:
  - Added support for three distinct resource pool types: `Bar` (gauge with current and calculated max), `Counter` (increments/decrements from 0 up to max), and `FlatNumber` (evaluated formula stat directly rollable with active dice systems).
  - Configurable resource types in ruleset templates, character sheets, party sync, and stat sheet interfaces.
- **PC vs NPC Distinction in Initiative Tracker**:
  - Added dedicated PC and NPC tags, visual indicators, and quick toggle buttons in `InitiativeTrackerWindow`.
  - Added combatant filter controls (All, PCs, NPCs) for Dungeon Masters.
- **Universal Character Variable Formula Solver**:
  - Extended `StatFormulaEvaluator` to resolve any character sheet property (Level, XP, HP, MP, Initiative, Inventory items, Gear bonuses, Buff bonuses, and arbitrary sheet fields).
  - Added support for dot-notation property accessors (`.Mod`, `.Current`, `.Max`, `.Base`, `.Gear`, `.Buff`), comparison operators, and math functions (`pow`, `exp`, `log`, `sign`, `trunc`, `dndmod`, `if`).
  - Added comprehensive formula solver reference manual in `docs/FORMULA_SOLVER.md`.
- **Attribute, Skill, and Ability Descriptions**:
  - Added optional description fields to Attributes, Skills, and Abilities with hover tooltips in `CharStatsWindow`.

### Changed
- **Licensing**:
  - Clarified and aligned project licensing under the **GNU Affero General Public License v3.0 (AGPL-3.0)** in `README.md` and repository metadata.

---

## [1.0.2] - 2026-09-12

### Added
- **Dynamic Skill Attribute Linking**:
  - Added `DynamicSkillAttributeLinking` option to `DiceSystem` rulesets.
  - When enabled, skills are uncoupled from static attributes; clicking a skill roll triggers a modal prompt to dynamically select the attribute to link for that specific roll.
- **NPC Character Sheets in Initiative Tracker**:
  - Added support for attaching full `CharacterSheet` instances to combatants and NPCs in `InitiativeTrackerWindow`.
  - Added capabilities to generate blank NPC sheets on the fly or load premade `.json` character sheet templates from the sheets directory.
  - Added an in-tracker NPC character sheet inspector modal to view and manage vitals, dynamic resources, attributes, skills, and active buffs directly within combat encounters.
- **Formula-Based Initiative Calculation**:
  - Added `InitiativeStatType.Formula` and configurable `InitiativeFormula` field to `DiceSystem`.
  - Integrated `StatFormulaEvaluator` with support for `@ATTRIBUTE` and `{ATTRIBUTE}` bracketed variable placeholders to evaluate arbitrary mathematical initiative expressions (e.g., `(@DEX + @INT) / 2`).
  - Added ruleset-driven initiative rolling in `DiceWindow` and `InitiativeTrackerWindow`, accurately displaying ruleset notation, dice type, and stat sources.
- **Dice System Persistence**:
  - Added `LastActiveDiceSystem` configuration property in `Configuration.cs` to persist the active dice system filename and automatically restore it across plugin reloads.

### Fixed
- **Group View DM / Host Role Detection**:
  - Fixed an issue in `PartySyncManager` and `GroupWindow` where non-host and non-leader members could mistakenly see themselves or other non-host players marked as the DM / session leader.
- **Initiative Tracker Participant Re-rolls**:
  - Fixed individual participant re-roll button in `InitiativeTrackerWindow` to invoke `InitiativeTrackerManager.RerollParticipant`, correctly evaluate active ruleset dice types, bonuses, and buffs, re-sort combatants, and broadcast updates to connected peers.
- **Dice Roller Initiative Quick Card**:
  - Fixed `DiceWindow` initiative quick card to use the active dice system's dice configuration, notation, and source modifier instead of defaulting to a hardcoded d20 roll.
- **Cyberware (Augmentations) Window UI & Icon Buttons**:
  - Replaced text "install" buttons with FontAwesome icon buttons (`Plus`, `Exchange`, `Trash`, `Times`) matching the gear loadout interface.
  - Fixed layout and slot card rendering bug in `AugmentationsWindow` where empty cyberware slots overlapped or failed to render properly.
- **Chat Echo Localization**:
  - Localized all echoed in-game chat messages (dice rolls, initiative announcements, combat resets, ruleset notifications) across English and French dictionaries (`en.json` and `fr.json`).

### Changed
- **Version Manifests & Metadata**:
  - Bumped version to `1.0.2.0` across `Soulstone.csproj`, `Soulstone.json`, and `SoulstoneRep.json`.
- **Documentation**:
  - Updated `README.md` and `docs/DOCUMENTATION.md` with detailed sections covering formula initiative, dynamic skill attribute linking, NPC character sheets, and ruleset persistence.

---

## [1.0.1] - 2026-09-10

### Added
- Multi-language localization support (English & French) with hot-reloading dictionaries.
- Enhanced party synchronization with encrypted payload sharing via standalone WebSocket relay.
- Flexible dice system ruleset editor supporting D&D 5e, Dice Pool, and Percentile d100 systems.
- Dynamic resource bars with custom mathematical formula evaluations.
