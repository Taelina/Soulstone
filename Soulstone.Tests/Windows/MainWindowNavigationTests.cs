using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud.Interface.Windowing;
using FluentAssertions;
using Soulstone.Windows;
using Xunit;

namespace Soulstone.Tests.Windows;

[Collection("NonParallelCollection")]
public class MainWindowNavigationTests
{
    [Fact]
    public void OpeningGroup_SelectsEmbeddedPageAndReopensMainWindow()
    {
        TestHelper.EnsureMockServices();
        var plugin = (Plugin)RuntimeHelpers.GetUninitializedObject(typeof(Plugin));
        typeof(Plugin).GetProperty(nameof(Plugin.Configuration))!.SetValue(plugin, new Soulstone.Configuration());
        using var mainWindow = new MainWindow(plugin);
        typeof(Plugin).GetProperty("MainWindow", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(plugin, mainWindow);
        typeof(Plugin).GetField("pluginInitialized", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(plugin, true);
        mainWindow.IsOpen = false;

        plugin.ToggleGroupUi();

        mainWindow.IsOpen.Should().BeTrue();
        mainWindow.IsGroupSelected.Should().BeTrue();
        plugin.ToggleGroupUi();
        mainWindow.IsOpen.Should().BeTrue("opening the Group page should keep its main window visible");
    }

    [Fact]
    public void GroupIsEmbedded_WhileInitiativeRemainsAnIndependentWindow()
    {
        typeof(Window).IsAssignableFrom(typeof(GroupWindow)).Should().BeFalse();
        typeof(Window).IsAssignableFrom(typeof(InitiativeTrackerWindow)).Should().BeTrue();
    }
}
