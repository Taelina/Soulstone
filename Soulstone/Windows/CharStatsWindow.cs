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
        public string diceType = "";

        private bool showAbilitiesPopup = false;
        private bool showSkillPopup = false;
        private bool showAttributesPopup = false;
        private bool showBuffPopup = false;

        private CharacterSheet? currentCharacter = null;
        private DiceSystem? currentDiceSystem = null;

        private bool editingStats = false;

        private string newAttributeName = "";
        private int newAttributeValue = 0;

        private string newSkillName = "";
        private int newSkillValue = 0;
        private string selectedAttribute = "";
        private Skill? newSkill = null;

        private string newAbilityName = "";
        private int newAbilityValue = 0;
        private string selectedSkill = "";
        private Ability? newAbility = null;

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
                    if (UiUtils.IconButton("AddCharBuffBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddBuffButton"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
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

            using (var banner = ImRaii.Child("##VitalsBanner", new Vector2(0, 95.0f * ImGuiHelpers.GlobalScale), true))
            {
                if (banner.Success)
                {
                    // Top row: Class, Level, XP, System Dice, Edit Toggle, Save
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

                    ImGui.Separator();

                    // Bottom row: Dynamic generic resources & advantage/disadvantage
                    var resources = currentCharacter.GetEffectiveResources(currentDiceSystem);
                    for (int i = 0; i < resources.Count; i++)
                    {
                        var res = resources[i];
                        if (i > 0)
                        {
                            ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);
                        }

                        var def = currentDiceSystem?.SystemResources.FirstOrDefault(d => string.Equals(d.Name, res.Name, StringComparison.OrdinalIgnoreCase));
                        var resCol = GetResourceColor(res.Name, def?.ColorHex);

                        ImGui.TextColored(resCol, $"{res.Name}:");
                        ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);

                        if (editingStats)
                        {
                            ImGui.SetNextItemWidth(45.0f * ImGuiHelpers.GlobalScale);
                            int curVal = res.CurrentValue;
                            if (ImGui.InputInt($"##ResCur_{res.Name}", ref curVal, 0))
                            {
                                currentCharacter.SetResourceCurrent(res.Name, curVal);
                            }
                            ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                            ImGui.Text("/");
                            ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                            ImGui.SetNextItemWidth(45.0f * ImGuiHelpers.GlobalScale);
                            int maxVal = res.MaxValue;
                            if (ImGui.InputInt($"##ResMax_{res.Name}", ref maxVal, 0))
                            {
                                currentCharacter.SetResourceMax(res.Name, maxVal);
                            }

                            string effectiveFormula = !string.IsNullOrWhiteSpace(res.Formula) ? res.Formula : (def?.Formula ?? string.Empty);
                            if (!string.IsNullOrWhiteSpace(effectiveFormula))
                            {
                                ImGui.SameLine(0, 2.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"RecalcRes_{res.Name}", FontAwesomeIcon.Sync, LocalizationManager.Instance.GetLocalizedString("RecalculateResourcesBtn"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    currentCharacter.RecalculateResourceMax(res.Name, currentDiceSystem);
                                }
                                if (ImGui.IsItemHovered())
                                {
                                    ImGui.SetTooltip($"{LocalizationManager.Instance.GetLocalizedString("RecalculateResourcesTooltip")}\n({effectiveFormula})");
                                }
                            }
                        }
                        else
                        {
                            int effectiveMax = currentCharacter.GetEffectiveResourceMax(res.Name, currentDiceSystem);
                            int gearBonus = currentCharacter.GetGearStatBonus(res.Name) + currentCharacter.GetGearStatBonus($"Max {res.Name}") + currentCharacter.GetGearStatBonus($"Max{res.Name}");
                            float fraction = effectiveMax > 0
                                ? Math.Clamp((float)res.CurrentValue / effectiveMax, 0f, 1f)
                                : 1f;
                            string overlay = effectiveMax > 0
                                ? $"{res.CurrentValue} / {effectiveMax}{(gearBonus != 0 ? $" ({FormatModifier(gearBonus)})" : "")}"
                                : $"{res.CurrentValue}";
                            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, resCol);
                            ImGui.ProgressBar(fraction, new Vector2(95.0f * ImGuiHelpers.GlobalScale, 18.0f * ImGuiHelpers.GlobalScale), overlay);
                            ImGui.PopStyleColor();

                            if (ImGui.IsItemHovered())
                            {
                                string effectiveFormula = !string.IsNullOrWhiteSpace(res.Formula) ? res.Formula : (def?.Formula ?? string.Empty);
                                ImGui.BeginTooltip();
                                ImGui.TextColored(resCol, res.Name);
                                ImGui.Separator();
                                ImGui.Text($"• Current: {res.CurrentValue}");
                                if (!string.IsNullOrWhiteSpace(effectiveFormula))
                                {
                                    ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {LocalizationManager.Instance.GetLocalizedString("DiceSysResourceFormulaHeader")}: {effectiveFormula}");
                                }
                                ImGui.Text($"• Base Max: {res.MaxValue}");
                                if (res.TempBonus != 0) ImGui.Text($"• Temp Max: {FormatModifier(res.TempBonus)}");
                                if (gearBonus != 0) ImGui.TextColored(ImGuiColors.ParsedBlue, $"• Gear Bonus: {FormatModifier(gearBonus)}");
                                ImGui.TextColored(ImGuiColors.ParsedGreen, $"• Effective Max: {effectiveMax}");
                                ImGui.EndTooltip();
                            }
                        }
                    }

                    // Initiative Quick Roll if configured
                    if (currentDiceSystem != null && currentDiceSystem.InitiativeStatType != InitiativeStatType.None)
                    {
                        int initMod = currentCharacter.GetInitiativeModifier(currentDiceSystem);
                        ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);
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
                        var advWidth = ImGui.CalcTextSize(advLabel).X + 30.0f * ImGuiHelpers.GlobalScale;
                        var disadvWidth = ImGui.CalcTextSize(disadvLabel).X + 30.0f * ImGuiHelpers.GlobalScale;
                        var totalAdvWidth = advWidth + disadvWidth + 8.0f * ImGuiHelpers.GlobalScale;
                        var rightAdvX = ImGui.GetWindowContentRegionMax().X - totalAdvWidth;

                        if (ImGui.GetCursorPosX() < rightAdvX)
                            ImGui.SameLine(rightAdvX);
                        else
                            ImGui.SameLine(0, 12.0f * ImGuiHelpers.GlobalScale);

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
                }
            }
        }

        private void DrawColumnsSection()
        {
            if (currentCharacter == null) return;

            var availHeight = Math.Max(240.0f * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - 4.0f);

            using (var table = ImRaii.Table("##StatsColumnsGrid", 3, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.BordersInnerV))
            {
                if (table.Success)
                {
                    ImGui.TableNextColumn();
                    DrawAttributesColumn(availHeight);

                    ImGui.TableNextColumn();
                    DrawSkillsColumn(availHeight);

                    ImGui.TableNextColumn();
                    DrawAbilitiesColumn(availHeight);
                }
            }
        }

        private static Vector4 GetResourceColor(string name, string? colorHex = null)
        {
            if (!string.IsNullOrWhiteSpace(colorHex) && colorHex.StartsWith("#") && colorHex.Length >= 7)
            {
                try
                {
                    byte r = Convert.ToByte(colorHex.Substring(1, 2), 16);
                    byte g = Convert.ToByte(colorHex.Substring(3, 2), 16);
                    byte b = Convert.ToByte(colorHex.Substring(5, 2), 16);
                    return new Vector4(r / 255f, g / 255f, b / 255f, 0.85f);
                }
                catch { }
            }

            return name.ToLowerInvariant() switch
            {
                "health" or "hp" or "vie" or "santé" => new Vector4(0.2f, 0.7f, 0.3f, 0.85f),
                "mana" or "mp" => new Vector4(0.2f, 0.45f, 0.85f, 0.85f),
                "stamina" or "endurance" or "energy" => new Vector4(0.85f, 0.60f, 0.15f, 0.85f),
                "rage" => new Vector4(0.85f, 0.20f, 0.20f, 0.85f),
                "focus" or "sanity" => new Vector4(0.60f, 0.25f, 0.85f, 0.85f),
                _ => new Vector4(0.25f, 0.65f, 0.65f, 0.85f)
            };
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
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGold, FontAwesomeIcon.ShieldAlt.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("AttributeLabel"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge((currentCharacter.characterAttributes?.Count ?? 0).ToString(), new Vector4(0.35f, 0.28f, 0.12f, 0.5f), ImGuiColors.ParsedGold);

                    var addBtnWidth = 24.0f * ImGuiHelpers.GlobalScale;
                    var rightX = ImGui.GetWindowContentRegionMax().X - addBtnWidth;
                    if (ImGui.GetCursorPosX() < rightX)
                        ImGui.SameLine(rightX);
                    else
                        ImGui.SameLine();

                    if (UiUtils.IconButton("AddAttrBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddButton"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                    {
                        newAttributeName = "";
                        newAttributeValue = 0;
                        showAttributesPopup = true;
                    }

                    ImGui.Separator();
                    ImGui.Spacing();

                    if (currentCharacter.characterAttributes == null || currentCharacter.characterAttributes.Count == 0)
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
                        string? attrToRemove = null;
                        var availWidth = ImGui.GetContentRegionAvail().X;

                        foreach (KeyValuePair<string, Datamodels.Attribute> attribute in currentCharacter.characterAttributes)
                        {
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

                            if (editingStats)
                            {
                                if (UiUtils.IconButton($"Del_{attribute.Key}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    attrToRemove = attribute.Key;
                                }
                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, attribute.Key);

                                int inputCount = 1 + (hasBonusTemp ? 1 : 0) + (hasBonusPerm ? 1 : 0) + (showEpic ? 1 : 0);
                                var inputAreaWidth = (inputCount * 40.0f + 10.0f) * ImGuiHelpers.GlobalScale;
                                var rightInputX = pos.X + availWidth - inputAreaWidth;
                                if (ImGui.GetCursorScreenPos().X < rightInputX)
                                {
                                    ImGui.SetCursorScreenPos(new Vector2(rightInputX, pos.Y + (cardHeight - 22.0f * ImGuiHelpers.GlobalScale) * 0.5f));
                                }

                                ImGui.SetNextItemWidth(36.0f * ImGuiHelpers.GlobalScale);
                                ImGui.InputInt($"##Val_{attribute.Key}", ref attribute.Value.Value, 0);
                                if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("StatValueTooltip"));

                                if (hasBonusTemp)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    ImGui.SetNextItemWidth(36.0f * ImGuiHelpers.GlobalScale);
                                    ImGui.InputInt($"##Temp_{attribute.Key}", ref attribute.Value.TempBonus, 0);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("StatTempTooltip"));
                                }

                                if (hasBonusPerm)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    ImGui.SetNextItemWidth(36.0f * ImGuiHelpers.GlobalScale);
                                    ImGui.InputInt($"##Perm_{attribute.Key}", ref attribute.Value.PermBonus, 0);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("StatPermTooltip"));
                                }

                                if (showEpic)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    ImGui.SetNextItemWidth(36.0f * ImGuiHelpers.GlobalScale);
                                    ImGui.InputInt($"##Epic_{attribute.Key}", ref attribute.Value.EpicBonus, 0);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip(LocalizationManager.Instance.GetLocalizedString("StatEpicTooltip"));
                                }
                            }
                            else
                            {
                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, attribute.Key);

                                int baseVal = attribute.Value.Value;
                                int tempVal = hasBonusTemp ? attribute.Value.TempBonus : 0;
                                int permVal = hasBonusPerm ? attribute.Value.PermBonus : 0;
                                int gearBonus = currentCharacter.GetGearStatBonus(attribute.Key);
                                int buffBonus = currentCharacter.GetBuffStatBonus(attribute.Key);
                                int epicVal = showEpic ? attribute.Value.EpicBonus : 0;
                                int totalVal = baseVal + tempVal + permVal + gearBonus + buffBonus;

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

                                if (epicVal > 0)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge($"★{epicVal}", new Vector4(0.30f, 0.15f, 0.40f, 0.85f), ImGuiColors.DalamudViolet);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("StatEpicTooltip")}: ★{epicVal}");
                                }

                                if (hasSaves)
                                {
                                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                    if (UiUtils.IconButton($"SaveRoll_{attribute.Key}", FontAwesomeIcon.ShieldAlt, $"{LocalizationManager.Instance.GetLocalizedString("SavingThrowButton")} {attribute.Key}", new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                                    {
                                        int totalDice = totalVal;
                                        int totalModifier = totalVal;
                                        int totalTarget = totalVal;
                                        int rawSuccesses = epicVal;
                                        string rollLabel = string.Format(LocalizationManager.Instance.GetLocalizedString("SavingThrowRollFormat"), attribute.Key);
                                        DiceRoll.RollDice(totalDice, totalModifier, advantageRoll, disadvantageRoll, rollLabel, detailedRoll, totalTarget, rawSuccesses);
                                    }
                                }

                                ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"Roll_{attribute.Key}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {attribute.Key}", new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    int totalDice = totalVal;
                                    int totalModifier = totalVal;
                                    int totalTarget = totalVal;
                                    int rawSuccesses = epicVal;
                                    DiceRoll.RollDice(totalDice, totalModifier, advantageRoll, disadvantageRoll, attribute.Key, detailedRoll, totalTarget, rawSuccesses);
                                }

                                if (isHovered && !ImGui.IsAnyItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.TextColored(ImGuiColors.ParsedGold, attribute.Key);
                                    ImGui.Separator();
                                    ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("StatValueTooltip")}: {baseVal}");
                                    if (tempVal != 0)
                                        ImGui.Text($"{LocalizationManager.Instance.GetLocalizedString("StatTempTooltip")}: {FormatModifier(tempVal)}");
                                    if (gearBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedBlue, $"{LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(gearBonus)}");
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

                        if (attrToRemove != null)
                        {
                            currentCharacter.characterAttributes.Remove(attrToRemove);
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
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.ParsedGreen, FontAwesomeIcon.Book.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("SkillLabel"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge((currentCharacter.characterSkills?.Count ?? 0).ToString(), new Vector4(0.15f, 0.35f, 0.2f, 0.5f), ImGuiColors.ParsedGreen);

                    var addBtnWidth = 24.0f * ImGuiHelpers.GlobalScale;
                    var rightX = ImGui.GetWindowContentRegionMax().X - addBtnWidth;
                    if (ImGui.GetCursorPosX() < rightX)
                        ImGui.SameLine(rightX);
                    else
                        ImGui.SameLine();

                    if (UiUtils.IconButton("AddSkillBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddButton"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                    {
                        newSkillName = "";
                        newSkillValue = 0;
                        selectedAttribute = currentCharacter.characterAttributes?.Keys.FirstOrDefault() ?? "";
                        showSkillPopup = true;
                    }

                    ImGui.Separator();
                    ImGui.Spacing();

                    if (currentCharacter.characterSkills == null || currentCharacter.characterSkills.Count == 0)
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
                        string? skillToRemove = null;
                        var availWidth = ImGui.GetContentRegionAvail().X;

                        foreach (KeyValuePair<string, Skill> skill in currentCharacter.characterSkills)
                        {
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
                            bool hasLinkedAttr = !string.IsNullOrEmpty(skill.Value.linkedAttribute) &&
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
                            int attrGearBonus = hasLinkedAttr ? currentCharacter.GetGearStatBonus(skill.Value.linkedAttribute) : 0;
                            int attrBuffBonus = hasLinkedAttr ? currentCharacter.GetBuffStatBonus(skill.Value.linkedAttribute) : 0;
                            int effectiveAttrVal = attributeValue + attributeTemp + attributePerm + attrGearBonus + attrBuffBonus;
                            int totalModifier = skill.Value.skillModifier + skillGearBonus + skillBuffBonus + (hasLinkedAttr ? effectiveAttrVal : 0);

                            if (editingStats)
                            {
                                if (UiUtils.IconButton($"Del_{skill.Key}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    skillToRemove = skill.Key;
                                }
                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);

                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, skill.Value.skillName);
                                if (!string.IsNullOrEmpty(skill.Value.linkedAttribute))
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(skill.Value.linkedAttribute, new Vector4(0.28f, 0.22f, 0.12f, 0.6f), ImGuiColors.ParsedGold);
                                }

                                var rightInputX = pos.X + availWidth - 45.0f * ImGuiHelpers.GlobalScale;
                                if (ImGui.GetCursorScreenPos().X < rightInputX)
                                {
                                    ImGui.SetCursorScreenPos(new Vector2(rightInputX, pos.Y + (cardHeight - 22.0f * ImGuiHelpers.GlobalScale) * 0.5f));
                                }
                                ImGui.SetNextItemWidth(36.0f * ImGuiHelpers.GlobalScale);
                                ImGui.InputInt($"##SkillVal_{skill.Key}", ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterSkills, skill.Key).skillModifier, 0);
                            }
                            else
                            {
                                ImGui.AlignTextToFramePadding();
                                ImGui.TextColored(ImGuiColors.DalamudWhite, skill.Value.skillName);

                                if (!string.IsNullOrEmpty(skill.Value.linkedAttribute))
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

                                string? totalModText = (hasLinkedAttr || skillGearBonus != 0 || skillBuffBonus != 0) ? FormatModifier(totalModifier) : null;
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

                                if (totalModText != null)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(totalModText, new Vector4(0.14f, 0.38f, 0.20f, 0.85f), ImGuiColors.ParsedGreen);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")}: {totalModText}");
                                }

                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"Roll_{skill.Value.skillName}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {skill.Value.skillName}", new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    if (currentDiceSystem != null)
                                    {
                                        int totalDice = totalModifier;
                                        int totalTarget = totalModifier;
                                        DiceRoll.RollDice(totalDice, totalModifier, advantageRoll, disadvantageRoll, skill.Value.skillName, detailedRoll, totalTarget, rawSuccesses);
                                    }
                                }

                                if (isHovered && !ImGui.IsAnyItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.TextColored(ImGuiColors.ParsedGreen, skill.Value.skillName);
                                    ImGui.Separator();
                                    ImGui.Text($"• {LocalizationManager.Instance.GetLocalizedString("NewSkillValue")}: {baseModText}");
                                    if (skillGearBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(skillGearBonus)}");
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

                        if (skillToRemove != null)
                        {
                            currentCharacter.characterSkills.Remove(skillToRemove);
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
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(ImGuiColors.TankBlue, FontAwesomeIcon.Bolt.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    ImGui.TextColored(ImGuiColors.TankBlue, LocalizationManager.Instance.GetLocalizedString("AbilityLabel"));
                    ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                    UiUtils.Badge((currentCharacter.characterAbilities?.Count ?? 0).ToString(), new Vector4(0.15f, 0.25f, 0.45f, 0.5f), ImGuiColors.TankBlue);

                    var addBtnWidth = 24.0f * ImGuiHelpers.GlobalScale;
                    var rightX = ImGui.GetWindowContentRegionMax().X - addBtnWidth;
                    if (ImGui.GetCursorPosX() < rightX)
                        ImGui.SameLine(rightX);
                    else
                        ImGui.SameLine();

                    if (UiUtils.IconButton("AddAbilityBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddButton"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                    {
                        newAbilityName = "";
                        newAbilityValue = 0;
                        selectedAttribute = currentCharacter.characterAttributes?.Keys.FirstOrDefault() ?? "";
                        selectedSkill = currentCharacter.characterSkills?.Keys.FirstOrDefault() ?? "";
                        showAbilitiesPopup = true;
                    }

                    ImGui.Separator();
                    ImGui.Spacing();

                    if (currentCharacter.characterAbilities == null || currentCharacter.characterAbilities.Count == 0)
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
                        string? abilityToRemove = null;
                        var availWidth = ImGui.GetContentRegionAvail().X;

                        foreach (KeyValuePair<string, Ability> ability in currentCharacter.characterAbilities)
                        {
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
                            int attrGearBonus = hasLinkedAttr ? currentCharacter.GetGearStatBonus(ability.Value.linkedAttribute) : 0;
                            int attrBuffBonus = hasLinkedAttr ? currentCharacter.GetBuffStatBonus(ability.Value.linkedAttribute) : 0;
                            int skillValue = ability.Value.linkedSkill != null ? ability.Value.linkedSkill.skillModifier : 0;
                            int skillGearBonus = 0;
                            int skillBuffBonus = 0;
                            bool hasLinkedSkill = ability.Value.linkedSkill != null && !string.IsNullOrEmpty(ability.Value.linkedSkill.skillName);
                            if (hasLinkedSkill && ability.Value.linkedSkill != null)
                            {
                                skillGearBonus = currentCharacter.GetGearStatBonus(ability.Value.linkedSkill.skillName);
                                skillBuffBonus = currentCharacter.GetBuffStatBonus(ability.Value.linkedSkill.skillName);
                            }

                            int effectiveAttrValue = attributeValue + attributeTemp + attributePerm + attrGearBonus + attrBuffBonus;
                            int effectiveSkillValue = skillValue + skillGearBonus + skillBuffBonus;
                            int totalModifier = ability.Value.abilityModifier + abilityGearBonus + abilityBuffBonus + (hasLinkedAttr ? effectiveAttrValue : 0) + (hasLinkedSkill ? effectiveSkillValue : 0);

                            if (editingStats)
                            {
                                if (UiUtils.IconButton($"Del_{ability.Key}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
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
                                ImGui.SetNextItemWidth(36.0f * ImGuiHelpers.GlobalScale);
                                ImGui.InputInt($"##AbilityVal_{ability.Key}", ref CollectionsMarshal.GetValueRefOrNullRef(currentCharacter.characterAbilities, ability.Key).abilityModifier, 0);
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

                                string? totalModText = (hasLinkedAttr || hasLinkedSkill || abilityGearBonus != 0 || abilityBuffBonus != 0) ? FormatModifier(totalModifier) : null;
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

                                if (totalModText != null)
                                {
                                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                                    UiUtils.Badge(totalModText, new Vector4(0.16f, 0.32f, 0.55f, 0.85f), ImGuiColors.TankBlue);
                                    if (ImGui.IsItemHovered()) ImGuiEx.Tooltip($"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")}: {totalModText}");
                                }

                                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                                if (UiUtils.IconButton($"Roll_{ability.Value.abilityName}", FontAwesomeIcon.DiceD20, $"{LocalizationManager.Instance.GetLocalizedString("ThrowButton")} {ability.Value.abilityName}", new Vector2(24, 20) * ImGuiHelpers.GlobalScale))
                                {
                                    if (currentDiceSystem != null)
                                    {
                                        int totalDice = totalModifier;
                                        int totalTarget = totalModifier;
                                        DiceRoll.RollDice(totalDice, totalModifier, advantageRoll, disadvantageRoll, ability.Value.abilityName, detailedRoll, totalTarget, rawSuccesses);
                                    }
                                }

                                if (isHovered && !ImGui.IsAnyItemHovered())
                                {
                                    ImGui.BeginTooltip();
                                    ImGui.TextColored(ImGuiColors.TankBlue, ability.Value.abilityName);
                                    ImGui.Separator();
                                    ImGui.Text($"• {LocalizationManager.Instance.GetLocalizedString("NewAbilityValue")}: {baseModText}");
                                    if (abilityGearBonus != 0)
                                        ImGui.TextColored(ImGuiColors.ParsedBlue, $"• {LocalizationManager.Instance.GetLocalizedString("GearBonusTooltip")}: {FormatModifier(abilityGearBonus)}");
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

                        if (abilityToRemove != null)
                        {
                            currentCharacter.characterAbilities.Remove(abilityToRemove);
                        }
                    }
                }
            }
        }

        private void DrawModals()
        {
            if (currentCharacter == null) return;

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
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("AttributeLabel"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAttributeNameLabel"));
                ImGui.InputText("##NewAttrName", ref newAttributeName, 100);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAttributeValueLabel"));
                ImGui.InputInt("##NewAttrVal", ref newAttributeValue, 1);

                ImGui.Spacing();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
                {
                    if (!string.IsNullOrWhiteSpace(newAttributeName))
                    {
                        currentCharacter.characterAttributes ??= new Dictionary<string, Datamodels.Attribute>();
                        if (!currentCharacter.characterAttributes.ContainsKey(newAttributeName))
                        {
                            currentCharacter.characterAttributes.Add(newAttributeName, new Datamodels.Attribute(newAttributeName, newAttributeValue));
                            newAttributeName = "";
                            newAttributeValue = 0;
                            showAttributesPopup = false;
                        }
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
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
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("SkillLabel"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewSkillName"));
                ImGui.InputText("##NewSkillName", ref newSkillName, 100);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewSkillValue"));
                ImGui.InputInt("##NewSkillVal", ref newSkillValue, 1);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewLinkedAttribute"));
                var attrKeys = currentCharacter.characterAttributes?.Keys.ToList() ?? new List<string>();
                if (attrKeys.Count > 0)
                {
                    if (string.IsNullOrEmpty(selectedAttribute) || !attrKeys.Contains(selectedAttribute))
                        selectedAttribute = attrKeys[0];

                    if (ImGui.BeginCombo("##LinkedAttrCombo", selectedAttribute))
                    {
                        foreach (var key in attrKeys)
                        {
                            bool isSelected = selectedAttribute == key;
                            if (ImGui.Selectable(key, isSelected))
                            {
                                selectedAttribute = key;
                            }
                            if (isSelected) ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }
                }
                else
                {
                    ImGui.InputText("##LinkedAttrText", ref selectedAttribute, 100);
                }

                ImGui.Spacing();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
                {
                    if (!string.IsNullOrWhiteSpace(newSkillName))
                    {
                        newSkill = new Skill
                        {
                            skillName = newSkillName,
                            skillModifier = newSkillValue,
                            linkedAttribute = selectedAttribute
                        };
                        currentCharacter.characterSkills ??= new Dictionary<string, Skill>();
                        if (!currentCharacter.characterSkills.ContainsKey(newSkillName))
                        {
                            currentCharacter.characterSkills.Add(newSkillName, newSkill);
                            newSkillName = "";
                            newSkillValue = 0;
                            selectedAttribute = "";
                            showSkillPopup = false;
                        }
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
                    showSkillPopup = false;
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
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                ImGui.TextColored(ImGuiColors.TankBlue, LocalizationManager.Instance.GetLocalizedString("AbilityLabel"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAbilityName"));
                ImGui.InputText("##NewAbilityName", ref newAbilityName, 100);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewAbilityValue"));
                ImGui.InputInt("##NewAbilityVal", ref newAbilityValue, 1);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewLinkedAttribute"));
                var noneLabel = LocalizationManager.Instance.GetLocalizedString("NoneOption");
                var attrKeys = currentCharacter.characterAttributes?.Keys.ToList() ?? new List<string>();
                var attrOptions = new List<string> { "" };
                attrOptions.AddRange(attrKeys);
                if (ImGui.BeginCombo("##AbilityLinkedAttrCombo", string.IsNullOrEmpty(selectedAttribute) ? noneLabel : selectedAttribute))
                {
                    foreach (var key in attrOptions)
                    {
                        bool isSelected = selectedAttribute == key;
                        if (ImGui.Selectable(string.IsNullOrEmpty(key) ? noneLabel : key, isSelected))
                        {
                            selectedAttribute = key;
                        }
                        if (isSelected) ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("NewLinkedSkill"));
                var skillKeysList = currentCharacter.characterSkills?.Keys.ToList() ?? new List<string>();
                var skillOptions = new List<string> { "" };
                skillOptions.AddRange(skillKeysList);
                if (ImGui.BeginCombo("##AbilityLinkedSkillCombo", string.IsNullOrEmpty(selectedSkill) ? noneLabel : selectedSkill))
                {
                    foreach (var key in skillOptions)
                    {
                        bool isSelected = selectedSkill == key;
                        if (ImGui.Selectable(string.IsNullOrEmpty(key) ? noneLabel : key, isSelected))
                        {
                            selectedSkill = key;
                        }
                        if (isSelected) ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }

                ImGui.Spacing();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
                {
                    if (!string.IsNullOrWhiteSpace(newAbilityName))
                    {
                        newAbility = new Ability
                        {
                            abilityName = newAbilityName,
                            abilityModifier = newAbilityValue,
                            linkedAttribute = selectedAttribute
                        };
                        if (currentCharacter.characterSkills != null && !string.IsNullOrEmpty(selectedSkill))
                        {
                            currentCharacter.characterSkills.TryGetValue(selectedSkill, out newAbility.linkedSkill);
                        }
                        currentCharacter.characterAbilities ??= new Dictionary<string, Ability>();
                        if (!currentCharacter.characterAbilities.ContainsKey(newAbilityName))
                        {
                            currentCharacter.characterAbilities.Add(newAbilityName, newAbility);
                            newAbilityName = "";
                            newAbilityValue = 0;
                            selectedAttribute = "";
                            selectedSkill = "";
                            showAbilitiesPopup = false;
                        }
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
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
                ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("BuffModalTitle"));
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("BuffNameLabel"));
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##CharBuffName", ref newCharBuffName, 60);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("BuffDurationLabel"));
                ImGui.SetNextItemWidth(100.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputInt("##CharBuffDuration", ref newCharBuffDuration, 1))
                {
                    if (newCharBuffDuration < 1) newCharBuffDuration = 1;
                }

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("BuffTargetStatLabel"));
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##CharBuffTargetStat", LocalizationManager.Instance.GetLocalizedString("BuffStatNameHint"), ref newCharBuffTargetStat, 60);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("BuffValueLabel"));
                ImGui.SetNextItemWidth(100.0f * ImGuiHelpers.GlobalScale);
                if (ImGui.InputInt("##CharBuffValue", ref newCharBuffValue, 1))
                {
                    if (newCharBuffValue < 0) newCharBuffIsDebuff = true;
                }

                ImGui.Checkbox(LocalizationManager.Instance.GetLocalizedString("BuffIsDebuffLabel"), ref newCharBuffIsDebuff);

                ImGui.Text(LocalizationManager.Instance.GetLocalizedString("DiceSysResourceDescription"));
                ImGui.SetNextItemWidth(-1);
                ImGui.InputText("##CharBuffDesc", ref newCharBuffDesc, 120);

                ImGui.Spacing();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("AddConfirmButton"), new Vector2(100, 0) * ImGuiHelpers.GlobalScale))
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
                ImGui.SameLine();
                if (ImGui.Button(LocalizationManager.Instance.GetLocalizedString("CancelButton"), new Vector2(80, 0) * ImGuiHelpers.GlobalScale))
                {
                    showBuffPopup = false;
                }

                ImGui.EndPopup();
            }
        }
    }
}
