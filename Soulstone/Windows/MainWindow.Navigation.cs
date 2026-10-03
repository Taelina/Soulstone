using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using Soulstone.Managers;
using Soulstone.Utils;

namespace Soulstone.Windows;

public partial class MainWindow
{
    private enum MainSection { Rp, Dice, Stats, Feats, Gear, Augmentations, Inventory, DiceSystem, Group }
    private MainSection selectedSection = MainSection.Rp;
    private static readonly Vector4 NavigationGold = new(0.90f, 0.75f, 0.43f, 1);
    internal bool IsGroupSelected => selectedSection == MainSection.Group;

    public void OpenGroup()
    {
        selectedSection = MainSection.Group;
        IsOpen = true;
    }

    private void DrawSidebar(float width, bool compact)
    {
        var scale = ImGuiHelpers.GlobalScale;
        using var background = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.045f, 0.057f, 0.075f, 1));
        using var border = ImRaii.PushColor(ImGuiCol.Border, NavigationGold with { W = 0.45f });
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 7 * scale);
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(8, 12) * scale);
        using var sidebar = ImRaii.Child("##MainNavigation", new Vector2(width, 0), true);
        if (!sidebar.Success) return;

        SoulstoneBrand.DrawIcon((compact ? 28 : 36) * scale);
        if (!compact)
        {
            ImGui.SameLine(0, 10 * scale);
            ImGui.TextColored(NavigationGold, "SOULSTONE");
        }
        ImGui.Spacing();
        DrawNavigationHeading("NavigationCharacter", compact);
        DrawSectionButton(MainSection.Rp, "RPTab", FontAwesomeIcon.User, compact);
        DrawSectionButton(MainSection.Stats, "StatSheetTab", FontAwesomeIcon.Crown, compact);
        DrawSectionButton(MainSection.Feats, "FeatTab", FontAwesomeIcon.TheaterMasks, compact);
        DrawSectionButton(MainSection.Gear, "GearTab", FontAwesomeIcon.ShieldAlt, compact);
        var system = DiceSystemManager.Instance.CurrentDiceSystem;
        if (system?.systemHasAugmentations == true)
        {
            var title = string.IsNullOrWhiteSpace(system.AugmentationTitle)
                ? LocalizationManager.Instance.GetLocalizedString("AugmentationTab") : system.AugmentationTitle;
            if (DrawNavigationButton("Augmentations", title, FontAwesomeIcon.Cogs,
                    selectedSection == MainSection.Augmentations, compact))
                selectedSection = MainSection.Augmentations;
        }
        else if (selectedSection == MainSection.Augmentations)
            selectedSection = MainSection.Rp;
        DrawSectionButton(MainSection.Inventory, "InventoryTab", FontAwesomeIcon.ShoppingBag, compact);

        DrawNavigationHeading("NavigationTools", compact);
        DrawSectionButton(MainSection.Dice, "DiceRollTab", FontAwesomeIcon.DiceD20, compact);
        DrawSectionButton(MainSection.DiceSystem, "DiceSystemTab", FontAwesomeIcon.Cog, compact);
        DrawSectionButton(MainSection.Group, "GroupOpenWindow", FontAwesomeIcon.Users, compact);
        if (DrawNavigationButton("Initiative", LocalizationManager.Instance.GetLocalizedString("InitiativeOpenTracker"), FontAwesomeIcon.Stopwatch, false, compact))
            plugin.ToggleInitiativeTrackerUi();

        DrawNavigationHeading("NavigationSettings", compact);
        if (DrawNavigationButton("Settings", LocalizationManager.Instance.GetLocalizedString("ConfigButton"), FontAwesomeIcon.Cog, false, compact))
            plugin.ToggleConfigUi();

        DrawNavigationHeading("SupportUsHere", compact);
        if (DrawNavigationButton("KoFi", "Ko-fi", FontAwesomeIcon.Coffee, false, compact))
            Util.OpenLink("https://ko-fi.com/taelina");
        if (DrawNavigationButton("Patreon", "Patreon", FontAwesomeIcon.Heart, false, compact))
            Util.OpenLink("https://www.patreon.com/Taelina");
    }

    private static void DrawNavigationHeading(string key, bool compact)
    {
        ImGui.Spacing();
        if (!compact)
            ImGui.TextColored(new Vector4(0.61f, 0.65f, 0.72f, 1), LocalizationManager.Instance.GetLocalizedString(key));
        ImGui.Separator();
        ImGui.Spacing();
    }

    private void DrawSectionButton(MainSection section, string key, FontAwesomeIcon icon, bool compact)
    {
        if (DrawNavigationButton(section.ToString(), LocalizationManager.Instance.GetLocalizedString(key), icon, selectedSection == section, compact))
            selectedSection = section;
    }

    private static bool DrawNavigationButton(string id, string title, FontAwesomeIcon icon, bool selected, bool compact)
    {
        var scale = ImGuiHelpers.GlobalScale;
        var position = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        using var normal = ImRaii.PushColor(ImGuiCol.Header, new Vector4(0.36f, 0.28f, 0.13f, 0.95f));
        using var hover = ImRaii.PushColor(ImGuiCol.HeaderHovered, new Vector4(0.27f, 0.23f, 0.16f, 1));
        using var active = ImRaii.PushColor(ImGuiCol.HeaderActive, new Vector4(0.43f, 0.33f, 0.15f, 1));
        var clicked = ImGui.Selectable("##Navigation_" + id, selected, ImGuiSelectableFlags.None, new Vector2(width, 36 * scale));
        var hovered = ImGui.IsItemHovered();
        var drawList = ImGui.GetWindowDrawList();
        if (selected)
            drawList.AddRect(position, position + new Vector2(width, 36 * scale), ImGui.ColorConvertFloat4ToU32(NavigationGold), 5 * scale);
        using (ImRaii.PushFont(UiBuilder.IconFont))
            drawList.AddText(position + new Vector2(7, 9) * scale, ImGui.ColorConvertFloat4ToU32(NavigationGold), icon.ToIconString());
        if (!compact)
        {
            var label = title;
            var available = width - 43 * scale;
            if (ImGui.CalcTextSize(label).X > available)
            {
                while (label.Length > 0 && ImGui.CalcTextSize(label + "…").X > available)
                    label = label[..^1];
                label += "…";
            }
            drawList.AddText(position + new Vector2(36, 9) * scale,
                ImGui.ColorConvertFloat4ToU32(selected ? NavigationGold : new Vector4(0.80f, 0.83f, 0.88f, 1)), label);
        }
        if (hovered) UiUtils.SetTooltip(title);
        return clicked;
    }

    private void DrawSelectedSection()
    {
        switch (selectedSection)
        {
            case MainSection.Rp: charwin.DrawCharTab(); break;
            case MainSection.Dice: dicewin.DrawDiceTab(); break;
            case MainSection.Stats: statwin.DrawCharStats(); break;
            case MainSection.Feats: featwin.DrawFeatsTab(); break;
            case MainSection.Gear: gearwin.DrawGearTab(); break;
            case MainSection.Augmentations: augwin.DrawAugmentationsTab(); break;
            case MainSection.Inventory: invwin.DrawInventoryTab(); break;
            case MainSection.DiceSystem: dicesyswin.DrawDiceSystemTab(); break;
            case MainSection.Group: plugin.GroupWindow.Draw(); break;
        }
    }
}
