using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Soulstone.Windows
{
    internal class CharStatsWindow
    {
        private enum StatRollType
        {
            Attribute,
            SavingThrow,
            Skill,
            Ability
        }

        public string diceType = "";

        private bool showBuffPopup = false;
        private bool showResourcePopup = false;
        private bool showStatRollModal = false;
        private StatRollType statRollType = StatRollType.Attribute;
        private string statRollName = "";
        private Datamodels.Attribute? statRollAttribute = null;
        private Skill? statRollSkill = null;
        private Ability? statRollAbility = null;
        private string selectedDynamicAttr = "";
        private int rollBonusOrPenalty = 0;

        private CharacterSheet? currentCharacter = null;
        private DiceSystem? currentDiceSystem = null;

        private bool editingStats = false;

        private bool showAttributesPopup = false;
        private bool showSkillPopup = false;
        private bool showAbilitiesPopup = false;
        private string newAttributeName = string.Empty;
        private int newAttributeValue = 0;
        private string newAttributeDescription = string.Empty;
        private bool newAttributeFavorite = false;
        private string newSkillName = string.Empty;
        private int newSkillValue = 0;
        private string newSkillDescription = string.Empty;
        private Skill newSkill = new();
        private string newAbilityName = string.Empty;
        private int newAbilityValue = 0;
        private string newAbilityDescription = string.Empty;
        private Ability newAbility = new();
        private string selectedAttribute = string.Empty;
        private string selectedSkill = string.Empty;

        private string newResourceName = "";
        private int newResourceMaxValue = 100;
        private int newResourceType = 0;
        private bool newResourceIsRollable = false;
        private bool newResourceShowInGroup = true;

        private string newCharBuffName = "";
        private int newCharBuffDuration = 3;
        private string newCharBuffTargetStat = "";
        private int newCharBuffValue = 1;
        private bool newCharBuffIsDebuff = false;
        private string newCharBuffDesc = "";

        private bool advantageRoll = false;
        private bool disadvantageRoll = false;

        private readonly Plugin plugin;
        public bool detailedRoll = false;
        private readonly Configuration configuration;

        public CharStatsWindow(Plugin _plugin)
        {
            plugin = _plugin;
            configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public void DrawCharStats()
        {
            detailedRoll = configuration.detailedRolls;

            if (CharacterManager.Instance.CharacterSheet != null)
            {
                currentCharacter = CharacterManager.Instance.CharacterSheet;
                if (!DiceSystemManager.Instance.IsSessionRulesetActive && !string.IsNullOrWhiteSpace(currentCharacter.linkedDiceSystem))
                {
                    var activeSys = DiceSystemManager.Instance.CurrentDiceSystem;
                    if (activeSys == null || !string.Equals(activeSys.systemName, currentCharacter.linkedDiceSystem, StringComparison.OrdinalIgnoreCase))
                    {
                        var linkedSys = DiceSystem.LoadDiceSystem(currentCharacter.linkedDiceSystem);
                        if (linkedSys != null)
                        {
                            DiceSystemManager.Instance.SwitchDiceSystem(linkedSys);
                        }
                    }
                }
            }
            if (DiceSystemManager.Instance.CurrentDiceSystem != null)
            {
                currentDiceSystem = DiceSystemManager.Instance.CurrentDiceSystem;
                diceType = Enum.GetName<DiceType>(DiceSystemManager.Instance.CurrentDiceSystem.DiceType) ?? "";
            }

            if (currentCharacter == null)
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("NoCharLoadedStatsMessage"));
                return;
            }

            DrawVitalsBanner();
            ImGui.Spacing();
            DrawResourcesSection();
            ImGui.Spacing();
            DrawActiveBuffsBanner();
            ImGui.Spacing();
            DrawColumnsSection();
            DrawModals();
        }

        private void DrawActiveBuffsBanner()
        {
            if (currentCharacter == null) return;
            var buffs = currentCharacter.ActiveBuffs;

            using (var buffBar = ImRaii.Child("##ActiveBuffsBanner", new Vector2(0, 32.0f * ImGuiHelpers.GlobalScale), true))
            {
                if (buffBar.Success)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Magic.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.ParsedGold, $"{LocalizationManager.Instance.GetLocalizedString("BuffsHeader")}:");

                    string? buffToRemove = null;
                    if (buffs != null && buffs.Count > 0)
                    {
                        for (int i = 0; i < buffs.Count; i++)
                        {
                            var buff = buffs[i];
                            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                            string badgeText = $"{buff.Name} ({buff.Duration}t)";
                            var badgeBg = buff.IsDebuff ? new Vector4(0.35f, 0.12f, 0.12f, 0.85f) : new Vector4(0.12f, 0.30f, 0.16f, 0.85f);
                            var badgeCol = buff.IsDebuff ? ImGuiColors.DalamudRed : ImGuiColors.ParsedGreen;

                            UiUtils.Badge(badgeText, badgeBg, badgeCol);
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.TextColored(badgeCol, $"{buff.Name} {(buff.IsDebuff ? "[Debuff]" : "[Buff]")}");
                                ImGui.Separator();
                                ImGui.Text(string.Format(LocalizationManager.Instance.GetLocalizedString("BuffDurationRemaining"), buff.Duration));
                                if (!string.IsNullOrWhiteSpace(buff.Description))
                                {
                                    ImGui.TextDisabled(buff.Description);
                                }
                                string mods = buff.GetFormattedModifiers();
                                if (!string.IsNullOrWhiteSpace(mods))
                                {
                                    ImGui.TextColored(ImGuiColors.ParsedGold, $"{LocalizationManager.Instance.GetLocalizedString("StatModifiersLabel")} {mods}");
                                }
                                ImGui.Separator();
                                ImGui.TextDisabled("Right click to manage");
                                ImGui.EndTooltip();
                            }

                            if (ImGui.BeginPopupContextItem($"CharBuffCtx_{buff.Id}"))
                            {
                                ImGui.TextColored(badgeCol, buff.Name);
                                ImGui.Separator();
                                if (ImGui.MenuItem("+1 Turn"))
                                {
                                    buff.Duration++;
                                    currentCharacter.SyncWithInitiativeTracker();
                                }
                                if (ImGui.MenuItem("-1 Turn"))
                                {
                                    if (buff.Tick(1))
                                    {
                                        buffToRemove = buff.Id;
                                    }
                                    else
                                    {
                                        currentCharacter.SyncWithInitiativeTracker();
                                    }
                                }
                                ImGui.Separator();
                                if (ImGui.MenuItem(LocalizationManager.Instance.GetLocalizedString("SupprButton") == "-" ? "Remove" : LocalizationManager.Instance.GetLocalizedString("SupprButton")))
                                {
                                    buffToRemove = buff.Id;
                                }
                                ImGui.EndPopup();
                            }
                        }
                    }
                    else
                    {
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoActiveBuffs"));
                    }

                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton("AddCharBuffBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddBuffButton"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
                    {
                        newCharBuffName = "";
                        newCharBuffDuration = 3;
                        newCharBuffTargetStat = "";
                        newCharBuffValue = 1;
                        newCharBuffIsDebuff = false;
                        newCharBuffDesc = "";
                        showBuffPopup = true;
                    }

                    if (buffToRemove != null)
                    {
                        currentCharacter.RemoveBuff(buffToRemove);
                    }
                }
            }
        }

        private void DrawVitalsBanner()
        {
            if (currentCharacter == null) return;

            using (var banner = ImRaii.Child("##VitalsBanner", new Vector2(0, 36.0f * ImGuiHelpers.GlobalScale), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (banner.Success)
                {
                    // Class, Level, XP, System Dice, Linked System
                    if (currentDiceSystem == null || currentDiceSystem.systemHasClasses)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("ClassLabel"));
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.ManageInputField(ref currentCharacter.characterClass, "ClassInput", editingStats, 80.0f);
                        ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                    }

                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("LevelLabel"));
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.ManageInputField(ref currentCharacter.characterLevel, "LevelInput", editingStats, 40.0f);

                    ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("XPLabel"));
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.ManageInputField(ref currentCharacter.characterExperiencePoints, "XpInput", editingStats, 50.0f);

                    if (!string.IsNullOrEmpty(diceType))
                    {
                        ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.Badge(string.Format(LocalizationManager.Instance.GetLocalizedString("SystemDiceBadgeFormat"), diceType), new Vector4(0.3f, 0.2f, 0.5f, 0.4f), ImGuiColors.DalamudViolet);
                    }

                    if (editingStats)
                    {
                        ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                        ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("DiceSysLinkedLabel"));
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.ManageInputField(ref currentCharacter.linkedDiceSystem, "LinkedDiceSystemInput", editingStats, 100.0f);
                    }
                    else if (!string.IsNullOrWhiteSpace(currentCharacter.linkedDiceSystem))
                    {
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        UiUtils.Badge(currentCharacter.linkedDiceSystem, new Vector4(0.2f, 0.35f, 0.5f, 0.4f), ImGuiColors.ParsedBlue);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip($"{LocalizationManager.Instance.GetLocalizedString("DiceSysLinkedLabel")} {currentCharacter.linkedDiceSystem}");
                        }
                    }

                    // Initiative Quick Roll if configured
                    if (currentDiceSystem != null && currentDiceSystem.InitiativeStatType != InitiativeStatType.None)
                    {
                        int initMod = currentCharacter.GetInitiativeModifier(currentDiceSystem);
                        ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                        string initLabel = $"{LocalizationManager.Instance.GetLocalizedString("InitiativeTab")}: {FormatModifier(initMod)}";
                        if (UiUtils.IconButton("RollInitStatsBtn", FontAwesomeIcon.Stopwatch, initLabel))
                        {
                            var roll = currentCharacter.RollInitiative(currentDiceSystem, advantageRoll, disadvantageRoll, detailedRoll);
                            InitiativeTrackerManager.Instance.AddOrUpdateCurrentCharacter(currentCharacter, currentDiceSystem, roll.RollResult, initMod);
                        }
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip($"{LocalizationManager.Instance.GetLocalizedString("InitiativeRollInitiative")} ({currentDiceSystem.InitiativeStatName})");
                        }
                    }

                    // Advantage / Disadvantage toggles if enabled
                    if (currentDiceSystem?.systemHasAdvantageDisadvantage == true)
                    {
                        var advLabel = LocalizationManager.Instance.GetLocalizedString("AdvantageRollCheckbox");
                        var disadvLabel = LocalizationManager.Instance.GetLocalizedString("DisadvantageRollCheckbox");

                        ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
                        if (ImGui.Checkbox($"{advLabel}###AdvCheck", ref advantageRoll))
                        {
                            if (advantageRoll) disadvantageRoll = false;
                        }
                        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                        if (ImGui.Checkbox($"{disadvLabel}###DisadvCheck", ref disadvantageRoll))
                        {
                            if (disadvantageRoll) advantageRoll = false;
                        }
                    }

                    // Right side: Edit Stats checkbox & Save button
                    var saveLabel = LocalizationManager.Instance.GetLocalizedString("SaveStatButton");
                    var editLabel = LocalizationManager.Instance.GetLocalizedString("EditStatCheckbox");
                    var editWidth = ImGui.CalcTextSize(editLabel).X + 30.0f * ImGuiHelpers.GlobalScale;
                    var saveWidth = 28.0f * ImGuiHelpers.GlobalScale;
                    var totalRightWidth = editWidth + saveWidth + 10.0f * ImGuiHelpers.GlobalScale;
                    var rightX = ImGui.GetWindowContentRegionMax().X - totalRightWidth;

                    if (ImGui.GetCursorPosX() < rightX)
                        ImGui.SameLine(rightX);
                    else
                        ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);

                    ImGui.Checkbox($"{editLabel}###EditStatCheck", ref editingStats);
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    if (UiUtils.IconButton("SaveStatBtn", FontAwesomeIcon.Save, saveLabel))
                    {
                        CharacterSheet.SaveSheet(currentCharacter);
                    }
                }
            }
        }

        private void DrawResourcesSection()
        {
            if (currentCharacter == null) return;
            var resources = currentCharacter.GetEffectiveResources(currentDiceSystem);
            if (resources.Count == 0 && !editingStats) return;

            var title = LocalizationManager.Instance.GetLocalizedString("ResourcesSectionTitle");
            if (string.IsNullOrEmpty(title) || title == "ResourcesSectionTitle")
                title = LocalizationManager.Instance.GetLocalizedString("DiceSysResourcesHeader");

            if (UiUtils.StyledCollapsingHeader(title.Replace(":", "").Trim(), defaultOpen: true, icon: FontAwesomeIcon.Heartbeat, accentColor: ImGuiColors.ParsedGreen))
            {
                var scale = ImGuiHelpers.GlobalScale;
                if (editingStats)
                {
                    if (UiUtils.IconTextButton("AddNewResBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("DiceSysAddResourceBtn")))
                    {
                        newResourceName = "";
                        newResourceMaxValue = 100;
                        newResourceType = 0;
                        newResourceIsRollable = false;
                        newResourceShowInGroup = true;
                        showResourcePopup = true;
                    }
                    ImGui.Spacing();
                }

                if (resources.Count == 0)
                {
                    ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("DiceSysNoResources"));
                    return;
                }

                string? resToRemove = null;
                string? resToMoveUp = null;
                string? resToMoveDown = null;

                for (int i = 0; i < resources.Count; i++)
                {
                    var res = resources[i];
                    DrawResourceListItem(res, i, resources.Count, ref resToMoveUp, ref resToMoveDown, ref resToRemove);
                    if (i < resources.Count - 1)
                    {
                        ImGui.Spacing();
                    }
                }

                if (resToMoveUp != null)
                {
                    currentCharacter.MoveResource(resToMoveUp, -1);
                    currentDiceSystem?.MoveResource(resToMoveUp, -1);
                }
                if (resToMoveDown != null)
                {
                    currentCharacter.MoveResource(resToMoveDown, 1);
                    currentDiceSystem?.MoveResource(resToMoveDown, 1);
                }
                if (resToRemove != null)
                {
                    currentCharacter.RemoveResource(resToRemove);
                    currentDiceSystem?.RemoveResource(resToRemove);
                }
            }
        }

        private void DrawResourceListItem(CharacterResource res, int index, int totalCount, ref string? resToMoveUp, ref string? resToMoveDown, ref string? resToRemove)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var def = currentDiceSystem?.SystemResources.FirstOrDefault(d => string.Equals(d.Name, res.Name, StringComparison.OrdinalIgnoreCase));
            var resCol = GetResourceColor(res.Name, def?.ColorHex);
            string effectiveFormula = !string.IsNullOrWhiteSpace(res.Formula) ? res.Formula : (def?.Formula ?? string.Empty);

            int effectiveMax = currentCharacter!.GetEffectiveResourceMax(res.Name, currentDiceSystem);
            int gearBonus = currentCharacter.GetGearStatBonus(res.Name) + currentCharacter.GetGearStatBonus($"Max {res.Name}") + currentCharacter.GetGearStatBonus($"Max{res.Name}");
            int buffBonus = currentCharacter.GetBuffStatBonus(res.Name) + currentCharacter.GetBuffStatBonus($"Max {res.Name}") + currentCharacter.GetBuffStatBonus($"Max{res.Name}");

            float rowHeight = (editingStats ? 48.0f : 42.0f) * scale;

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.11f, 0.12f, 0.15f, 0.90f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(resCol.X, resCol.Y, resCol.Z, 0.45f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 6.0f) * scale))
            using (var card = ImRaii.Child($"##ResRow_{res.Name}_{index}", new Vector2(0, rowHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (card.Success)
                {
                    var drawList = ImGui.GetWindowDrawList();
                    var cardPos = ImGui.GetWindowPos();
                    var cardSize = ImGui.GetWindowSize();

                    // Left color accent stripe
                    drawList.AddRectFilled(
                        cardPos + new Vector2(2.0f * scale, 4.0f * scale),
                        cardPos + new Vector2(5.0f * scale, cardSize.Y - 4.0f * scale),
                        ImGui.ColorConvertFloat4ToU32(resCol),
                        1.5f * scale);

                    // Name and Type
                    ImGui.AlignTextToFramePadding();
                    ImGui.TextColored(resCol, res.Name);

                    ImGui.SameLine(0, 10.0f * scale);

                    if (editingStats)
                    {
                        // Editing controls
                        if (res.ResourceType == ResourceType.FlatNumber)
                        {
                            float inputW = 70.0f * scale;
                            int val = res.MaxValue > 0 ? res.MaxValue : res.CurrentValue;
                            if (UiUtils.StyledInputInt($"ResVal_{res.Name}", ref val, step: 0, width: inputW / scale))
                            {
                                currentCharacter.SetResourceMax(res.Name, val);
                                currentCharacter.SetResourceCurrent(res.Name, val);
                            }

                            ImGui.SameLine(0, 6.0f * scale);
                            var rollableIconCol = res.IsRollable ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudGrey;
                            var rollableTooltip = res.IsRollable
                                ? LocalizationManager.Instance.GetLocalizedString("ResourceRollableEnabledTooltip")
                                : LocalizationManager.Instance.GetLocalizedString("ResourceRollableDisabledTooltip");

                            ImGui.PushStyleColor(ImGuiCol.Text, rollableIconCol);
                            if (UiUtils.IconButton($"ToggleRoll_{res.Name}", FontAwesomeIcon.DiceD20, rollableTooltip, new Vector2(24, 22) * scale))
                            {
                                res.IsRollable = !res.IsRollable;
                            }
                            ImGui.PopStyleColor();

                            ImGui.SameLine(0, 6.0f * scale);
                            var groupIconCol = res.ShowInGroup ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudGrey;
                            var groupTooltip = res.ShowInGroup
                                ? LocalizationManager.Instance.GetLocalizedString("ResourceShowInGroupTooltip")
                                : LocalizationManager.Instance.GetLocalizedString("ResourceHideFromGroupTooltip");

                            ImGui.PushStyleColor(ImGuiCol.Text, groupIconCol);
                            if (UiUtils.IconButton($"ToggleGroup_{res.Name}", FontAwesomeIcon.Users, groupTooltip, new Vector2(24, 22) * scale))
                            {
                                res.ShowInGroup = !res.ShowInGroup;
                                PartySyncManager.Instance.BroadcastResourceUpdate();
                            }
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            float inputW = 55.0f * scale;
                            int curVal = res.CurrentValue;
                            if (UiUtils.StyledInputInt($"ResCur_{res.Name}", ref curVal, step: 0, width: inputW / scale))
                            {
                                currentCharacter.SetResourceCurrent(res.Name, curVal);
                            }
                            ImGui.SameLine(0, 4.0f * scale);
                            ImGui.TextDisabled("/");
                            ImGui.SameLine(0, 4.0f * scale);
                            int maxVal = res.MaxValue;
                            if (UiUtils.StyledInputInt($"ResMax_{res.Name}", ref maxVal, step: 0, width: inputW / scale))
                            {
                                currentCharacter.SetResourceMax(res.Name, maxVal);
                            }

                            ImGui.SameLine(0, 6.0f * scale);
                            var groupIconCol = res.ShowInGroup ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudGrey;
                            var groupTooltip = res.ShowInGroup
                                ? LocalizationManager.Instance.GetLocalizedString("ResourceShowInGroupTooltip")
                                : LocalizationManager.Instance.GetLocalizedString("ResourceHideFromGroupTooltip");

                            ImGui.PushStyleColor(ImGuiCol.Text, groupIconCol);
                            if (UiUtils.IconButton($"ToggleGroup_{res.Name}", FontAwesomeIcon.Users, groupTooltip, new Vector2(24, 22) * scale))
                            {
                                res.ShowInGroup = !res.ShowInGroup;
                                PartySyncManager.Instance.BroadcastResourceUpdate();
                            }
                            ImGui.PopStyleColor();
                        }

                        // Right action buttons (recalc, move up/down, delete)
                        float btnsWidth = 26.0f * scale; // Trash
                        if (index > 0) btnsWidth += 24.0f * scale;
                        if (index < totalCount - 1) btnsWidth += 24.0f * scale;
                        if (!string.IsNullOrWhiteSpace(effectiveFormula)) btnsWidth += 24.0f * scale;

                        var rightBtnX = cardSize.X - btnsWidth - 10.0f * scale;
                        if (ImGui.GetCursorPosX() < rightBtnX)
                            ImGui.SameLine(rightBtnX);
                        else
                            ImGui.SameLine(0, 4.0f * scale);

                        if (index > 0)
                        {
                            if (UiUtils.IconButton($"MoveUpRes_{res.Name}", FontAwesomeIcon.ArrowUp, LocalizationManager.Instance.GetLocalizedString("MoveUpTooltip"), new Vector2(22, 22) * scale))
                            {
                                resToMoveUp = res.Name;
                            }
                            ImGui.SameLine(0, 2.0f * scale);
                        }

                        if (index < totalCount - 1)
                        {
                            if (UiUtils.IconButton($"MoveDownRes_{res.Name}", FontAwesomeIcon.ArrowDown, LocalizationManager.Instance.GetLocalizedString("MoveDownTooltip"), new Vector2(22, 22) * scale))
                            {
                                resToMoveDown = res.Name;
                            }
                            ImGui.SameLine(0, 2.0f * scale);
                        }

                        if (!string.IsNullOrWhiteSpace(effectiveFormula))
                        {
                            if (UiUtils.IconButton($"RecalcRes_{res.Name}", FontAwesomeIcon.Sync, LocalizationManager.Instance.GetLocalizedString("RecalculateResourcesBtn"), new Vector2(22, 22) * scale))
                            {
                                currentCharacter.RecalculateResourceMax(res.Name, currentDiceSystem);
                            }
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.SetTooltip($"{LocalizationManager.Instance.GetLocalizedString("RecalculateResourcesTooltip")}\n({effectiveFormula})");
                            }
                            ImGui.SameLine(0, 2.0f * scale);
                        }

                        if (UiUtils.IconButton($"DelRes_{res.Name}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(22, 22) * scale))
                        {
                            resToRemove = res.Name;
                        }
                    }
                    else
                    {
                        // Viewing mode
                        if (res.ResourceType == ResourceType.FlatNumber)
                        {
                            string valText = $"{effectiveMax}{(gearBonus != 0 ? $" ({FormatModifier(gearBonus)})" : "")}";
                            UiUtils.PillBadge(valText, new Vector4(0.24f, 0.20f, 0.12f, 0.85f), ImGuiColors.ParsedGold);

                            if (res.IsRollable)
                            {
                                ImGui.SameLine(0, 8.0f * scale);
                                if (UiUtils.IconButton($"RollRes_{res.Name}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {res.Name}", new Vector2(24, 22) * scale))
                                {
                                    currentCharacter.RollResource(res.Name, currentDiceSystem, advantageRoll, disadvantageRoll, detailedRoll);
                                }
                            }
                        }
                        else if (res.ResourceType == ResourceType.Counter)
                        {
                            float btnSize = 22.0f * scale;
                            if (UiUtils.IconButton($"ResDec_{res.Name}", FontAwesomeIcon.Minus, "-", new Vector2(btnSize, btnSize)))
                            {
                                if (res.CurrentValue > 0)
                                {
                                    currentCharacter.SetResourceCurrent(res.Name, res.CurrentValue - 1);
                                }
                            }

                            ImGui.SameLine(0, 6.0f * scale);
                            float barW = Math.Max(80.0f * scale, ImGui.GetContentRegionAvail().X - btnSize - 8.0f * scale);

                            string overlay = effectiveMax > 0
                                ? $"{res.CurrentValue} / {effectiveMax}{(gearBonus != 0 ? $" ({FormatModifier(gearBonus)})" : "")}"
                                : $"{res.CurrentValue}";

                            UiUtils.DrawSectionedBar(
                                res.CurrentValue,
                                effectiveMax > 0 ? effectiveMax : 1,
                                overlay,
                                new Vector2(barW, 20.0f * scale),
                                activeColor: resCol,
                                onSegmentClick: newVal => currentCharacter.SetResourceCurrent(res.Name, newVal));

                            ImGui.SameLine(0, 6.0f * scale);
                            if (UiUtils.IconButton($"ResInc_{res.Name}", FontAwesomeIcon.Plus, "+", new Vector2(btnSize, btnSize)))
                            {
                                if (res.CurrentValue < effectiveMax)
                                {
                                    currentCharacter.SetResourceCurrent(res.Name, res.CurrentValue + 1);
                                }
                            }
                        }
                        else // ResourceType.Bar
                        {
                            string overlay = effectiveMax > 0
                                ? $"{res.CurrentValue} / {effectiveMax}{(gearBonus != 0 ? $" ({FormatModifier(gearBonus)})" : "")}"
                                : $"{res.CurrentValue}";
                            UiUtils.DrawProgressBar(res.CurrentValue, effectiveMax > 0 ? effectiveMax : 1, overlay, new Vector2(-1, 20.0f * scale), resCol);
                        }
                    }
                }
            }

            // Hover tooltip on the card
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                string typeSuffix = res.ResourceType switch
                {
                    ResourceType.Counter => $" ({LocalizationManager.Instance.GetLocalizedString("ResourceTypeCounter")})",
                    ResourceType.FlatNumber => $" ({LocalizationManager.Instance.GetLocalizedString("ResourceTypeFlatNumber")})",
                    _ => ""
                };
                ImGui.TextColored(resCol, $"{res.Name}{typeSuffix}");
                ImGui.Separator();

                if (res.ResourceType == ResourceType.FlatNumber)
                {
                    if (!string.IsNullOrWhiteSpace(effectiveFormula))
                        ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaHeader")}: {effectiveFormula}");
                    ImGui.Text($"• Base Value: {res.MaxValue}");
                    if (res.TempBonus != 0) ImGui.Text($"• Temp Bonus: {FormatModifier(res.TempBonus)}");
                    if (gearBonus != 0) ImGui.TextColored(ImGuiColors.ParsedBlue, $"• Gear Bonus: {FormatModifier(gearBonus)}");
                    if (buffBonus != 0) ImGui.TextColored(ImGuiColors.ParsedGreen, $"• Buff/Debuff: {FormatModifier(buffBonus)}");
                    int resFeatBonus = currentCharacter.GetFeatStatBonus(res.Name) + currentCharacter.GetFeatStatBonus($"Max {res.Name}") + currentCharacter.GetFeatStatBonus($"Max{res.Name}");
                    if (resFeatBonus != 0) ImGui.TextColored(ImGuiColors.ParsedPurple, $"• Feat Bonus: {FormatModifier(resFeatBonus)}");
                    ImGui.TextColored(ImGuiColors.ParsedGreen, $"• Effective: {effectiveMax}");
                    ImGui.Separator();
                    ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {res.Name}");
                }
                else
                {
                    ImGui.Text($"• Current: {res.CurrentValue}");
                    if (!string.IsNullOrWhiteSpace(effectiveFormula))
                        ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaHeader")}: {effectiveFormula}");
                    ImGui.Text($"• Base Max: {res.MaxValue}");
                    if (res.TempBonus != 0) ImGui.Text($"• Temp Max: {FormatModifier(res.TempBonus)}");
                    if (gearBonus != 0) ImGui.TextColored(ImGuiColors.ParsedBlue, $"• Gear Bonus: {FormatModifier(gearBonus)}");
                    if (buffBonus != 0) ImGui.TextColored(ImGuiColors.ParsedGreen, $"• Buff/Debuff: {FormatModifier(buffBonus)}");
                    int resFeatBonus2 = currentCharacter.GetFeatStatBonus(res.Name) + currentCharacter.GetFeatStatBonus($"Max {res.Name}") + currentCharacter.GetFeatStatBonus($"Max{res.Name}");
                    if (resFeatBonus2 != 0) ImGui.TextColored(ImGuiColors.ParsedPurple, $"• Feat Bonus: {FormatModifier(resFeatBonus2)}");
                    ImGui.TextColored(ImGuiColors.ParsedGreen, $"• Effective Max: {effectiveMax}");
                }
                ImGui.EndTooltip();
            }
        }

        private void DrawColumnsSection()
        {
            if (currentCharacter == null) return;

            var availHeight = Math.Max(240.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - 4.0f);

            using (var table = ImRaii.Table("##StatsColumnsGrid", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.BordersInnerV))
            {
                if (table.Success)
                {
                    ImGui.TableNextColumn();
                    DrawAttributesColumn(availHeight);

                    ImGui.TableNextColumn();
                    DrawSkillsColumn(availHeight);
                }
            }
        }

        private static Vector4 GetResourceColor(string name, string? colorHex = null)
        {
            return UiUtils.GetResourceColor(name, colorHex);
        }

        private static string FormatModifier(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }

        private void DrawAttributesColumn(float height)
        {
            if (currentCharacter == null) return;

            using (var child = ImRaii.Child("##AttributesColChild", new Vector2(0, height), true))
            {
                if (child.Success)
                {
                    // Column Header
                    var effectiveAttributes = currentCharacter.GetEffectiveAttributes(currentDiceSystem);

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.ShieldAlt.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("AttributeLabel"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge((effectiveAttributes?.Count ?? 0).ToString(), new Vector4(0.35f, 0.28f, 0.12f, 0.5f), ImGuiColors.ParsedGold);

                    ImGui.Separator();
                    ImGui.Spacing();

                    if (effectiveAttributes == null || effectiveAttributes.Count == 0)
                    {
                        ImGui.Spacing();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.DalamudGrey, FontAwesomeIcon.InfoCircle.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAttributesDefined"));
                    }
                    else
                    {
                        var attrList = effectiveAttributes.ToList();
                        string? attrToMoveUp = null;
                        string? attrToMoveDown = null;
                        var availWidth = ImGui.GetContentRegionAvail().X;

                        for (int i = 0; i < attrList.Count; i++)
                        {
                            var attribute = attrList[i];
                            ImGui.PushID($"AttrCard_{attribute.Key}");

                            var pos = ImGui.GetCursorScreenPos();
                            var cardHeight = (editingStats ? 36.0f : 34.0f) * ImGuiHelpers.GlobalScale;
                            var cardSize = new Vector2(availWidth, cardHeight);

                            var drawList = ImGui.GetWindowDrawList();
                            bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + cardSize);
                            var bgCol = isHovered
                                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.22f, 0.28f, 0.70f))
                                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.14f, 0.18f, 0.55f));
                            var borderCol = isHovered
                                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.85f, 0.75f, 0.35f, 0.60f))
                                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.28f, 0.35f, 0.40f));

                            drawList.AddRectFilled(pos, pos + cardSize, bgCol, 4.0f * ImGuiHelpers.GlobalScale);
                            drawList.AddRect(pos, pos + cardSize, borderCol, 4.0f * ImGuiHelpers.GlobalScale, ImDrawFlags.None, isHovered ? 1.5f : 1.0f);

                            ImGui.SetCursorScreenPos(pos + new Vector2(6.0f, (cardHeight - 22.0f * ImGuiHelpers.GlobalScale) * 0.5f));

                            bool hasBonusTemp = currentDiceSystem == null || currentDiceSystem.systemHasBonusTemp;
                            bool hasBonusPerm = currentDiceSystem == null || currentDiceSystem.systemHasBonusPerm;
                            bool showEpic = currentDiceSystem != null ? currentDiceSystem.systemHasEpicAttributes : configuration.showEpicBonus;
                            bool hasSaves = currentDiceSystem == null || currentDiceSystem.systemHasSaves;
                            bool hasFavoriteAttributes = currentDiceSystem == null || currentDiceSystem.systemHasFavoriteAttributes;

                            if (editingStats)
                            {
                                if (i > 0)
                                {
                                    if (UiUtils.IconButton($"MoveUp_{attribute.Key}", FontAwesomeIcon.ChevronUp, LocalizationManager.Instance.GetLocalizedString("MoveUpTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                    {
                                        attrToMoveUp = attribute.Key;
                                    }
                                    ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                                }
                                if (i < attrList.Count - 1)
                                {
                                    if (UiUtils.IconButton($"MoveDown_{attribute.Key}", FontAwesomeIcon.ChevronDown, LocalizationManager.Instance.GetLocalizedString("MoveDownTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                    {
                                        attrToMoveDown = attribute.Key;
                                    }
                                    ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                                }

                                if (hasFavoriteAttributes)
                                {
                                    var favIconCol = attribute.Value.IsFavorite ? ImGuiColors.ParsedGold : ImGuiColors.DalamudGrey;
                                    if (UiUtils.IconButton($"Fav_{attribute.Key}", FontAwesomeIcon.Star, LocalizationManager.Instance.GetLocalizedString("FavoriteAttributeTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale, customColor: favIconCol))
                                    {
                                        attribute.Value.IsFavorite = !attribute.Value.IsFavorite;
                                    }
                                    ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                                }
                                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);

                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, attribute.Key);

                                int inputCount = 1 + (hasBonusTemp ? 1 : 0) + (hasBonusPerm ? 1 : 0) + (showEpic ? 1 : 0);
                                var inputAreaWidth = (inputCount * 40.0f + 10.0f) * ImGuiHelpers.GlobalScale;
                                var rightInputX = pos.X + availWidth - inputAreaWidth;
                                if (ImGui.GetCursorScreenPos().X < rightInputX)
                                {
                                    ImGui.SetCursorScreenPos(new Vector2(rightInputX, pos.Y + (cardHeight - 22.0f * ImGuiHelpers.GlobalScale) * 0.5f));
                                }

                                UiUtils.StyledInputInt($"Val_{attribute.Key}", ref attribute.Value.Value, step: 0, width: 36.0f);
                                if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("StatValueTooltip"));

                                if (hasBonusTemp)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.StyledInputInt($"Temp_{attribute.Key}", ref attribute.Value.TempBonus, step: 0, width: 36.0f);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("StatTempTooltip"));
                                }

                                if (hasBonusPerm)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.StyledInputInt($"Perm_{attribute.Key}", ref attribute.Value.PermBonus, step: 0, width: 36.0f);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("StatPermTooltip"));
                                }

                                if (showEpic)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.StyledInputInt($"Epic_{attribute.Key}", ref attribute.Value.EpicBonus, step: 0, width: 36.0f);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("StatEpicTooltip"));
                                }
                            }
                            else
                            {
                                if (hasFavoriteAttributes && attribute.Value.IsFavorite)
                                {
                                    ImGui.PushFont(UiBuilder.IconFont);
                                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Star.ToIconString());
                                    ImGui.PopFont();
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("FavoriteAttributeTooltip"));
                                    }
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                }

                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, attribute.Key);

                                int baseVal = attribute.Value.Value;
                                int tempVal = hasBonusTemp ? attribute.Value.TempBonus : 0;
                                int permVal = hasBonusPerm ? attribute.Value.PermBonus : 0;
                                int gearBonus = currentCharacter.GetGearStatBonus(attribute.Key);
                                int buffBonus = currentCharacter.GetBuffStatBonus(attribute.Key);
                                int featBonus = currentCharacter.GetFeatStatBonus(attribute.Key);
                                int epicVal = showEpic ? attribute.Value.EpicBonus : 0;
                                int totalVal = baseVal + tempVal + permVal + gearBonus + buffBonus + featBonus;

                                float rightItemsWidth = (hasSaves ? 58.0f : 30.0f) * ImGuiHelpers.GlobalScale;
                                string baseText = baseVal.ToString();
                                rightItemsWidth += ImGui.CalcTextSize(baseText).X + 16.0f * ImGuiHelpers.GlobalScale;

                                if (tempVal != 0)
                                {
                                    string tempText = FormatModifier(tempVal);
                                    rightItemsWidth += ImGui.CalcTextSize(tempText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }
                                if (permVal != 0)
                                {
                                    string permText = FormatModifier(permVal);
                                    rightItemsWidth += ImGui.CalcTextSize(permText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }
                                if (gearBonus != 0)
                                {
                                    string gearText = FormatModifier(gearBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(gearText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }
                                if (buffBonus != 0)
                                {
                                    string buffText = FormatModifier(buffBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(buffText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }
                                if (featBonus != 0)
                                {
                                    string featText = FormatModifier(featBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(featText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }
                                if (epicVal > 0)
                                {
                                    string epicText = $"★{epicVal}";
                                    rightItemsWidth += ImGui.CalcTextSize(epicText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                var rightStartX = pos.X + availWidth - rightItemsWidth - 6.0f * ImGuiHelpers.GlobalScale;
                                if (ImGui.GetCursorScreenPos().X < rightStartX)
                                {
                                    ImGui.SetCursorScreenPos(new Vector2(rightStartX, pos.Y + (cardHeight - 20.0f * ImGuiHelpers.GlobalScale) * 0.5f));
                                }

                                UiUtils.Badge(baseText, new Vector4(0.24f, 0.20f, 0.12f, 0.85f), ImGuiColors.ParsedGold);
                                if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("StatValueTooltip")}: {baseVal}");

                                if (tempVal != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var tempCol = tempVal > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                                    var tempBg = tempVal > 0 ? new Vector4(0.12f, 0.30f, 0.16f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(tempVal), tempBg, tempCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("StatTempTooltip")}: {FormatModifier(tempVal)}");
                                }

                                if (permVal != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var permCol = permVal > 0 ? new Vector4(0.2f, 0.85f, 0.85f, 1.0f) : ImGuiColors.DalamudRed;
                                    var permBg = permVal > 0 ? new Vector4(0.12f, 0.28f, 0.32f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(permVal), permBg, permCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("StatPermTooltip")}: {FormatModifier(permVal)}");
                                }

                                if (gearBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var gearCol = gearBonus > 0 ? ImGuiColors.ParsedBlue : ImGuiColors.DalamudRed;
                                    var gearBg = gearBonus > 0 ? new Vector4(0.12f, 0.22f, 0.38f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(gearBonus), gearBg, gearCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(gearBonus)}");
                                }

                                if (buffBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var buffCol = buffBonus > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                                    var buffBg = buffBonus > 0 ? new Vector4(0.12f, 0.30f, 0.16f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(buffBonus), buffBg, buffCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"Buff / Debuff: {FormatModifier(buffBonus)}");
                                }

                                if (featBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var featCol = featBonus > 0 ? ImGuiColors.ParsedPurple : ImGuiColors.DalamudRed;
                                    var featBg = featBonus > 0 ? new Vector4(0.24f, 0.12f, 0.32f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(featBonus), featBg, featCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(featBonus)}");
                                }

                                if (epicVal > 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge($"★{epicVal}", new Vector4(0.30f, 0.15f, 0.40f, 0.85f), ImGuiColors.DalamudViolet);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("StatEpicTooltip")}: ★{epicVal}");
                                }

                                if (hasSaves)
                                {
                                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                    if (UiUtils.IconButton($"SaveRoll_{attribute.Key}", FontAwesomeIcon.ShieldAlt, $"{LocalizationManager.Instance.GetLocalizedString("SavingThrowButton")} {attribute.Key}", new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                                    {
                                        statRollType = StatRollType.SavingThrow;
                                        statRollName = attribute.Key;
                                        statRollAttribute = attribute.Value;
                                        statRollSkill = null;
                                        rollBonusOrPenalty = 0;
                                        showStatRollModal = true;
                                    }
                                }

                                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"Roll_{attribute.Key}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {attribute.Key}", new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                                {
                                    statRollType = StatRollType.Attribute;
                                    statRollName = attribute.Key;
                                    statRollAttribute = attribute.Value;
                                    statRollSkill = null;
                                    rollBonusOrPenalty = 0;
                                    showStatRollModal = true;
                                }

                                if (isHovered && !ImGui.IsAnyItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.TextColored(ImGuiColors.ParsedGold, attribute.Key);
                                    if (!string.IsNullOrWhiteSpace(attribute.Value.Description))
                                    {
                                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                                        ImGui.TextWrapped(attribute.Value.Description);
                                        ImGui.PopStyleColor();
                                    }
                                    ImGui.Separator();
                                    ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("StatValueTooltip")}: {baseVal}");
                                    if (tempVal != 0)
                                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("StatTempTooltip")}: {FormatModifier(tempVal)}");
                                    if (gearBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedBlue, $"{LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(gearBonus)}");
                                    if (featBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedPurple, $"{LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(featBonus)}");
                                    if (showEpic && epicVal > 0)
                                        ImGui.TextColored(ImGuiColors.DalamudViolet, $"{LocalizationManager.Instance.GetLocalizedString("StatEpicTooltip")}: ★{epicVal}");
                                    ImGui.Separator();
                                    ImGui.TextColored(ImGuiColors.ParsedGreen, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")}: {totalVal} {(showEpic && epicVal > 0 ? $"(+★{epicVal})" : "")}");
                                    ImGui.EndTooltip();
                                }
                            }

                            ImGui.PopID();
                            ImGui.SetCursorScreenPos(new Vector2(pos.X, pos.Y + cardHeight + 4.0f * ImGuiHelpers.GlobalScale));
                        }

                        if (attrToMoveUp != null)
                        {
                            currentCharacter.MoveAttribute(attrToMoveUp, -1);
                            currentDiceSystem?.MoveAttribute(attrToMoveUp, -1);
                        }
                        if (attrToMoveDown != null)
                        {
                            currentCharacter.MoveAttribute(attrToMoveDown, 1);
                            currentDiceSystem?.MoveAttribute(attrToMoveDown, 1);
                        }
                    }
                }
            }
        }

        private void DrawSkillsColumn(float height)
        {
            if (currentCharacter == null) return;

            using (var child = ImRaii.Child("##SkillsColChild", new Vector2(0, height), true))
            {
                if (child.Success)
                {
                    // Column Header
                    var effectiveSkills = currentCharacter.GetEffectiveSkills(currentDiceSystem);

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.Book.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("SkillLabel"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge((effectiveSkills?.Count ?? 0).ToString(), new Vector4(0.15f, 0.35f, 0.2f, 0.5f), ImGuiColors.ParsedGreen);

                    ImGui.Separator();
                    ImGui.Spacing();

                    if (effectiveSkills == null || effectiveSkills.Count == 0)
                    {
                        ImGui.Spacing();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.DalamudGrey, FontAwesomeIcon.InfoCircle.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoSkillsDefined"));
                    }
                    else
                    {
                        var skillList = effectiveSkills.ToList();
                        string? skillToMoveUp = null;
                        string? skillToMoveDown = null;
                        var availWidth = ImGui.GetContentRegionAvail().X;
                        var effectiveAttributes = currentCharacter.GetEffectiveAttributes(currentDiceSystem);

                        for (int i = 0; i < skillList.Count; i++)
                        {
                            var skill = skillList[i];
                            ImGui.PushID($"SkillCard_{skill.Key}");

                            var pos = ImGui.GetCursorScreenPos();
                            var cardHeight = (editingStats ? 36.0f : 36.0f) * ImGuiHelpers.GlobalScale;
                            var cardSize = new Vector2(availWidth, cardHeight);

                            var drawList = ImGui.GetWindowDrawList();
                            bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + cardSize);
                            var bgCol = isHovered
                                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.24f, 0.20f, 0.70f))
                                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.16f, 0.14f, 0.55f));
                            var borderCol = isHovered
                                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.75f, 0.45f, 0.60f))
                                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.35f, 0.28f, 0.40f));

                            drawList.AddRectFilled(pos, pos + cardSize, bgCol, 4.0f * ImGuiHelpers.GlobalScale);
                            drawList.AddRect(pos, pos + cardSize, borderCol, 4.0f * ImGuiHelpers.GlobalScale, ImDrawFlags.None, isHovered ? 1.5f : 1.0f);

                            ImGui.SetCursorScreenPos(pos + new Vector2(6.0f, (cardHeight - 22.0f * ImGuiHelpers.GlobalScale) * 0.5f));

                            int attributeValue = 0;
                            int attributeTemp = 0;
                            int attributePerm = 0;
                            int rawSuccesses = 0;
                            Datamodels.Attribute? linkedAttr = null;
                            bool hasLinkedAttr = (currentDiceSystem?.dynamicSkillAttributeLinking != true) &&
                                                 !string.IsNullOrEmpty(skill.Value.linkedAttribute) &&
                                                 currentCharacter.characterAttributes != null &&
                                                 currentCharacter.characterAttributes.TryGetValue(skill.Value.linkedAttribute, out linkedAttr);
                            if (hasLinkedAttr && linkedAttr != null)
                            {
                                attributeValue = linkedAttr.Value;
                                attributeTemp = (currentDiceSystem == null || currentDiceSystem.systemHasBonusTemp) ? linkedAttr.TempBonus : 0;
                                attributePerm = (currentDiceSystem == null || currentDiceSystem.systemHasBonusPerm) ? linkedAttr.PermBonus : 0;
                                rawSuccesses = (currentDiceSystem != null ? currentDiceSystem.systemHasEpicAttributes : configuration.showEpicBonus) ? linkedAttr.EpicBonus : 0;
                            }
                            int skillGearBonus = currentCharacter.GetGearStatBonus(skill.Value.skillName);
                            int skillBuffBonus = currentCharacter.GetBuffStatBonus(skill.Value.skillName);
                            int skillFeatBonus = currentCharacter.GetFeatStatBonus(skill.Value.skillName);
                            int attrGearBonus = hasLinkedAttr ? currentCharacter.GetGearStatBonus(skill.Value.linkedAttribute) : 0;
                            int attrBuffBonus = hasLinkedAttr ? currentCharacter.GetBuffStatBonus(skill.Value.linkedAttribute) : 0;
                            int attrFeatBonus = hasLinkedAttr ? currentCharacter.GetFeatStatBonus(skill.Value.linkedAttribute) : 0;
                            int effectiveAttrVal = attributeValue + attributeTemp + attributePerm + attrGearBonus + attrBuffBonus + attrFeatBonus;
                            int totalModifier = skill.Value.skillModifier + skillGearBonus + skillBuffBonus + skillFeatBonus + (hasLinkedAttr ? effectiveAttrVal : 0);

                            if (editingStats)
                            {
                                if (i > 0)
                                {
                                    if (UiUtils.IconButton($"MoveUpSkill_{skill.Key}", FontAwesomeIcon.ChevronUp, LocalizationManager.Instance.GetLocalizedString("MoveUpTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                    {
                                        skillToMoveUp = skill.Key;
                                    }
                                    ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                                }
                                if (i < skillList.Count - 1)
                                {
                                    if (UiUtils.IconButton($"MoveDownSkill_{skill.Key}", FontAwesomeIcon.ChevronDown, LocalizationManager.Instance.GetLocalizedString("MoveDownTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                    {
                                        skillToMoveDown = skill.Key;
                                    }
                                    ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                                }
                                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);

                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, skill.Value.skillName);
                                if (currentDiceSystem?.dynamicSkillAttributeLinking != true && !string.IsNullOrEmpty(skill.Value.linkedAttribute))
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(skill.Value.linkedAttribute, new Vector4(0.28f, 0.22f, 0.12f, 0.6f), ImGuiColors.ParsedGold);
                                }

                                var rightInputX = pos.X + availWidth - 45.0f * ImGuiHelpers.GlobalScale;
                                if (ImGui.GetCursorScreenPos().X < rightInputX)
                                {
                                    ImGui.SetCursorScreenPos(new Vector2(rightInputX, pos.Y + (cardHeight - 22.0f * ImGuiHelpers.GlobalScale) * 0.5f));
                                }
                                UiUtils.StyledInputInt($"SkillVal_{skill.Key}", ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterSkills, skill.Key).skillModifier, step: 0, width: 36.0f);
                            }
                            else
                            {
                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, skill.Value.skillName);

                                if (currentDiceSystem?.dynamicSkillAttributeLinking != true && !string.IsNullOrEmpty(skill.Value.linkedAttribute))
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(skill.Value.linkedAttribute, new Vector4(0.28f, 0.22f, 0.12f, 0.6f), ImGuiColors.ParsedGold);
                                }

                                float rightItemsWidth = 30.0f * ImGuiHelpers.GlobalScale;
                                string baseModText = FormatModifier(skill.Value.skillModifier);
                                rightItemsWidth += ImGui.CalcTextSize(baseModText).X + 16.0f * ImGuiHelpers.GlobalScale;

                                if (skillGearBonus != 0)
                                {
                                    string gearText = FormatModifier(skillGearBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(gearText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                if (skillBuffBonus != 0)
                                {
                                    string buffText = FormatModifier(skillBuffBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(buffText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                if (skillFeatBonus != 0)
                                {
                                    string featText = FormatModifier(skillFeatBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(featText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                string? totalModText = (hasLinkedAttr || skillGearBonus != 0 || skillBuffBonus != 0 || skillFeatBonus != 0) ? FormatModifier(totalModifier) : null;
                                if (totalModText != null)
                                {
                                    rightItemsWidth += ImGui.CalcTextSize(totalModText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                var rightStartX = pos.X + availWidth - rightItemsWidth - 6.0f * ImGuiHelpers.GlobalScale;
                                if (ImGui.GetCursorScreenPos().X < rightStartX)
                                {
                                    ImGui.SetCursorScreenPos(new Vector2(rightStartX, pos.Y + (cardHeight - 20.0f * ImGuiHelpers.GlobalScale) * 0.5f));
                                }

                                UiUtils.Badge(baseModText, new Vector4(0.18f, 0.22f, 0.20f, 0.85f), ImGuiColors.DalamudGrey);
                                if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("NewSkillValue")}: {baseModText}");

                                if (skillGearBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var gearCol = skillGearBonus > 0 ? ImGuiColors.ParsedBlue : ImGuiColors.DalamudRed;
                                    var gearBg = skillGearBonus > 0 ? new Vector4(0.12f, 0.22f, 0.38f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(skillGearBonus), gearBg, gearCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(skillGearBonus)}");
                                }

                                if (skillBuffBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var buffCol = skillBuffBonus > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                                    var buffBg = skillBuffBonus > 0 ? new Vector4(0.12f, 0.30f, 0.16f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(skillBuffBonus), buffBg, buffCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"Buff / Debuff: {FormatModifier(skillBuffBonus)}");
                                }

                                if (skillFeatBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var featCol = skillFeatBonus > 0 ? ImGuiColors.ParsedPurple : ImGuiColors.DalamudRed;
                                    var featBg = skillFeatBonus > 0 ? new Vector4(0.24f, 0.12f, 0.32f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(skillFeatBonus), featBg, featCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(skillFeatBonus)}");
                                }

                                if (totalModText != null)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(totalModText, new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")}: {totalModText}");
                                }

                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"Roll_{skill.Value.skillName}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {skill.Value.skillName}", new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                                {
                                    statRollType = StatRollType.Skill;
                                    statRollName = skill.Value.skillName;
                                    statRollSkill = skill.Value;
                                    statRollAttribute = null;
                                    if (currentDiceSystem?.dynamicSkillAttributeLinking == true && currentCharacter.characterAttributes != null && currentCharacter.characterAttributes.Count > 0)
                                    {
                                        selectedDynamicAttr = currentCharacter.characterAttributes.Keys.FirstOrDefault() ?? "";
                                    }
                                    else
                                    {
                                        selectedDynamicAttr = skill.Value.linkedAttribute ?? "";
                                    }
                                    rollBonusOrPenalty = 0;
                                    showStatRollModal = true;
                                }

                                if (isHovered && !ImGui.IsAnyItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.TextColored(ImGuiColors.ParsedGreen, skill.Value.skillName);
                                    if (!string.IsNullOrWhiteSpace(skill.Value.skillDescription))
                                    {
                                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                                        ImGui.TextWrapped(skill.Value.skillDescription);
                                        ImGui.PopStyleColor();
                                    }
                                    ImGui.Separator();
                                    ImGui.Text($"• {LocalizationManager.Instance.GetLocalizedString("NewSkillValue")}: {baseModText}");
                                    if (skillGearBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(skillGearBonus)}");
                                    if (skillFeatBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedPurple, $"• {LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(skillFeatBonus)}");
                                    if (hasLinkedAttr)
                                    {
                                        ImGui.Text($"• {skill.Value.linkedAttribute} ({LocalizationManager.Instance.GetLocalizedString("AttributeLabel")}): {FormatModifier(attributeValue)}");
                                        if (attributeTemp != 0)
                                            ImGui.Text($"• {LocalizationManager.Instance.GetLocalizedString("StatTempTooltip")}: {FormatModifier(attributeTemp)}");
                                        if (attrGearBonus != 0)
                                            ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {skill.Value.linkedAttribute} {LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(attrGearBonus)}");
                                        if (rawSuccesses > 0)
                                            ImGui.TextColored(ImGuiColors.DalamudViolet, $"• {LocalizationManager.Instance.GetLocalizedString("StatEpicTooltip")}: ★{rawSuccesses}");
                                    }
                                    if (hasLinkedAttr || skillGearBonus != 0)
                                    {
                                        ImGui.Separator();
                                        ImGui.TextColored(ImGuiColors.ParsedGreen, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")}: {totalModText} {(rawSuccesses > 0 ? $"(+★{rawSuccesses})" : "")}");
                                    }
                                    ImGui.EndTooltip();
                                }
                            }

                            ImGui.PopID();
                            ImGui.SetCursorScreenPos(new Vector2(pos.X, pos.Y + cardHeight + 4.0f * ImGuiHelpers.GlobalScale));
                        }

                        if (skillToMoveUp != null)
                        {
                            currentCharacter.MoveSkill(skillToMoveUp, -1);
                            currentDiceSystem?.MoveSkill(skillToMoveUp, -1);
                        }
                        if (skillToMoveDown != null)
                        {
                            currentCharacter.MoveSkill(skillToMoveDown, 1);
                            currentDiceSystem?.MoveSkill(skillToMoveDown, 1);
                        }
                    }
                }
            }
        }

        private void DrawAbilitiesColumn(float height)
        {
            if (currentCharacter == null) return;

            using (var child = ImRaii.Child("##AbilitiesColChild", new Vector2(0, height), true))
            {
                if (child.Success)
                {
                    // Column Header
                    var effectiveAbilities = currentCharacter.GetEffectiveAbilities(currentDiceSystem);

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.TankBlue, FontAwesomeIcon.Bolt.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.TankBlue, LocalizationManager.Instance.GetLocalizedString("AbilityLabel"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge((effectiveAbilities?.Count ?? 0).ToString(), new Vector4(0.15f, 0.25f, 0.45f, 0.5f), ImGuiColors.TankBlue);

                    var addBtnWidth = 24.0f * ImGuiHelpers.GlobalScale;
                    var rightX = ImGui.GetWindowContentRegionMax().X - addBtnWidth;
                    if (ImGui.GetCursorPosX() < rightX)
                        ImGui.SameLine(rightX);
                    else
                        ImGui.SameLine();

                    if (UiUtils.IconButton("AddAbilityBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddButton"), new Vector2(24, 24) * ImGuiHelpers.GlobalScale))
                    {
                        newAbilityName = "";
                        newAbilityValue = 0;
                        selectedAttribute = currentCharacter.GetEffectiveAttributes(currentDiceSystem)?.Keys.FirstOrDefault() ?? "";
                        selectedSkill = currentCharacter.GetEffectiveSkills(currentDiceSystem)?.Keys.FirstOrDefault() ?? "";
                        showAbilitiesPopup = true;
                    }

                    ImGui.Separator();
                    ImGui.Spacing();

                    if (effectiveAbilities == null || effectiveAbilities.Count == 0)
                    {
                        ImGui.Spacing();
                        ImGui.PushFont(UiBuilder.IconFont);
                        ImGui.TextColored(ImGuiColors.DalamudGrey, FontAwesomeIcon.InfoCircle.ToIconString());
                        ImGui.PopFont();
                        ImGui.SameLine();
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoAbilitiesDefined"));
                    }
                    else
                    {
                        var abilityList = effectiveAbilities.ToList();
                        string? abilityToRemove = null;
                        string? abilityToMoveUp = null;
                        string? abilityToMoveDown = null;
                        var availWidth = ImGui.GetContentRegionAvail().X;
                        var effectiveAttributes = currentCharacter.GetEffectiveAttributes(currentDiceSystem);

                        for (int i = 0; i < abilityList.Count; i++)
                        {
                            var ability = abilityList[i];
                            ImGui.PushID($"AbilityCard_{ability.Key}");

                            var pos = ImGui.GetCursorScreenPos();
                            var cardHeight = (editingStats ? 36.0f : 36.0f) * ImGuiHelpers.GlobalScale;
                            var cardSize = new Vector2(availWidth, cardHeight);

                            var drawList = ImGui.GetWindowDrawList();
                            bool isHovered = ImGui.IsMouseHoveringRect(pos, pos + cardSize);
                            var bgCol = isHovered
                                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.22f, 0.30f, 0.70f))
                                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.15f, 0.20f, 0.55f));
                            var borderCol = isHovered
                                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.35f, 0.60f, 0.85f, 0.60f))
                                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.22f, 0.32f, 0.45f, 0.40f));

                            drawList.AddRectFilled(pos, pos + cardSize, bgCol, 4.0f * ImGuiHelpers.GlobalScale);
                            drawList.AddRect(pos, pos + cardSize, borderCol, 4.0f * ImGuiHelpers.GlobalScale, ImDrawFlags.None, isHovered ? 1.5f : 1.0f);

                            ImGui.SetCursorScreenPos(pos + new Vector2(6.0f, (cardHeight - 22.0f * ImGuiHelpers.GlobalScale) * 0.5f));

                            int attributeValue = 0;
                            int attributeTemp = 0;
                            int attributePerm = 0;
                            int rawSuccesses = 0;
                            Datamodels.Attribute? linkedAttr = null;
                            bool hasLinkedAttr = !string.IsNullOrEmpty(ability.Value.linkedAttribute) &&
                                                 currentCharacter.characterAttributes != null &&
                                                 currentCharacter.characterAttributes.TryGetValue(ability.Value.linkedAttribute, out linkedAttr);
                            if (hasLinkedAttr && linkedAttr != null)
                            {
                                attributeValue = linkedAttr.Value;
                                attributeTemp = (currentDiceSystem == null || currentDiceSystem.systemHasBonusTemp) ? linkedAttr.TempBonus : 0;
                                attributePerm = (currentDiceSystem == null || currentDiceSystem.systemHasBonusPerm) ? linkedAttr.PermBonus : 0;
                                rawSuccesses = (currentDiceSystem != null ? currentDiceSystem.systemHasEpicAttributes : configuration.showEpicBonus) ? linkedAttr.EpicBonus : 0;
                            }
                            int abilityGearBonus = currentCharacter.GetGearStatBonus(ability.Value.abilityName);
                            int abilityBuffBonus = currentCharacter.GetBuffStatBonus(ability.Value.abilityName);
                            int abilityFeatBonus = currentCharacter.GetFeatStatBonus(ability.Value.abilityName);
                            int attrGearBonus = hasLinkedAttr ? currentCharacter.GetGearStatBonus(ability.Value.linkedAttribute) : 0;
                            int attrBuffBonus = hasLinkedAttr ? currentCharacter.GetBuffStatBonus(ability.Value.linkedAttribute) : 0;
                            int attrFeatBonus = hasLinkedAttr ? currentCharacter.GetFeatStatBonus(ability.Value.linkedAttribute) : 0;
                            int skillValue = ability.Value.linkedSkill != null ? ability.Value.linkedSkill.skillModifier : 0;
                            int skillGearBonus = 0;
                            int skillBuffBonus = 0;
                            int skillFeatBonus = 0;
                            bool hasLinkedSkill = ability.Value.linkedSkill != null && !string.IsNullOrEmpty(ability.Value.linkedSkill.skillName);
                            if (hasLinkedSkill && ability.Value.linkedSkill != null)
                            {
                                skillGearBonus = currentCharacter.GetGearStatBonus(ability.Value.linkedSkill.skillName);
                                skillBuffBonus = currentCharacter.GetBuffStatBonus(ability.Value.linkedSkill.skillName);
                                skillFeatBonus = currentCharacter.GetFeatStatBonus(ability.Value.linkedSkill.skillName);
                            }

                            int effectiveAttrValue = attributeValue + attributeTemp + attributePerm + attrGearBonus + attrBuffBonus + attrFeatBonus;
                            int effectiveSkillValue = skillValue + skillGearBonus + skillBuffBonus + skillFeatBonus;
                            int totalModifier = ability.Value.abilityModifier + abilityGearBonus + abilityBuffBonus + abilityFeatBonus + (hasLinkedAttr ? effectiveAttrValue : 0) + (hasLinkedSkill ? effectiveSkillValue : 0);

                            if (editingStats)
                            {
                                if (i > 0)
                                {
                                    if (UiUtils.IconButton($"MoveUpAbility_{ability.Key}", FontAwesomeIcon.ChevronUp, LocalizationManager.Instance.GetLocalizedString("MoveUpTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                    {
                                        abilityToMoveUp = ability.Key;
                                    }
                                    ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                                }
                                if (i < abilityList.Count - 1)
                                {
                                    if (UiUtils.IconButton($"MoveDownAbility_{ability.Key}", FontAwesomeIcon.ChevronDown, LocalizationManager.Instance.GetLocalizedString("MoveDownTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                    {
                                        abilityToMoveDown = ability.Key;
                                    }
                                    ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                                }

                                if (UiUtils.IconButton($"Del_{ability.Key}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(22, 22) * ImGuiHelpers.GlobalScale))
                                {
                                    abilityToRemove = ability.Key;
                                }
                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, ability.Value.abilityName);
                                if (!string.IsNullOrEmpty(ability.Value.linkedAttribute))
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(ability.Value.linkedAttribute, new Vector4(0.28f, 0.22f, 0.12f, 0.6f), ImGuiColors.ParsedGold);
                                }
                                if (hasLinkedSkill && ability.Value.linkedSkill != null)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(ability.Value.linkedSkill.skillName, new Vector4(0.15f, 0.28f, 0.18f, 0.6f), ImGuiColors.ParsedGreen);
                                }

                                var rightInputX = pos.X + availWidth - 45.0f * ImGuiHelpers.GlobalScale;
                                if (ImGui.GetCursorScreenPos().X < rightInputX)
                                {
                                    ImGui.SetCursorScreenPos(new Vector2(rightInputX, pos.Y + (cardHeight - 22.0f * ImGuiHelpers.GlobalScale) * 0.5f));
                                }
                                UiUtils.StyledInputInt($"AbilityVal_{ability.Key}", ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterAbilities, ability.Key).abilityModifier, step: 0, width: 36.0f);
                            }
                            else
                            {
                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, ability.Value.abilityName);

                                if (!string.IsNullOrEmpty(ability.Value.linkedAttribute))
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(ability.Value.linkedAttribute, new Vector4(0.28f, 0.22f, 0.12f, 0.6f), ImGuiColors.ParsedGold);
                                }
                                if (hasLinkedSkill && ability.Value.linkedSkill != null)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(ability.Value.linkedSkill.skillName, new Vector4(0.15f, 0.28f, 0.18f, 0.6f), ImGuiColors.ParsedGreen);
                                }

                                float rightItemsWidth = 30.0f * ImGuiHelpers.GlobalScale;
                                string baseModText = FormatModifier(ability.Value.abilityModifier);
                                rightItemsWidth += ImGui.CalcTextSize(baseModText).X + 16.0f * ImGuiHelpers.GlobalScale;

                                if (abilityGearBonus != 0)
                                {
                                    string gearText = FormatModifier(abilityGearBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(gearText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                if (abilityBuffBonus != 0)
                                {
                                    string buffText = FormatModifier(abilityBuffBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(buffText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                if (abilityFeatBonus != 0)
                                {
                                    string featText = FormatModifier(abilityFeatBonus);
                                    rightItemsWidth += ImGui.CalcTextSize(featText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                string? totalModText = (hasLinkedAttr || hasLinkedSkill || abilityGearBonus != 0 || abilityBuffBonus != 0 || abilityFeatBonus != 0) ? FormatModifier(totalModifier) : null;
                                if (totalModText != null)
                                {
                                    rightItemsWidth += ImGui.CalcTextSize(totalModText).X + 16.0f * ImGuiHelpers.GlobalScale;
                                }

                                var rightStartX = pos.X + availWidth - rightItemsWidth - 6.0f * ImGuiHelpers.GlobalScale;
                                if (ImGui.GetCursorScreenPos().X < rightStartX)
                                {
                                    ImGui.SetCursorScreenPos(new Vector2(rightStartX, pos.Y + (cardHeight - 20.0f * ImGuiHelpers.GlobalScale) * 0.5f));
                                }

                                UiUtils.Badge(baseModText, new Vector4(0.15f, 0.20f, 0.28f, 0.85f), ImGuiColors.DalamudGrey);
                                if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("NewAbilityValue")}: {baseModText}");

                                if (abilityGearBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var gearCol = abilityGearBonus > 0 ? ImGuiColors.ParsedBlue : ImGuiColors.DalamudRed;
                                    var gearBg = abilityGearBonus > 0 ? new Vector4(0.12f, 0.22f, 0.38f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(abilityGearBonus), gearBg, gearCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(abilityGearBonus)}");
                                }

                                if (abilityBuffBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var buffCol = abilityBuffBonus > 0 ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudRed;
                                    var buffBg = abilityBuffBonus > 0 ? new Vector4(0.12f, 0.30f, 0.16f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(abilityBuffBonus), buffBg, buffCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"Buff / Debuff: {FormatModifier(abilityBuffBonus)}");
                                }

                                if (abilityFeatBonus != 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    var featCol = abilityFeatBonus > 0 ? ImGuiColors.ParsedPurple : ImGuiColors.DalamudRed;
                                    var featBg = abilityFeatBonus > 0 ? new Vector4(0.24f, 0.12f, 0.32f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                                    UiUtils.Badge(FormatModifier(abilityFeatBonus), featBg, featCol);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(abilityFeatBonus)}");
                                }

                                if (totalModText != null)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(totalModText, new Vector4(0.16f, 0.32f, 0.55f, 0.85f), ImGuiColors.TankBlue);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")}: {totalModText}");
                                }

                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"Roll_{ability.Value.abilityName}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {ability.Value.abilityName}", new Vector2(24, 22) * ImGuiHelpers.GlobalScale))
                                {
                                    statRollType = StatRollType.Ability;
                                    statRollName = ability.Value.abilityName;
                                    statRollAbility = ability.Value;
                                    statRollSkill = null;
                                    statRollAttribute = null;
                                    rollBonusOrPenalty = 0;
                                    showStatRollModal = true;
                                }

                                if (isHovered && !ImGui.IsAnyItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.TextColored(ImGuiColors.TankBlue, ability.Value.abilityName);
                                    if (!string.IsNullOrWhiteSpace(ability.Value.abilityDescription))
                                    {
                                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                                        ImGui.TextWrapped(ability.Value.abilityDescription);
                                        ImGui.PopStyleColor();
                                    }
                                    ImGui.Separator();
                                    ImGui.Text($"• {LocalizationManager.Instance.GetLocalizedString("NewAbilityValue")}: {baseModText}");
                                    if (abilityGearBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(abilityGearBonus)}");
                                    if (abilityFeatBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedPurple, $"• {LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(abilityFeatBonus)}");
                                    if (hasLinkedAttr)
                                    {
                                        ImGui.Text($"• {ability.Value.linkedAttribute} ({LocalizationManager.Instance.GetLocalizedString("AttributeLabel")}): {FormatModifier(attributeValue)}");
                                        if (attributeTemp != 0)
                                            ImGui.Text($"• {LocalizationManager.Instance.GetLocalizedString("StatTempTooltip")}: {FormatModifier(attributeTemp)}");
                                        if (attrGearBonus != 0)
                                            ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {ability.Value.linkedAttribute} {LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(attrGearBonus)}");
                                        if (rawSuccesses > 0)
                                            ImGui.TextColored(ImGuiColors.DalamudViolet, $"• {LocalizationManager.Instance.GetLocalizedString("StatEpicTooltip")}: ★{rawSuccesses}");
                                    }
                                    if (hasLinkedSkill && ability.Value.linkedSkill != null)
                                    {
                                        ImGui.Text($"• {ability.Value.linkedSkill.skillName} ({LocalizationManager.Instance.GetLocalizedString("SkillLabel")}): {FormatModifier(skillValue)}");
                                        if (skillGearBonus != 0)
                                            ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {ability.Value.linkedSkill.skillName} {LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(skillGearBonus)}");
                                    }
                                    if (hasLinkedAttr || hasLinkedSkill || abilityGearBonus != 0)
                                    {
                                        ImGui.Separator();
                                        ImGui.TextColored(ImGuiColors.TankBlue, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")}: {totalModText} {(rawSuccesses > 0 ? $"(+★{rawSuccesses})" : "")}");
                                    }
                                    ImGui.EndTooltip();
                                }
                            }

                            ImGui.PopID();
                            ImGui.SetCursorScreenPos(new Vector2(pos.X, pos.Y + cardHeight + 4.0f * ImGuiHelpers.GlobalScale));
                        }

                        if (abilityToMoveUp != null)
                        {
                            currentCharacter.MoveAbility(abilityToMoveUp, -1);
                            currentDiceSystem?.MoveAbility(abilityToMoveUp, -1);
                        }
                        if (abilityToMoveDown != null)
                        {
                            currentCharacter.MoveAbility(abilityToMoveDown, 1);
                            currentDiceSystem?.MoveAbility(abilityToMoveDown, 1);
                        }
                        if (abilityToRemove != null)
                        {
                            currentCharacter.characterAbilities.Remove(abilityToRemove);
                            currentDiceSystem?.SystemAbilities?.Remove(abilityToRemove);
                        }
                    }
                }
            }
        }

        private void DrawModals()
        {
            if (currentCharacter == null) return;
            var scale = ImGuiHelpers.GlobalScale;

            // New Resource Modal
            if (showResourcePopup)
            {
                ImGui.OpenPopup("NewResourceModal");
            }
            if (ImGui.BeginPopupModal("NewResourceModal", ref showResourcePopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.Heartbeat.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * scale);
                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("DiceSysAddResource"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceName"));
                UiUtils.StyledInputText("NewResName", ref newResourceName, 100, width: 260.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceMax"));
                UiUtils.StyledInputInt("NewResMaxVal", ref newResourceMaxValue, 1, width: 100.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("ResourceTypeLabel"));
                string[] resTypeNames = {
                    LocalizationManager.Instance.GetLocalizedString("ResourceTypeBar"),
                    LocalizationManager.Instance.GetLocalizedString("ResourceTypeCounter"),
                    LocalizationManager.Instance.GetLocalizedString("ResourceTypeFlatNumber")
                };
                UiUtils.StyledCombo("##NewResTypeCombo", ref newResourceType, resTypeNames, width: 180.0f);

                if (newResourceType == 2)
                {
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("ResourceRollableLabel"), ref newResourceIsRollable);
                }

                ImGui.Spacing();
                ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("ResourceShowInGroupLabel"), ref newResourceShowInGroup);

                ImGui.Spacing();
                if (UiUtils.IconTextButton("AddResConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (!string.IsNullOrWhiteSpace(newResourceName))
                    {
                        var createdRes = new CharacterResource(newResourceName, newResourceMaxValue, newResourceMaxValue)
                        {
                            ResourceType = (ResourceType)newResourceType,
                            IsRollable = newResourceIsRollable,
                            ShowInGroup = newResourceShowInGroup
                        };
                        currentCharacter.characterResources ??= new Dictionary<string, CharacterResource>(StringComparer.OrdinalIgnoreCase);
                        currentCharacter.characterResources[newResourceName] = createdRes;
                        newResourceName = "";
                        newResourceMaxValue = 100;
                        newResourceType = 0;
                        newResourceIsRollable = false;
                        newResourceShowInGroup = true;
                        showResourcePopup = false;
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("AddResCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
                {
                    showResourcePopup = false;
                }

                ImGui.EndPopup();
            }

            // New Attribute Modal
            if (showAttributesPopup)
            {
                ImGui.OpenPopup("NewAttributeModal");
            }
            if (ImGui.BeginPopupModal("NewAttributeModal", ref showAttributesPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.ShieldAlt.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * scale);
                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("AttributeLabel"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAttributeNameLabel"));
                UiUtils.StyledInputText("NewAttrName", ref newAttributeName, 100, width: 260.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAttributeValueLabel"));
                UiUtils.StyledInputInt("NewAttrVal", ref newAttributeValue, 1, width: 100.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAttributeDescription"));
                UiUtils.StyledInputText("NewAttrDesc", ref newAttributeDescription, 200, width: 260.0f);

                bool hasFavoriteAttributes = currentDiceSystem == null || currentDiceSystem.systemHasFavoriteAttributes;
                if (hasFavoriteAttributes)
                {
                    ImGui.Spacing();
                    ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("FavoriteAttributeLabel"), ref newAttributeFavorite);
                }

                ImGui.Spacing();
                if (UiUtils.IconTextButton("AddAttrConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (!string.IsNullOrWhiteSpace(newAttributeName))
                    {
                        currentCharacter.characterAttributes ??= new Dictionary<string, Datamodels.Attribute>();
                        if (!currentCharacter.characterAttributes.ContainsKey(newAttributeName))
                        {
                            var newAttr = new Datamodels.Attribute(newAttributeName, newAttributeValue, newAttributeDescription, hasFavoriteAttributes && newAttributeFavorite);
                            currentCharacter.characterAttributes.Add(newAttributeName, newAttr);
                            if (currentDiceSystem != null && currentDiceSystem.SystemAttributes != null && currentDiceSystem.SystemAttributes.Count > 0)
                            {
                                currentDiceSystem.SystemAttributes[newAttributeName] = new Datamodels.Attribute(newAttributeName, newAttributeValue, newAttributeDescription, false);
                            }
                            newAttributeName = "";
                            newAttributeValue = 0;
                            newAttributeDescription = "";
                            newAttributeFavorite = false;
                            showAttributesPopup = false;
                        }
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("AddAttrCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
                {
                    newAttributeDescription = "";
                    newAttributeFavorite = false;
                    showAttributesPopup = false;
                }

                ImGui.EndPopup();
            }

            // New Skill Modal
            if (showSkillPopup)
            {
                ImGui.OpenPopup("NewSkillModal");
            }
            if (ImGui.BeginPopupModal("NewSkillModal", ref showSkillPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.Book.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * scale);
                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("SkillLabel"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewSkillName"));
                UiUtils.StyledInputText("NewSkillName", ref newSkillName, 100, width: 260.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewSkillValue"));
                UiUtils.StyledInputInt("NewSkillVal", ref newSkillValue, 1, width: 100.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewSkillDescription"));
                UiUtils.StyledInputText("NewSkillDesc", ref newSkillDescription, 200, width: 260.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewLinkedAttribute"));
                var attrKeys = currentCharacter.GetEffectiveAttributes(currentDiceSystem)?.Keys.ToList() ?? new List<string>();
                if (attrKeys.Count > 0)
                {
                    if (string.IsNullOrEmpty(selectedAttribute) || !attrKeys.Contains(selectedAttribute))
                        selectedAttribute = attrKeys[0];

                    UiUtils.StyledCombo("##LinkedAttrCombo", ref selectedAttribute, attrKeys, icon: FontAwesomeIcon.Link);
                }
                else
                {
                    UiUtils.StyledInputText("LinkedAttrText", ref selectedAttribute, 100, width: 260.0f);
                }

                ImGui.Spacing();
                if (UiUtils.IconTextButton("AddSkillConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (!string.IsNullOrWhiteSpace(newSkillName))
                    {
                        newSkill = new Skill
                        {
                            skillName = newSkillName,
                            skillModifier = newSkillValue,
                            linkedAttribute = selectedAttribute,
                            skillDescription = newSkillDescription
                        };
                        currentCharacter.characterSkills ??= new Dictionary<string, Skill>();
                        if (!currentCharacter.characterSkills.ContainsKey(newSkillName))
                        {
                            currentCharacter.characterSkills.Add(newSkillName, newSkill);
                            if (currentDiceSystem != null && currentDiceSystem.SystemSkills != null && currentDiceSystem.SystemSkills.Count > 0)
                            {
                                currentDiceSystem.SystemSkills[newSkillName] = new Skill
                                {
                                    skillName = newSkillName,
                                    skillModifier = newSkillValue,
                                    linkedAttribute = selectedAttribute,
                                    skillDescription = newSkillDescription
                                };
                            }
                            newSkillName = "";
                            newSkillValue = 0;
                            newSkillDescription = "";
                            selectedAttribute = "";
                            showSkillPopup = false;
                        }
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("AddSkillCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
                {
                    newSkillDescription = "";
                    showSkillPopup = false;
                }

                ImGui.EndPopup();
            }

            // Stat Roll Modal (Attribute, Saving Throw, Skill, Ability)
            if (showStatRollModal)
            {
                ImGui.OpenPopup("StatRollModal");
            }
            if (ImGui.BeginPopupModal("StatRollModal", ref showStatRollModal, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                var headerIcon = statRollType switch
                {
                    StatRollType.SavingThrow => FontAwesomeIcon.ShieldAlt,
                    StatRollType.Skill => FontAwesomeIcon.DiceD20,
                    StatRollType.Ability => FontAwesomeIcon.Bolt,
                    _ => FontAwesomeIcon.DiceD20
                };
                var headerCol = statRollType switch
                {
                    StatRollType.SavingThrow => ImGuiColors.ParsedGold,
                    StatRollType.Skill => ImGuiColors.ParsedGreen,
                    StatRollType.Ability => ImGuiColors.TankBlue,
                    _ => ImGuiColors.ParsedGold
                };

                ImGui.TextColored(headerCol, headerIcon.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * scale);

                string modalTitle = statRollType switch
                {
                    StatRollType.SavingThrow => string.Format(LocalizationManager.Instance.GetLocalizedString("SavingThrowRollFormat"), statRollName),
                    StatRollType.Skill => LocalizationManager.Instance.GetLocalizedString("DynamicSkillModalTitle", statRollSkill?.skillName ?? statRollName),
                    StatRollType.Ability => LocalizationManager.Instance.GetLocalizedString("RollAbilityModalTitle", statRollAbility?.abilityName ?? statRollName),
                    _ => string.Format(LocalizationManager.Instance.GetLocalizedString("RollAttributeModalTitle"), statRollName)
                };

                ImGui.TextColored(headerCol, modalTitle);
                ImGui.Separator();
                ImGui.Spacing();

                int calculatedBaseTotal = 0;
                int rawSuccesses = 0;
                string rollLabel = statRollName;

                if (statRollType == StatRollType.Attribute || statRollType == StatRollType.SavingThrow)
                {
                    if (!string.IsNullOrWhiteSpace(statRollAttribute?.Description))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                        ImGui.TextWrapped(statRollAttribute.Description);
                        ImGui.PopStyleColor();
                        ImGui.Spacing();
                    }

                    int baseVal = statRollAttribute?.Value ?? 0;
                    int tempVal = (currentDiceSystem == null || currentDiceSystem.systemHasBonusTemp) ? (statRollAttribute?.TempBonus ?? 0) : 0;
                    int permVal = (currentDiceSystem == null || currentDiceSystem.systemHasBonusPerm) ? (statRollAttribute?.PermBonus ?? 0) : 0;
                    int gearBonus = currentCharacter.GetGearStatBonus(statRollName);
                    int buffBonus = currentCharacter.GetBuffStatBonus(statRollName);
                    int featBonus = currentCharacter.GetFeatStatBonus(statRollName);
                    rawSuccesses = (currentDiceSystem != null ? currentDiceSystem.systemHasEpicAttributes : configuration.showEpicBonus) ? (statRollAttribute?.EpicBonus ?? 0) : 0;

                    calculatedBaseTotal = baseVal + tempVal + permVal + gearBonus + buffBonus + featBonus;

                    ImGui.Text(LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltyLabel"));
                    UiUtils.StyledInputInt("StatRollBonusInput", ref rollBonusOrPenalty, 1, width: 120.0f);
                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltyTooltip"));

                    int finalTotal = calculatedBaseTotal + rollBonusOrPenalty;

                    ImGui.Spacing();
                    ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("StatValueTooltip")}: {baseVal}");
                    if (tempVal != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("StatTempTooltip")}: {FormatModifier(tempVal)}");
                    if (permVal != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("StatPermTooltip")}: {FormatModifier(permVal)}");
                    if (gearBonus != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(gearBonus)}");
                    if (buffBonus != 0) ImGui.TextDisabled($"Buff / Debuff: {FormatModifier(buffBonus)}");
                    if (featBonus != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(featBonus)}");
                    if (rollBonusOrPenalty != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltySummary")}: {FormatModifier(rollBonusOrPenalty)}");

                    ImGui.TextColored(ImGuiColors.ParsedGold, $"{LocalizationManager.Instance.GetLocalizedString("TotalModifierLabel")}: {FormatModifier(finalTotal)} {(rawSuccesses > 0 ? $"(+★{rawSuccesses})" : "")}");

                    rollLabel = statRollType == StatRollType.SavingThrow
                        ? string.Format(LocalizationManager.Instance.GetLocalizedString("SavingThrowRollFormat"), statRollName)
                        : statRollName;
                }
                else if (statRollType == StatRollType.Skill)
                {
                    if (!string.IsNullOrWhiteSpace(statRollSkill?.skillDescription))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                        ImGui.TextWrapped(statRollSkill.skillDescription);
                        ImGui.PopStyleColor();
                        ImGui.Spacing();
                    }

                    bool isDynamic = currentDiceSystem?.dynamicSkillAttributeLinking == true && currentCharacter.characterAttributes != null && currentCharacter.characterAttributes.Count > 0;
                    if (isDynamic)
                    {
                        ImGui.Text(LocalizationManager.Instance.GetLocalizedString("SelectLinkedAttributePrompt"));
                        var noneLabel = LocalizationManager.Instance.GetLocalizedString("NoneOption");
                        var effectiveAttrs = currentCharacter.GetEffectiveAttributes(currentDiceSystem);
                        var attrKeys = effectiveAttrs?.Keys.ToList() ?? new List<string>();
                        var attrOptions = new List<string> { "" };
                        attrOptions.AddRange(attrKeys);
                        UiUtils.StyledCombo("##DynamicSkillAttrCombo", ref selectedDynamicAttr, attrOptions, icon: FontAwesomeIcon.Link, emptyLabel: noneLabel);
                        ImGui.Spacing();
                    }

                    int dynAttrVal = 0;
                    int dynAttrTemp = 0;
                    int dynAttrPerm = 0;
                    int dynAttrGearBonus = 0;
                    int dynAttrBuffBonus = 0;
                    int dynAttrFeatBonus = 0;

                    string linkedAttrName = isDynamic ? selectedDynamicAttr : (statRollSkill?.linkedAttribute ?? "");
                    if (!string.IsNullOrEmpty(linkedAttrName))
                    {
                        var effectiveAttrs = currentCharacter.GetEffectiveAttributes(currentDiceSystem);
                        if (effectiveAttrs != null && effectiveAttrs.TryGetValue(linkedAttrName, out var dynAttr) && dynAttr != null)
                        {
                            dynAttrVal = dynAttr.Value;
                            dynAttrTemp = (currentDiceSystem == null || currentDiceSystem.systemHasBonusTemp) ? dynAttr.TempBonus : 0;
                            dynAttrPerm = (currentDiceSystem == null || currentDiceSystem.systemHasBonusPerm) ? dynAttr.PermBonus : 0;
                            rawSuccesses = (currentDiceSystem != null ? currentDiceSystem.systemHasEpicAttributes : configuration.showEpicBonus) ? dynAttr.EpicBonus : 0;
                            dynAttrGearBonus = currentCharacter.GetGearStatBonus(linkedAttrName);
                            dynAttrBuffBonus = currentCharacter.GetBuffStatBonus(linkedAttrName);
                            dynAttrFeatBonus = currentCharacter.GetFeatStatBonus(linkedAttrName);
                        }
                    }

                    int skillBaseMod = statRollSkill?.skillModifier ?? 0;
                    int skillGear = statRollSkill != null ? currentCharacter.GetGearStatBonus(statRollSkill.skillName) : 0;
                    int skillBuff = statRollSkill != null ? currentCharacter.GetBuffStatBonus(statRollSkill.skillName) : 0;
                    int skillFeat = statRollSkill != null ? currentCharacter.GetFeatStatBonus(statRollSkill.skillName) : 0;
                    int totalAttrMod = dynAttrVal + dynAttrTemp + dynAttrPerm + dynAttrGearBonus + dynAttrBuffBonus + dynAttrFeatBonus;
                    calculatedBaseTotal = skillBaseMod + skillGear + skillBuff + skillFeat + totalAttrMod;

                    ImGui.Text(LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltyLabel"));
                    UiUtils.StyledInputInt("SkillRollBonusInput", ref rollBonusOrPenalty, 1, width: 120.0f);
                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltyTooltip"));

                    int finalTotal = calculatedBaseTotal + rollBonusOrPenalty;

                    ImGui.Spacing();
                    ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("NewSkillValue")}: {FormatModifier(skillBaseMod)}");
                    if (skillGear != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(skillGear)}");
                    if (skillBuff != 0) ImGui.TextDisabled($"Buff: {FormatModifier(skillBuff)}");
                    if (skillFeat != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(skillFeat)}");
                    if (!string.IsNullOrEmpty(linkedAttrName))
                    {
                        ImGui.TextDisabled($"{linkedAttrName}: {FormatModifier(totalAttrMod)}");
                    }
                    if (rollBonusOrPenalty != 0)
                    {
                        ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltySummary")}: {FormatModifier(rollBonusOrPenalty)}");
                    }

                    ImGui.TextColored(ImGuiColors.ParsedGreen, $"{LocalizationManager.Instance.GetLocalizedString("TotalModifierLabel")}: {FormatModifier(finalTotal)} {(rawSuccesses > 0 ? $"(+★{rawSuccesses})" : "")}");

                    rollLabel = !string.IsNullOrEmpty(linkedAttrName) && isDynamic
                        ? $"{statRollSkill?.skillName} ({linkedAttrName})"
                        : (statRollSkill?.skillName ?? "Skill");
                }
                else if (statRollType == StatRollType.Ability)
                {
                    if (!string.IsNullOrWhiteSpace(statRollAbility?.abilityDescription))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                        ImGui.TextWrapped(statRollAbility.abilityDescription);
                        ImGui.PopStyleColor();
                        ImGui.Spacing();
                    }

                    int abilBaseMod = statRollAbility?.abilityModifier ?? 0;
                    int abilGear = statRollAbility != null ? currentCharacter.GetGearStatBonus(statRollAbility.abilityName) : 0;
                    int abilBuff = statRollAbility != null ? currentCharacter.GetBuffStatBonus(statRollAbility.abilityName) : 0;
                    int abilFeat = statRollAbility != null ? currentCharacter.GetFeatStatBonus(statRollAbility.abilityName) : 0;

                    int attrMod = 0;
                    if (!string.IsNullOrEmpty(statRollAbility?.linkedAttribute))
                    {
                        var effectiveAttrs = currentCharacter.GetEffectiveAttributes(currentDiceSystem);
                        if (effectiveAttrs != null && effectiveAttrs.TryGetValue(statRollAbility.linkedAttribute, out var dynAttr) && dynAttr != null)
                        {
                            int aVal = dynAttr.Value;
                            int aTemp = (currentDiceSystem == null || currentDiceSystem.systemHasBonusTemp) ? dynAttr.TempBonus : 0;
                            int aPerm = (currentDiceSystem == null || currentDiceSystem.systemHasBonusPerm) ? dynAttr.PermBonus : 0;
                            int aGear = currentCharacter.GetGearStatBonus(statRollAbility.linkedAttribute);
                            int aBuff = currentCharacter.GetBuffStatBonus(statRollAbility.linkedAttribute);
                            int aFeat = currentCharacter.GetFeatStatBonus(statRollAbility.linkedAttribute);
                            rawSuccesses = (currentDiceSystem != null ? currentDiceSystem.systemHasEpicAttributes : configuration.showEpicBonus) ? dynAttr.EpicBonus : 0;
                            attrMod = aVal + aTemp + aPerm + aGear + aBuff + aFeat;
                        }
                    }

                    int skillMod = 0;
                    if (statRollAbility?.linkedSkill != null && !string.IsNullOrEmpty(statRollAbility.linkedSkill.skillName))
                    {
                        int sBase = statRollAbility.linkedSkill.skillModifier;
                        int sGear = currentCharacter.GetGearStatBonus(statRollAbility.linkedSkill.skillName);
                        int sBuff = currentCharacter.GetBuffStatBonus(statRollAbility.linkedSkill.skillName);
                        int sFeat = currentCharacter.GetFeatStatBonus(statRollAbility.linkedSkill.skillName);
                        skillMod = sBase + sGear + sBuff + sFeat;
                    }

                    calculatedBaseTotal = abilBaseMod + abilGear + abilBuff + abilFeat + attrMod + skillMod;

                    ImGui.Text(LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltyLabel"));
                    UiUtils.StyledInputInt("AbilRollBonusInput", ref rollBonusOrPenalty, 1, width: 120.0f);
                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltyTooltip"));

                    int finalTotal = calculatedBaseTotal + rollBonusOrPenalty;

                    ImGui.Spacing();
                    ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("NewAbilityValue")}: {FormatModifier(abilBaseMod)}");
                    if (abilGear != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(abilGear)}");
                    if (abilBuff != 0) ImGui.TextDisabled($"Buff: {FormatModifier(abilBuff)}");
                    if (abilFeat != 0) ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("FeatBonusTooltip")}: {FormatModifier(abilFeat)}");
                    if (!string.IsNullOrEmpty(statRollAbility?.linkedAttribute))
                    {
                        ImGui.TextDisabled($"{statRollAbility.linkedAttribute}: {FormatModifier(attrMod)}");
                    }
                    if (statRollAbility?.linkedSkill != null && !string.IsNullOrEmpty(statRollAbility.linkedSkill.skillName))
                    {
                        ImGui.TextDisabled($"{statRollAbility.linkedSkill.skillName}: {FormatModifier(skillMod)}");
                    }
                    if (rollBonusOrPenalty != 0)
                    {
                        ImGui.TextDisabled($"{LocalizationManager.Instance.GetLocalizedString("RollBonusPenaltySummary")}: {FormatModifier(rollBonusOrPenalty)}");
                    }

                    ImGui.TextColored(ImGuiColors.TankBlue, $"{LocalizationManager.Instance.GetLocalizedString("TotalModifierLabel")}: {FormatModifier(finalTotal)} {(rawSuccesses > 0 ? $"(+★{rawSuccesses})" : "")}");

                    rollLabel = statRollAbility?.abilityName ?? "Ability";
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                int totalFinalModifier = calculatedBaseTotal + rollBonusOrPenalty;
                if (UiUtils.IconTextButton("StatRollConfirmBtn", FontAwesomeIcon.DiceD20, LocalizationManager.Instance.GetLocalizedString("ThrowButton"), size: new Vector2(110, 0) * scale))
                {
                    int totalDice = totalFinalModifier;
                    int totalTarget = totalFinalModifier;
                    DiceRoll.RollDice(totalDice, totalFinalModifier, advantageRoll, disadvantageRoll, rollLabel, detailedRoll, totalTarget, rawSuccesses);
                    showStatRollModal = false;
                    rollBonusOrPenalty = 0;
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("StatRollCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
                {
                    showStatRollModal = false;
                    rollBonusOrPenalty = 0;
                }

                ImGui.EndPopup();
            }

            // New Ability Modal
            if (showAbilitiesPopup)
            {
                ImGui.OpenPopup("NewAbilityModal");
            }
            if (ImGui.BeginPopupModal("NewAbilityModal", ref showAbilitiesPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.TankBlue, FontAwesomeIcon.Bolt.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * scale);
                ImGui.TextColored(ImGuiColors.TankBlue, LocalizationManager.Instance.GetLocalizedString("AbilityLabel"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAbilityName"));
                UiUtils.StyledInputText("NewAbilityName", ref newAbilityName, 100, width: 260.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAbilityValue"));
                UiUtils.StyledInputInt("NewAbilityVal", ref newAbilityValue, 1, width: 100.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAbilityDescription"));
                UiUtils.StyledInputText("NewAbilityDesc", ref newAbilityDescription, 200, width: 260.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewLinkedAttribute"));
                var noneLabel = LocalizationManager.Instance.GetLocalizedString("NoneOption");
                var attrKeys = currentCharacter.GetEffectiveAttributes(currentDiceSystem)?.Keys.ToList() ?? new List<string>();
                var attrOptions = new List<string> { "" };
                attrOptions.AddRange(attrKeys);
                UiUtils.StyledCombo("##AbilityLinkedAttrCombo", ref selectedAttribute, attrOptions, icon: FontAwesomeIcon.Link, emptyLabel: noneLabel);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewLinkedSkill"));
                var skillKeysList = currentCharacter.GetEffectiveSkills(currentDiceSystem)?.Keys.ToList() ?? new List<string>();
                var skillOptions = new List<string> { "" };
                skillOptions.AddRange(skillKeysList);
                UiUtils.StyledCombo("##AbilityLinkedSkillCombo", ref selectedSkill, skillOptions, icon: FontAwesomeIcon.Book, emptyLabel: noneLabel);

                ImGui.Spacing();
                if (UiUtils.IconTextButton("AddAbilityConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (!string.IsNullOrWhiteSpace(newAbilityName))
                    {
                        newAbility = new Ability
                        {
                            abilityName = newAbilityName,
                            abilityModifier = newAbilityValue,
                            linkedAttribute = selectedAttribute,
                            abilityDescription = newAbilityDescription
                        };
                        if (currentCharacter.characterSkills != null && !string.IsNullOrEmpty(selectedSkill))
                        {
                            currentCharacter.characterSkills.TryGetValue(selectedSkill, out newAbility.linkedSkill);
                        }
                        currentCharacter.characterAbilities ??= new Dictionary<string, Ability>();
                        if (!currentCharacter.characterAbilities.ContainsKey(newAbilityName))
                        {
                            currentCharacter.characterAbilities.Add(newAbilityName, newAbility);
                            if (currentDiceSystem != null && currentDiceSystem.SystemAbilities != null && currentDiceSystem.SystemAbilities.Count > 0)
                            {
                                currentDiceSystem.SystemAbilities[newAbilityName] = new Ability
                                {
                                    abilityName = newAbilityName,
                                    abilityModifier = newAbilityValue,
                                    linkedAttribute = selectedAttribute,
                                    linkedSkill = newAbility.linkedSkill,
                                    abilityDescription = newAbilityDescription
                                };
                            }
                            newAbilityName = "";
                            newAbilityValue = 0;
                            newAbilityDescription = "";
                            selectedAttribute = "";
                            selectedSkill = "";
                            showAbilitiesPopup = false;
                        }
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("AddAbilityCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
                {
                    newAbilityDescription = "";
                    showAbilitiesPopup = false;
                }

                ImGui.EndPopup();
            }

            // New Buff Modal
            if (showBuffPopup)
            {
                ImGui.OpenPopup("NewBuffModal###CharNewBuffModal");
            }
            if (ImGui.BeginPopupModal("NewBuffModal###CharNewBuffModal", ref showBuffPopup, ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.Magic.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * scale);
                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("BuffModalTitle"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("BuffNameLabel"));
                UiUtils.StyledInputText("CharBuffName", ref newCharBuffName, 60, width: 280.0f);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("BuffDurationLabel"));
                UiUtils.StyledInputInt("CharBuffDuration", ref newCharBuffDuration, step: 1, width: 100.0f, min: 1);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("BuffTargetStatLabel"));
                UiUtils.StyledInputText("CharBuffTargetStat", ref newCharBuffTargetStat, 60, width: 280.0f, hint: LocalizationManager.Instance.GetLocalizedString("BuffStatNameHint"));

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("BuffValueLabel"));
                if (UiUtils.StyledInputInt("CharBuffValue", ref newCharBuffValue, step: 1, width: 100.0f))
                {
                    if (newCharBuffValue < 0) newCharBuffIsDebuff = true;
                }

                ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("BuffIsDebuffLabel"), ref newCharBuffIsDebuff);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceDescription"));
                UiUtils.StyledInputText("CharBuffDesc", ref newCharBuffDesc, 120, width: 280.0f);

                ImGui.Spacing();
                if (UiUtils.IconTextButton("AddBuffConfirmBtn", FontAwesomeIcon.Check, LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), size: new Vector2(110, 0) * scale))
                {
                    if (!string.IsNullOrWhiteSpace(newCharBuffName))
                    {
                        int val = newCharBuffValue;
                        if (newCharBuffIsDebuff && val > 0) val = -val;
                        var buff = new Buff(newCharBuffName.Trim(), Math.Max(1, newCharBuffDuration), newCharBuffTargetStat.Trim(), val, newCharBuffDesc.Trim(), newCharBuffIsDebuff);
                        currentCharacter.AddBuff(buff);
                        newCharBuffName = "";
                        newCharBuffTargetStat = "";
                        showBuffPopup = false;
                    }
                }
                ImGui.SameLine(0, 8.0f * scale);
                if (UiUtils.IconTextButton("AddBuffCancelBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton"), size: new Vector2(90, 0) * scale))
                {
                    showBuffPopup = false;
                }

                ImGui.EndPopup();
            }
        }
    }
}
