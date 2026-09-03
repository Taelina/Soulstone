using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Soulstone.Datamodels;
using Soulstone.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Utils
{
    internal class UiUtils
    {
        private static float defaultNextToSpace = 3.0f;
        private static float defaultFieldSpacing = 10.0f;
        private static float defaultInputWidth = 175.0f;

        public static float DefaultInputWidth { get => defaultInputWidth * ImGuiHelpers.GlobalScale; set => defaultInputWidth = value; }
        public static float DefaultFieldSpacing { get => defaultFieldSpacing * ImGuiHelpers.GlobalScale; set => defaultFieldSpacing = value; }
        public static float DefaultNextToSpace { get => defaultNextToSpace * ImGuiHelpers.GlobalScale; set => defaultNextToSpace = value; }

        public static void ManageInputField(ref string field, string fieldname, bool editing, float width = 175.0f)
        {
            if (editing)
            {
                if (width > 0)
                    ImGui.SetNextItemWidth(width * ImGuiHelpers.GlobalScale);
                else
                    ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputText($"##{fieldname}", ref field, 200);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 0.7f), "—");
                }
                else
                {
                    ImGui.TextUnformatted(field);
                }
            }
        }

        public static void ManageInputField(ref int field, string fieldname, bool editing, float width = 50.0f)
        {
            if (editing)
            {
                if (width > 0)
                    ImGui.SetNextItemWidth(width * ImGuiHelpers.GlobalScale);
                else
                    ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputInt($"##{fieldname}", ref field, 0);
            }
            else
            {
                ImGui.TextUnformatted(field.ToString());
            }
        }

        public static void ManageBigInputField(ref string field, string fieldname, bool editing, float height = 80.0f)
        {
            if (editing)
            {
                ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputTextMultiline($"##{fieldname}", ref field, 5000, new Vector2(-1.0f, height * ImGuiHelpers.GlobalScale));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 0.7f), "—");
                }
                else
                {
                    ImGui.TextWrapped(field);
                }
            }
        }

        public static void SectionHeader(string title, Vector4? titleColor = null)
        {
            ImGui.Spacing();
            ImGui.TextColored(titleColor ?? ImGuiColors.ParsedGold, title);
            ImGui.Separator();
            ImGui.Spacing();
        }

        public static void HelpMarker(string desc)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        public static bool SmallButton(string label, string tooltip = "", Vector2? size = null, bool enabled = true)
        {
            bool clicked = false;
            if (!enabled)
            {
                ImGui.BeginDisabled();
            }
            if (size.HasValue)
            {
                clicked = ImGui.Button(label, size.Value);
            }
            else
            {
                clicked = ImGui.Button(label);
            }
            if (!enabled)
            {
                ImGui.EndDisabled();
            }
            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
            return clicked;
        }

        public static bool IconButton(string id, FontAwesomeIcon icon, string tooltip = "", Vector2? size = null, bool enabled = true)
        {
            bool clicked = false;
            if (!enabled)
            {
                ImGui.BeginDisabled();
            }

            ImGui.PushFont(UiBuilder.IconFont);
            if (size.HasValue)
            {
                clicked = ImGui.Button($"{icon.ToIconString()}###{id}", size.Value);
            }
            else
            {
                clicked = ImGui.Button($"{icon.ToIconString()}###{id}");
            }
            ImGui.PopFont();

            if (!enabled)
            {
                ImGui.EndDisabled();
            }

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }

            return clicked;
        }

        public static void Badge(string text, Vector4 bgCol, Vector4 textCol)
        {
            var padding = new Vector2(6.0f, 2.0f) * ImGuiHelpers.GlobalScale;
            var textSize = ImGui.CalcTextSize(text);
            var size = textSize + padding * 2.0f;
            var pos = ImGui.GetCursorScreenPos();

            var drawList = ImGui.GetWindowDrawList();
            var col = ImGui.ColorConvertFloat4ToU32(bgCol);
            drawList.AddRectFilled(pos, pos + size, col, 4.0f * ImGuiHelpers.GlobalScale);

            drawList.AddText(pos + padding, ImGui.ColorConvertFloat4ToU32(textCol), text);
            ImGui.Dummy(size);
        }

        public static void DrawStatModifierEditor(
            GearItem item,
            CharacterSheet? sheet,
            DiceSystem? system,
            StatModifierEditorState state,
            string idPrefix = "ModEditor")
        {
            string[] categories = new[]
            {
                LocalizationManager.Instance.GetLocalizedString("StatCategoryAttribute"),
                LocalizationManager.Instance.GetLocalizedString("StatCategorySkill"),
                LocalizationManager.Instance.GetLocalizedString("StatCategoryAbility"),
                LocalizationManager.Instance.GetLocalizedString("StatCategoryResource"),
                LocalizationManager.Instance.GetLocalizedString("StatCategoryCustom")
            };

            List<string> availableStats = new();
            if (state.SelectedCategoryIndex == 0) // Attribute
            {
                if (sheet?.characterAttributes != null && sheet.characterAttributes.Count > 0)
                {
                    availableStats = sheet.characterAttributes.Keys.ToList();
                }
            }
            else if (state.SelectedCategoryIndex == 1) // Skill
            {
                if (sheet?.characterSkills != null && sheet.characterSkills.Count > 0)
                {
                    availableStats = sheet.characterSkills.Keys.ToList();
                }
            }
            else if (state.SelectedCategoryIndex == 2) // Ability
            {
                if (sheet?.characterAbilities != null && sheet.characterAbilities.Count > 0)
                {
                    availableStats = sheet.characterAbilities.Keys.ToList();
                }
            }
            else if (state.SelectedCategoryIndex == 3) // Resource
            {
                var resList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (sheet?.characterResources != null)
                {
                    foreach (var r in sheet.characterResources.Keys) resList.Add(r);
                }
                if (system != null)
                {
                    foreach (var r in system.GetEffectiveResources()) resList.Add(r.Name);
                }
                if (resList.Count == 0)
                {
                    resList.Add("Health");
                    resList.Add("Mana");
                }
                availableStats = resList.ToList();
            }

            ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("StatModifiersLabel"));

            ImGui.SetNextItemWidth(120.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo($"##{idPrefix}_CategoryCombo", ref state.SelectedCategoryIndex, categories, categories.Length))
            {
                state.SelectedStatIndex = 0;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            string chosenStatName = string.Empty;

            if (state.SelectedCategoryIndex < 4 && availableStats.Count > 0)
            {
                if (state.SelectedStatIndex >= availableStats.Count) state.SelectedStatIndex = 0;
                ImGui.SetNextItemWidth(140.0f * ImGuiHelpers.GlobalScale);
                var statsArr = availableStats.ToArray();
                ImGui.Combo($"##{idPrefix}_StatCombo", ref state.SelectedStatIndex, statsArr, statsArr.Length);
                chosenStatName = statsArr[state.SelectedStatIndex];
            }
            else
            {
                ImGui.SetNextItemWidth(140.0f * ImGuiHelpers.GlobalScale);
                ImGui.InputTextWithHint($"##{idPrefix}_CustomStatName", LocalizationManager.Instance.GetLocalizedString("StatNameHint"), ref state.CustomStatName, 50);
                chosenStatName = state.CustomStatName.Trim();
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            ImGui.SetNextItemWidth(60.0f * ImGuiHelpers.GlobalScale);
            ImGui.InputInt($"##{idPrefix}_ModVal", ref state.ModifierValue, 0);

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.Button($"{LocalizationManager.Instance.GetLocalizedString("AddStatModifierButton")}###{idPrefix}_AddBtn"))
            {
                if (!string.IsNullOrWhiteSpace(chosenStatName))
                {
                    item.SetStatModifier(chosenStatName, state.ModifierValue);
                    state.CustomStatName = string.Empty;
                    state.ModifierValue = 1;
                }
            }

            if (item.StatModifiers != null && item.StatModifiers.Count > 0)
            {
                ImGui.Spacing();
                string? modToRemove = null;
                foreach (var mod in item.StatModifiers)
                {
                    var modCol = mod.Value >= 0 ? ImGuiColors.ParsedBlue : ImGuiColors.DalamudRed;
                    var modBg = mod.Value >= 0 ? new Vector4(0.12f, 0.22f, 0.38f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                    string modText = $"{(mod.Value >= 0 ? "+" : "")}{mod.Value} {mod.Key}";
                    Badge(modText, modBg, modCol);
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    if (IconButton($"{idPrefix}_DelMod_{mod.Key}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(16, 16) * ImGuiHelpers.GlobalScale))
                    {
                        modToRemove = mod.Key;
                    }
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                }
                ImGui.NewLine();

                if (modToRemove != null)
                {
                    item.RemoveStatModifier(modToRemove);
                }
            }
        }
    }

    public class StatModifierEditorState
    {
        public int SelectedCategoryIndex = 0;
        public int SelectedStatIndex = 0;
        public string CustomStatName = string.Empty;
        public int ModifierValue = 1;
    }
}
