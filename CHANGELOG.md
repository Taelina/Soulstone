# Changelog

All notable changes to the Soulstone project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.1] - 2026-09-21

### Added
- **Ruleset Skill & Ability Configuration**:
  - Added skill and ability creation, editing, removal, and ordering controls to custom dice-system templates.
  - Added linked attribute and linked skill configuration for ruleset abilities and skills.
- **Character Ability Management**:
  - Added a dedicated ability tab with search, creation, editing, and removal controls to the feats and traits interface.

### Changed
- **Character Sheet Layouts**:
  - Refined the presentation and selection layouts for character stats, gear, augmentations, and feats.
- **Localization**:
  - Added matching English and French strings for ruleset skills, linked attributes and skills, and character ability management.
- **Version Manifests & Metadata**:
  - Bumped version to `1.2.1.0` across `Soulstone.csproj`, `Soulstone.json`, and `SoulstoneRep.json` and updated repository download links for release `V1.2.1`.

---

## [1.2.0] - 2026-09-20

### Added
- **Complex Dice Expression & Compound Term Engine**:
  - Implemented compound dice term parsing and arithmetic evaluation (`DiceTerm`) supporting multi-die notation and custom formulas.
- **Enhanced Stat & Attribute Formula Evaluation**:
  - Extended `StatFormulaEvaluator` to support secondary attributes, evaluated dynamic stats, and custom formula-driven attribute values.
- **Ruleset & Character Stat Customization**:
  - Added support for formula-driven secondary attributes and ruleset extensions in `DiceSystemWindow` and `CharStatsWindow`.
  - Added enhanced slot configuration for gear and augmentations in `GearWindow`.

### Changed
- **Version Manifests & Metadata**:
  - Bumped version to `1.2.0.0` across `Soulstone.csproj`, `Soulstone.json`, and `SoulstoneRep.json`.
- **Localization**:
  - Added localized strings for new dice formula options, attribute formulas, and UI controls in English and French dictionaries (`en.json` and `fr.json`).

---

## [1.1.2] - 2026-09-19

### Fixed
- **Fully Dynamic Character Resources**:
  - Removed the remaining legacy Health and Mana fields, properties, synchronization logic, and formula fallbacks from character sheets.
  - Updated party synchronization and initiative tracker integration to use generic character resources exclusively, preventing Health and Mana from being restored implicitly.

### Changed
- **Version Manifests & Metadata**:
  - Bumped version to `1.1.2.0` across `Soulstone.csproj`, `Soulstone.json`, and `SoulstoneRep.json`.

---

## [1.1.1] - 2026-09-19

### Added
- **Dynamic Group Management Resources & Visibility Controls**:
  - Replaced legacy hardcoded default "Health" and "Mana" progress bars in `GroupWindow` with dynamic, ruleset-defined active character resources in both card vitals and tactical grid views.
  - Added granular per-resource visibility controls (`ShowInGroup`) to `ResourceDefinition` and `CharacterResource` configurable in `DiceSystemWindow` and `CharStatsWindow`.
  - Added global group resource visibility setting (`ShowGroupResources`) in `Configuration`, `ConfigWindow`, and `GroupWindow` toolbar.
  - Extended party synchronization payloads (`PresencePayload`, `ResourceUpdatePayload`, and `PartyMemberSyncData`) to broadcast resource visibility across peers.
- **Unified Resource Presentation & Visual Styling**:
  - Centralized resource bar coloring (`UiUtils.GetResourceColor`) and widget rendering across Group Management, RP Sheet, Remote Character Inspect, Character Stats, and Initiative Tracker.
  - Added collapsible dynamic resources section to `CharacterWindow` and `CharacterInspectWindow`.
- **Character Inspection & Quick Looks**:
  - Added full support for serializing, publishing, and rendering Quick Glance Hooks (`CharacterQuickLook1..5`), distinctive features, and reputation in `CharacterInspectWindow`.
  - Added retry and refresh action buttons to `CharacterInspectWindow` for quick re-fetching of remote character sheets.
- **Default Relay & API Configuration**:
  - Set default relay and character cloud API endpoint to `http://82.65.2.251:5077` across all lookup clients with automatic normalization for user-entered IPs and hostnames.

### Fixed
- **Character Sheet Deserialization**:
  - Resolved `InvalidOperationException` property name collision on `CharacterSheet` JSON deserialization in `CharacterApiClient`.

### Changed
- **Version Manifests & Metadata**:
  - Bumped version to `1.1.1.0` across `Soulstone.csproj`, `Soulstone.json`, and `SoulstoneRep.json`.
- **Localization**:
  - Added localized strings for group resource toggles and inspect retry actions across English and French dictionaries (`en.json` and `fr.json`).

---

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
