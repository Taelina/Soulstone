using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.UI;
using Soulstone;
using FFXIVClientStructs.FFXIV.Client.System.String;
using Dalamud.Game.Text.SeStringHandling;
using Soulstone.Utils;

namespace Soulstone.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string goatImagePath;
    private readonly Plugin plugin;

    private string testroll = "";
    private bool detailedRoll = false;
    private string rollInputText = "";

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("Soulstone##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.goatImagePath = goatImagePath;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.Spacing();
        CharacterSheet test = null;
        if (CharacterManager.Instance.CharacterSheet != null)
        {
            test = CharacterManager.Instance.CharacterSheet;
        }

        if (test != null)
        {
            ImGui.LabelText("Nom/Prénom", test.CharacterFullName);
        }
        ImGui.Spacing();

        ImGui.InputText("Manual Roll Input", ref rollInputText);
        if (ImGui.Checkbox("Detailed Roll", ref detailedRoll))
        {
            //DO THING ?
        }

        if (ImGui.Button("Roll dice"))
        {
            
            Plugin.Log.Info($"Rolling dice with input: {rollInputText}");
            DiceRoll DR = DiceRoll.ParseDiceRollString(rollInputText);
            if (DR != null)
            {
                if (!detailedRoll)
                {
                    XivChatEntry testeuh = new XivChatEntry
                    {
                        Message = DR.RollResultString,
                        Type = XivChatType.Say
                    };
                    Messages.SendMessage(testeuh);
                }
                else
                {
                    XivChatEntry testeuh = new XivChatEntry
                    {
                        Message = DR.RollDetailedResultString,
                        Type = XivChatType.Say
                    };
                    Messages.SendMessage(testeuh);
                }
                    
            }            
        }

        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        /*using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                ImGui.TextUnformatted("Have a goat:");
                var goatImage = Plugin.TextureProvider.GetFromFile(goatImagePath).GetWrapOrDefault();
                if (goatImage != null)
                {
                    using (ImRaii.PushIndent(55f))
                    {
                        ImGui.Image(goatImage.Handle, goatImage.Size);
                    }
                }
                else
                {
                    ImGui.TextUnformatted("Image not found.");
                }

                ImGuiHelpers.ScaledDummy(20.0f);

                // Example for other services that Dalamud provides.
                // ClientState provides a wrapper filled with information about the local player object and client.

                var localPlayer = Plugin.ClientState.LocalPlayer;
                if (localPlayer == null)
                {
                    ImGui.TextUnformatted("Our local player is currently not loaded.");
                    return;
                }

                if (!localPlayer.ClassJob.IsValid)
                {
                    ImGui.TextUnformatted("Our current job is currently not valid.");
                    return;
                }

                // If you want to see the Macro representation of this SeString use `ToMacroString()`
                ImGui.TextUnformatted($"Our current job is ({localPlayer.ClassJob.RowId}) \"{localPlayer.ClassJob.Value.Abbreviation}\"");

                // Example for quarrying Lumina directly, getting the name of our current area.
                var territoryId = Plugin.ClientState.TerritoryType;
                if (Plugin.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
                {
                    ImGui.TextUnformatted($"We are currently in ({territoryId}) \"{territoryRow.PlaceName.Value.Name}\"");
                }
                else
                {
                    ImGui.TextUnformatted("Invalid territory.");
                }
            }
        }*/
    }
}
