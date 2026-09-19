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
        DrawGroupSettings();
        ImGui.Spacing();
        DrawLocalizationSettings();
    }

    private void DrawRollSettings()
    {
        if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("ConfigRollDisplayHeader"), defaultOpen: true, icon: FontAwesomeIcon.DiceD20, accentColor: ImGuiColors.ParsedGold))
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

    private void DrawGroupSettings()
    {
        if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("ConfigGroupManagementHeader"), defaultOpen: true, icon: FontAwesomeIcon.Users, accentColor: ImGuiColors.ParsedGreen))
        {
            bool showGroupRes = configuration.ShowGroupResources;
            if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("ConfigShowGroupResourcesCheck")}##ShowGroupRes", ref showGroupRes))
            {
                configuration.ShowGroupResources = showGroupRes;
                configuration.Save();
            }
        }
    }

    private void DrawLocalizationSettings()
    {
        if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("ConfigLocalizationHeader"), defaultOpen: true, icon: FontAwesomeIcon.Language, accentColor: ImGuiColors.ParsedBlue))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudGrey, LocalizationManager.Instance.GetLocalizedString("ConfigLanguageCombo"));
            ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.StyledCombo("##LanguageCombo", ref selectedLanguageIndex, Enum.GetNames<Language>(), icon: FontAwesomeIcon.Language, width: 150.0f))
            {
                configuration.Language = (Language)selectedLanguageIndex;
                configuration.Save();
            }
        }
    }
}
