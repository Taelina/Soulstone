using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Soulstone.Windows;
using System.IO;
using Dalamud.Plugin.Ipc;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text.SeStringHandling;
using Soulstone.Managers;
using System;
using Soulstone.Utils;

namespace Soulstone;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; set; } = null!;
    [PluginService] internal static IClientState ClientState { get; set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; set; } = null!;
    [PluginService] internal static IPluginLog Log { get; set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; set; } = null!;
    [PluginService] internal static IToastGui ToastGui { get; set; } = null!;
    [PluginService] internal static INotificationManager NotificationManager { get; set; } = null!;
    [PluginService] internal static IPartyList PartyList { get; set; } = null!;

    [PluginService] internal static IObjectTable ObjectTable { get; set; } = null!;

    private const string CommandName = "/soulstone";

    public static string dataLocation = string.Empty;
    private Boolean pluginInitialized = false;

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("Soulstone");
    private ConfigWindow ConfigWindow { get; init; }
    public InitiativeTrackerWindow InitiativeTrackerWindow { get; init; }
    public GroupWindow GroupWindow { get; init; }

    public ImGuiFileBrowserWindow fileBrowserWindow;

    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // You might normally want to embed resources and load them from the manifest stream
        //var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);
        InitiativeTrackerWindow = new InitiativeTrackerWindow(this);
        GroupWindow = new GroupWindow(this);
        fileBrowserWindow = new ImGuiFileBrowserWindow();
        fileBrowserWindow.SetConfiguration(Configuration);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(InitiativeTrackerWindow);
        WindowSystem.AddWindow(GroupWindow);
        WindowSystem.AddWindow(fileBrowserWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = LocalizationManager.Instance.GetLocalizedString("PluginCommandHelp")
        });

        // Tell the UI system that we want our windows to be drawn throught he window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;

        // This adds a button to the plugin installer entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anythign during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();
        InitiativeTrackerWindow.Dispose();
        GroupWindow.Dispose();
        PartySyncManager.Instance.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        dataLocation = PluginInterface.GetPluginLocDirectory();
        InitManagers();

        string trimmedArgs = (args ?? string.Empty).Trim().ToLower();
        if (trimmedArgs == "init" || trimmedArgs == "initiative")
        {
            ToggleInitiativeTrackerUi();
        }
        else if (trimmedArgs == "group" || trimmedArgs == "party")
        {
            ToggleGroupUi();
        }
        else
        {
            // In response to the slash command, toggle the display status of our main ui
            MainWindow.Toggle();
            Log.Information($"Data location: {dataLocation}");
        }
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleInitiativeTrackerUi() => InitiativeTrackerWindow.Toggle();
    public void ToggleGroupUi() => GroupWindow.Toggle();
    public void ToggleMainUi()
    {
        MainWindow.Toggle();
        dataLocation = PluginInterface.GetPluginLocDirectory();
        InitManagers();
    }

    public void InitManagers()
    {
        CharacterManager.Instance.Init();
        DiceSystemManager.Instance.Init();
        PartySyncManager.Instance.Init();
        if (!pluginInitialized)
        {
            pluginInitialized = true;
            Log.Information("Initializing managers...");
            LocalizationManager.Instance.InitLoc(this);
            fileBrowserWindow.SetCurrentDirectory(dataLocation);
        }
    }

    public void OpenFilePicker(string title, string filter, Action<string> onFileSelected, string? startDirectory = null)
    {
        // Use ImGui file browser
        if (fileBrowserWindow != null)
        {
            fileBrowserWindow.OnFileSelected = onFileSelected;
            fileBrowserWindow.Open(title, filter, startDirectory);
        }
    }
}
