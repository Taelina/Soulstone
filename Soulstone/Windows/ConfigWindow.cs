using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Soulstone.Localizations;
using Soulstone.Datamodels;
using System;
using System.Numerics;
using Soulstone.Managers;

namespace Soulstone.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;

    public int selectedLanguageIndex = 0;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin) : base("SoulstoneConfig###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 90);
        SizeCondition = ImGuiCond.Always;

        configuration = plugin.Configuration;
        selectedLanguageIndex = (int)configuration.Language;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
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
        // Can't ref a property, so use a local copy
        bool detailedRollsVal = configuration.detailedRolls;
        if (ImGui.Checkbox($"{LocalizationManager.Instance.GetLocalizedString("ConfigDetailedRollsCheck")}", ref detailedRollsVal))
        {
            configuration.detailedRolls = detailedRollsVal;
            // Can save immediately on change if you don't want to provide a "Save and Close" button
            configuration.Save();
        }

        ImGui.SetNextItemWidth(100.0f);
        if (ImGui.Combo($"{LocalizationManager.Instance.GetLocalizedString("ConfigLanguageCombo")}##Combo", ref selectedLanguageIndex, Enum.GetNames(typeof(Language))))
        {
            configuration.Language = (Language)selectedLanguageIndex;
            configuration.Save();
        }
    }
}
