using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace Soulstone.Windows;

public partial class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private readonly CharacterWindow charwin;
    private readonly DiceWindow dicewin;
    private readonly CharStatsWindow statwin;
    private readonly FeatsWindow featwin;
    private readonly GearWindow gearwin;
    private readonly AugmentationsWindow augwin;
    private readonly InventoryWindow invwin;
    private readonly DiceSystemWindow dicesyswin;
    private readonly Configuration configuration;

    public MainWindow(Plugin plugin)
        : base("Soulstone###SoulstoneMainWin", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(1280, 850);
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
        this.featwin = new FeatsWindow(plugin);
        this.gearwin = new GearWindow(plugin);
        this.augwin = new AugmentationsWindow(plugin);
        this.invwin = new InventoryWindow(plugin);
        this.dicesyswin = new DiceSystemWindow(plugin);
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var scale = ImGuiHelpers.GlobalScale;
        var compact = ImGui.GetContentRegionAvail().X < 800 * scale;
        var sidebarWidth = (compact ? 54 : 220) * scale;
        DrawSidebar(sidebarWidth, compact);
        ImGui.SameLine(0, 12 * scale);
        using var content = ImRaii.Child($"##MainContent_{selectedSection}", Vector2.Zero, false);
        if (content.Success) DrawSelectedSection();
    }

}
