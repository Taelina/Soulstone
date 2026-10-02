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
        Size = new Vector2(520, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320, 200),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
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
        using var sectionPanel = SoulstoneTheme.BeginPanel("##Panel_DrawRollSettings", LocalizationManager.Instance.GetLocalizedString("ConfigRollDisplayHeader"), FontAwesomeIcon.DiceD20);
        if (sectionPanel.Success)
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

            bool showRollPresentation = configuration.ShowRollPresentation;
            if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("ConfigRollPresentationCheck")}##RollPresentation", ref showRollPresentation))
            {
                configuration.ShowRollPresentation = showRollPresentation;
                configuration.Save();
            }
        }
    }

    private void DrawGroupSettings()
    {
        using var sectionPanel = SoulstoneTheme.BeginPanel("##Panel_DrawGroupSettings", LocalizationManager.Instance.GetLocalizedString("ConfigGroupManagementHeader"), FontAwesomeIcon.Users);
        if (sectionPanel.Success)
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
        using var sectionPanel = SoulstoneTheme.BeginPanel("##Panel_DrawLocalizationSettings", LocalizationManager.Instance.GetLocalizedString("ConfigLocalizationHeader"), FontAwesomeIcon.Language);
        if (sectionPanel.Success)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(SoulstoneTheme.Muted, LocalizationManager.Instance.GetLocalizedString("ConfigLanguageCombo"));
            ImGui.SameLine(0, 10.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.StyledCombo("##LanguageCombo", ref selectedLanguageIndex, Enum.GetNames<Language>(), icon: FontAwesomeIcon.Language, width: 150.0f))
            {
                configuration.Language = (Language)selectedLanguageIndex;
                configuration.Save();
            }
        }
    }
}
