using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Soulstone.Utils;

namespace Soulstone.Windows;

internal partial class CharacterWindow
{
    private void DrawSheetOoc()
    {
        if (currentCharacter == null) return;
        DrawSheetPanel("##SheetOoc", SheetLabel("PlayerOOCInfo"), FontAwesomeIcon.UserFriends, () =>
        {
            using (var table = ImRaii.Table("##SheetOocFields", 2, ImGuiTableFlags.SizingStretchSame))
            {
                if (table.Success)
                {
                    DrawSheetField("PlayerAvailability", ref currentCharacter.playerAvailability, "PlayerAvailability", FontAwesomeIcon.Clock, SheetGold);
                    DrawSheetField("PlayerTimezone", ref currentCharacter.playerTimezone, "PlayerTimezone", FontAwesomeIcon.Globe, SheetGold);
                    DrawSheetField("PlayerOOCInfo", ref currentCharacter.characterInfo, "CharacterInfo", FontAwesomeIcon.UserCircle, SheetGold);
                    DrawSheetField("CharNotesField", ref currentCharacter.characterNotes, "CharacterNotes", FontAwesomeIcon.StickyNote, SheetGold);
                }
            }
            ImGui.Spacing();
            DrawFieldLabelWithToggle(SheetLabel("PlayerOOCNotes"), "PlayerNotes");
            UiUtils.ManageBigInputField(ref currentCharacter.playerNotes, "SheetPlayerNotes", editingCharsheet, 70);
        });
    }

    private void DrawSheetBackground()
    {
        if (currentCharacter == null) return;
        DrawSheetPanel("##SheetBackground", SheetLabel("CharBackgroundField"), FontAwesomeIcon.BookOpen, () =>
        {
            using (var table = ImRaii.Table("##SheetBackgroundFields", 2, ImGuiTableFlags.SizingStretchSame))
            {
                if (table.Success)
                {
                    DrawSheetField("CharOriginField", ref currentCharacter.characterOrigin, "CharacterOrigin", FontAwesomeIcon.GlobeAmericas, SheetGold);
                    DrawSheetField("CharAffiliationField", ref currentCharacter.characterAffiliation, "CharacterAffiliation", FontAwesomeIcon.Building, SheetGold);
                    DrawSheetField("CharWorkField", ref currentCharacter.characterOccupation, "CharacterOccupation", FontAwesomeIcon.Briefcase, SheetGold);
                }
            }
            ImGui.Spacing();
            DrawFieldLabelWithToggle(SheetLabel("CharReputationField"), "CharacterReputation");
            UiUtils.ManageBigInputField(ref currentCharacter.characterReputation, "SheetReputation", editingCharsheet, 70);
        });
    }

    private void DrawSheetRelationships()
    {
        if (currentCharacter == null) return;
        var columns = ImGui.GetContentRegionAvail().X >= 900 * ImGuiHelpers.GlobalScale ? 3 : 1;
        using var table = ImRaii.Table("##SheetRelationships", columns, ImGuiTableFlags.SizingStretchSame);
        if (!table.Success) return;
        ImGui.TableNextColumn();
        DrawSheetRelation("Family", "CharFamilyRelationTab", FontAwesomeIcon.Users,
            currentCharacter.characterFamily ??= new Dictionary<string, string>(), () => showFamilyPopup = true, "CharacterFamily");
        ImGui.TableNextColumn();
        DrawSheetRelation("Friends", "CharFriendsTab", FontAwesomeIcon.UserFriends,
            currentCharacter.characterFriends ??= new Dictionary<string, string>(), () => showFriendsPopup = true, "CharacterFriends");
        ImGui.TableNextColumn();
        DrawSheetRelation("Enemies", "CharEnemiesTab", FontAwesomeIcon.UserShield,
            currentCharacter.characterEnnemies ??= new Dictionary<string, string>(), () => showEnemiesPopup = true, "CharacterEnnemies");
    }

    private void DrawSheetRelation(string id, string titleKey, FontAwesomeIcon icon,
        Dictionary<string, string> relations, Action onAdd, string fieldName)
    {
        DrawSheetPanel("##SheetRelation" + id, SheetLabel(titleKey), icon, () =>
        {
            var scale = ImGuiHelpers.GlobalScale;
            DrawVisibilityToggle(fieldName);
            ImGui.SameLine();
            UiUtils.Badge(relations.Count.ToString(), new Vector4(0.18f, 0.20f, 0.24f, 1), SheetGold);
            ImGui.SameLine();
            if (UiUtils.IconButton("AddRelation", FontAwesomeIcon.Plus, SheetLabel("AddButton"), new Vector2(24, 24) * scale))
            {
                newMemberName = "";
                newMemberDescription = "";
                onAdd();
            }
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            if (relations.Count == 0)
            {
                ImGui.TextDisabled(SheetLabel("NoneText"));
                return;
            }
            string? keyToRemove = null;
            foreach (var relation in relations.ToList())
            {
                using var entryId = ImRaii.PushId(relation.Key);
                ImGui.TextColored(SheetGold, relation.Key);
                if (editingCharsheet)
                {
                    ImGui.SameLine();
                    if (UiUtils.IconButton("RemoveRelation", FontAwesomeIcon.Trash, SheetLabel("RemoveTooltip"), new Vector2(20, 20) * scale))
                        keyToRemove = relation.Key;
                    var description = relation.Value;
                    if (UiUtils.StyledInputText("Description", ref description, 300, width: -1))
                        relations[relation.Key] = description;
                }
                else
                    ImGui.TextWrapped(relation.Value);
                ImGui.Spacing();
            }
            if (keyToRemove != null)
            {
                var key = keyToRemove;
                DeleteConfirmation.Request(() => relations.Remove(key));
            }
        });
    }
}
