using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Soulstone.Managers;
using Soulstone.Utils;

namespace Soulstone.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly CharacterWindow charwin;
    private readonly DiceWindow dicewin;
    private readonly CharStatsWindow statwin;
    private readonly GearWindow gearwin;
    private readonly AugmentationsWindow augwin;
    private readonly InventoryWindow invwin;
    private readonly DiceSystemWindow dicesyswin;
    private readonly Configuration configuration;

    public MainWindow(Plugin plugin)
        : base("Soulstone###SoulstoneMainWin", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(750, 620);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(480, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        this.plugin = plugin;
        this.charwin = new CharacterWindow(plugin);
        this.dicewin = new DiceWindow(plugin);
        this.statwin = new CharStatsWindow(plugin);
        this.gearwin = new GearWindow(plugin);
        this.augwin = new AugmentationsWindow(plugin);
        this.invwin = new InventoryWindow(plugin);
        this.dicesyswin = new DiceSystemWindow(plugin);
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        DrawHeader();

        using var tabs = ImRaii.TabBar("SoulstoneTabs", ImGuiTabBarFlags.FittingPolicyScroll);
        if (tabs.Success)
        {
            var rpTitle = $"{LocalizationManager.Instance.GetLocalizedString("RPTab")}###RPSheet";
            if (ImGui.BeginTabItem(rpTitle))
            {
                using (var child = ImRaii.Child("##RPTabContent", new Vector2(0, 0), false))
                {
                    if (child.Success)
                    {
                        charwin.DrawCharTab();
                    }
                }
                ImGui.EndTabItem();
            }

            var diceTitle = $"{LocalizationManager.Instance.GetLocalizedString("DiceRollTab")}###DiceSheet";
            if (ImGui.BeginTabItem(diceTitle))
            {
                using (var child = ImRaii.Child("##DiceTabContent", new Vector2(0, 0), false))
                {
                    if (child.Success)
                    {
                        dicewin.DrawDiceTab();
                    }
                }
                ImGui.EndTabItem();
            }

            var statTitle = $"{LocalizationManager.Instance.GetLocalizedString("StatSheetTab")}###StatSheet";
            if (ImGui.BeginTabItem(statTitle))
            {
                using (var child = ImRaii.Child("##StatTabContent", new Vector2(0, 0), false))
                {
                    if (child.Success)
                    {
                        statwin.DrawCharStats();
                    }
                }
                ImGui.EndTabItem();
            }

            var gearTitle = $"{LocalizationManager.Instance.GetLocalizedString("GearTab")}###GearSheet";
            if (ImGui.BeginTabItem(gearTitle))
            {
                using (var child = ImRaii.Child("##GearTabContent", new Vector2(0, 0), false))
                {
                    if (child.Success)
                    {
                        gearwin.DrawGearTab();
                    }
                }
                ImGui.EndTabItem();
            }

            var currentDiceSys = DiceSystemManager.Instance.CurrentDiceSystem;
            if (currentDiceSys?.systemHasAugmentations == true)
            {
                var augTabTitle = !string.IsNullOrWhiteSpace(currentDiceSys.AugmentationTitle)
                    ? $"{currentDiceSys.AugmentationTitle}###AugmentationsSheet"
                    : $"{LocalizationManager.Instance.GetLocalizedString("AugmentationTab")}###AugmentationsSheet";
                if (ImGui.BeginTabItem(augTabTitle))
                {
                    using (var child = ImRaii.Child("##AugTabContent", new Vector2(0, 0), false))
                    {
                        if (child.Success)
                        {
                            augwin.DrawAugmentationsTab();
                        }
                    }
                    ImGui.EndTabItem();
                }
            }

            var invTitle = $"{LocalizationManager.Instance.GetLocalizedString("InventoryTab")}###InventorySheet";
            if (ImGui.BeginTabItem(invTitle))
            {
                using (var child = ImRaii.Child("##InventoryTabContent", new Vector2(0, 0), false))
                {
                    if (child.Success)
                    {
                        invwin.DrawInventoryTab();
                    }
                }
                ImGui.EndTabItem();
            }

            var sysTitle = $"{LocalizationManager.Instance.GetLocalizedString("DiceSystemTab")}###DiceSystem";
            if (ImGui.BeginTabItem(sysTitle))
            {
                using (var child = ImRaii.Child("##DiceSystemTabContent", new Vector2(0, 0), false))
                {
                    if (child.Success)
                    {
                        dicesyswin.DrawDiceSystemTab();
                    }
                }
                ImGui.EndTabItem();
            }
        }
    }

    private void DrawHeader()
    {
        // Branded Header Title
        ImGui.TextColored(ImGuiColors.ParsedGold, "Soulstone");

        if (CharacterManager.Instance.CharacterSheet != null && !string.IsNullOrWhiteSpace(CharacterManager.Instance.CharacterSheet.CharacterFullName))
        {
            ImGui.SameLine();
            ImGui.TextDisabled("|");
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudWhite, CharacterManager.Instance.CharacterSheet.CharacterFullName);
        }

        // Right-aligned settings and initiative buttons
        var initLabel = LocalizationManager.Instance.GetLocalizedString("InitiativeOpenTracker");
        var configLabel = LocalizationManager.Instance.GetLocalizedString("ConfigButton");
        var initBtnWidth = ImGui.CalcTextSize(initLabel).X + 28.0f * ImGuiHelpers.GlobalScale;
        var configBtnWidth = ImGui.CalcTextSize(configLabel).X + 20.0f * ImGuiHelpers.GlobalScale;
        var totalButtonsWidth = initBtnWidth + configBtnWidth + 8.0f * ImGuiHelpers.GlobalScale;

        var rightX = ImGui.GetWindowContentRegionMax().X - totalButtonsWidth;
        if (ImGui.GetCursorPosX() < rightX)
        {
            ImGui.SameLine(rightX);
        }
        else
        {
            ImGui.SameLine();
        }

        if (UiUtils.IconButton("OpenInitTrackerBtn", FontAwesomeIcon.Stopwatch, initLabel))
        {
            plugin.ToggleInitiativeTrackerUi();
        }

        ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.Button($"{configLabel}###SettingsBtn"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.Separator();
        ImGui.Spacing();
    }
}
