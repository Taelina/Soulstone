using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Soulstone.Managers;
using System;
using System.Numerics;

namespace Soulstone.Utils;

public static class DeleteConfirmation
{
    private const string PopupId = "##GlobalDeleteConfirmation";
    private static Action? pendingAction;

    public static bool IsPending => pendingAction != null;

    public static void Request(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        pendingAction = action;
    }

    public static void Confirm()
    {
        var action = pendingAction;
        pendingAction = null;
        action?.Invoke();
    }

    public static void Cancel()
    {
        pendingAction = null;
    }

    public static void Draw()
    {
        if (!IsPending)
            return;

        using var theme = SoulstoneTheme.Push();

        ImGui.OpenPopup(PopupId);
        ImGui.SetNextWindowSize(new Vector2(420, 0), ImGuiCond.Appearing);

        var isOpen = true;
        if (!ImGui.BeginPopupModal(PopupId, ref isOpen, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
            return;

        ImGui.TextColored(ImGuiColors.DalamudRed, LocalizationManager.Instance.GetLocalizedString("DeleteConfirmationTitle"));
        ImGui.Spacing();
        ImGui.TextWrapped(LocalizationManager.Instance.GetLocalizedString("DeleteConfirmationMessage"));
        ImGui.Spacing();

        if (UiUtils.IconTextButton("ConfirmGlobalDeleteBtn", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("DeleteButton")))
        {
            Confirm();
            ImGui.CloseCurrentPopup();
        }

        ImGui.SameLine();
        if (UiUtils.IconTextButton("CancelGlobalDeleteBtn", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("CancelButton")))
        {
            Cancel();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();

        if (!isOpen)
            Cancel();
    }
}
