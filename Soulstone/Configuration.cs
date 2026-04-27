using Dalamud.Configuration;
using Soulstone.Localizations;
using System;

namespace Soulstone;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    public bool detailedRolls = false;

    public Language Language { get; set; } = Language.Français;

    // The below exist just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
