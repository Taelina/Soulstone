using Dalamud.Configuration;
using Soulstone.Localizations;
using System;
using System.Collections.Generic;

namespace Soulstone;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool IsConfigWindowMovable { get; set; } = true;
    public bool SomePropertyToBeSavedAndWithADefault { get; set; } = true;

    public bool detailedRolls = false;
    public bool showEpicBonus = false;

    public List<string> PinnedFileBrowserPaths = new List<string>();
    public string? LastBrowserDirectory;

    public Language Language { get; set; } = Language.Français;

    public string SyncServerUrl { get; set; } = "http://127.0.0.1:5077";
    public string SyncSessionId { get; set; } = string.Empty;
    public string SyncHostToken { get; set; } = string.Empty;
    public string SyncMemberToken { get; set; } = string.Empty;
    public string SyncRoomKey { get; set; } = string.Empty;
    public string SyncHostPublicKey { get; set; } = string.Empty;
    public string SyncHostPrivateKey { get; set; } = string.Empty;
    public string SyncHostName { get; set; } = string.Empty;
    public string SyncHostWorld { get; set; } = string.Empty;
    public string SyncInviteCode { get; set; } = string.Empty;
    public bool SyncAutoConnect { get; set; } = true;

    // The below exist just to make saving less cumbersome
    public void Save()
    {
        try
        {
            Plugin.PluginInterface?.SavePluginConfig(this);
        }
        catch (Exception ex)
        {
            Plugin.Log?.Error(ex, "Failed to save configuration via PluginInterface.SavePluginConfig");
        }
    }
}
