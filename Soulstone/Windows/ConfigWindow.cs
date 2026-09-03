using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Soulstone.Datamodels;
using Soulstone.Localizations;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Numerics;

namespace Soulstone.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    public int selectedLanguageIndex = 0;

    public ConfigWindow(Plugin plugin) : base("Soulstone Settings###SoulstoneConfig")
    {
        Size = new Vector2(360, 240);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 200),
            MaximumSize = new Vector2(600, 450)
        };

        configuration = plugin.Configuration;
        selectedLanguageIndex = (int)configuration.Language;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        WindowName = $"{LocalizationManager.Instance.GetLocalizedString("ConfigWindowTitle")}###SoulstoneConfig";

        if (configuration.IsConfigWindowMovable)
        {
            Flags &= ~ImGuiWindowFlags.NoMove;
        }
        else
        {
            Flags |= ImGuiWindowFlags.NoMove;
        }
    }

    public override void Draw()
    {
        DrawRollSettings();
        ImGui.Spacing();
        DrawLocalizationSettings();
    }

    private void DrawRollSettings()
    {
        var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("ConfigRollDisplayHeader")}###RollDisplayHeader", flags))
        {
            bool detailedRollsVal = configuration.detailedRolls;
            if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("ConfigDetailedRollsCheck")}##DetailedRolls", ref detailedRollsVal))
            {
                configuration.detailedRolls = detailedRollsVal;
                configuration.Save();
            }

            bool showEpicBonusVal = configuration.showEpicBonus;
            if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("ConfigEpicBonusCheck")}##EpicBonus", ref showEpicBonusVal))
            {
                configuration.showEpicBonus = showEpicBonusVal;
                configuration.Save();
            }
        }
    }

    private void DrawLocalizationSettings()
    {
        var flags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (ImGui.CollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("ConfigLocalizationHeader")}###LocalizationHeader", flags))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("ConfigLanguageCombo"));
            ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
            ImGui.SetNextItemWidth(150.0f * ImGuiHelpers.GlobalScale);
            if (ImGui.Combo("##LanguageCombo", ref selectedLanguageIndex, Enum.GetNames<Language>()))
            {
                configuration.Language = (Language)selectedLanguageIndex;
                configuration.Save();
            }
        }
    }
}
