using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Soulstone.Managers;
using Soulstone.Utils;

namespace Soulstone.Windows;

internal partial class CharacterWindow
{
    private static readonly Vector4 SheetBackground = SoulstoneTheme.Background;
    private static readonly Vector4 SheetGold = SoulstoneTheme.Gold;
    private static readonly Vector4 SheetMuted = SoulstoneTheme.Muted;
    private float sheetHeroHeight;

    private static string SheetLabel(string key) => LocalizationManager.Instance.GetLocalizedString(key).Trim().TrimEnd(':').TrimEnd();

    private void DrawSheetColumns(Action left, Action right)
    {
        var wide = ImGui.GetContentRegionAvail().X >= 900.0f * ImGuiHelpers.GlobalScale;
        if (!wide)
        {
            left();
            ImGui.Spacing();
            right();
            return;
        }

        using var table = ImRaii.Table("##SheetColumns" + left.Method.Name, 2, ImGuiTableFlags.SizingStretchProp);
        if (!table.Success) return;
        ImGui.TableSetupColumn("Primary", ImGuiTableColumnFlags.WidthStretch, 1.15f);
        ImGui.TableSetupColumn("Secondary", ImGuiTableColumnFlags.WidthStretch, 1.0f);
        ImGui.TableNextColumn();
        left();
        ImGui.TableNextColumn();
        right();
    }

    private void DrawSheetPanel(string id, string title, FontAwesomeIcon icon, Action content)
    {
        using var panel = SoulstoneTheme.BeginPanel(id, title, icon, () =>
        {
            if (UiUtils.IconButton("EditPanel", editingCharsheet ? FontAwesomeIcon.Check : FontAwesomeIcon.PencilAlt,
                    SheetLabel("EditCharsheetCheck"), new Vector2(26, 24) * ImGuiHelpers.GlobalScale))
                editingCharsheet = !editingCharsheet;
        });
        if (panel.Success) content();
    }

    private void DrawSheetHero()
    {
        if (currentCharacter == null) return;
        var scale = ImGuiHelpers.GlobalScale;
        var compact = ImGui.GetContentRegionAvail().X < 650 * scale;
        var portraitSize = new Vector2(compact ? 108 : 210, compact ? 140 : 230) * scale;
        using var background = ImRaii.PushColor(ImGuiCol.ChildBg, SheetBackground);
        using var border = ImRaii.PushColor(ImGuiCol.Border, SheetGold with { W = 0.75f });
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 9 * scale);
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(14, 14) * scale);
        using var hero = ImRaii.Child("##SheetHero", new Vector2(0, Math.Max(sheetHeroHeight, (compact ? 265 : 260) * scale)), true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!hero.Success) return;

        var portraitPos = ImGui.GetCursorScreenPos();
        DrawSheetPortrait(portraitSize);
        ImGui.SetCursorScreenPos(portraitPos + new Vector2(portraitSize.X + 22 * scale, 4 * scale));
        ImGui.BeginGroup();
        var name = string.IsNullOrWhiteSpace(currentCharacter.characterFullName)
            ? SheetLabel("UnnamedCharacter") : currentCharacter.characterFullName;
        ImGui.SetWindowFontScale(compact ? 1.15f : 1.65f);
        ImGui.PushTextWrapPos(ImGui.GetWindowContentRegionMax().X);
        ImGui.TextUnformatted(name);
        ImGui.PopTextWrapPos();
        ImGui.SetWindowFontScale(1);
        if (!string.IsNullOrWhiteSpace(currentCharacter.characterNickName))
        {
            using var nicknameColor = ImRaii.PushColor(ImGuiCol.Text, SheetMuted);
            ImGui.TextWrapped($"\"{currentCharacter.characterNickName}\"");
        }
        ImGui.Spacing();
        if (UiUtils.IconTextButton("EditSheetHero", editingCharsheet ? FontAwesomeIcon.Check : FontAwesomeIcon.PencilAlt,
                SheetLabel(editingCharsheet ? "BadgeEditing" : "SheetEditCharacter")))
            editingCharsheet = !editingCharsheet;
        ImGui.EndGroup();
        var detailsBottom = ImGui.GetItemRectMax().Y + 14 * scale;

        // Badges wrap independently so every field stays reachable in smaller windows.
        var badgeY = Math.Max(detailsBottom, portraitPos.Y + (compact ? portraitSize.Y + 12 * scale : 130 * scale));
        ImGui.SetCursorScreenPos(new Vector2(portraitPos.X + (compact ? 0 : portraitSize.X + 22 * scale), badgeY));
        var badgeStart = ImGui.GetCursorPosX();
        var first = true;
        DrawSheetBadge(currentCharacter.characterJob, ImGuiColors.ParsedBlue, FontAwesomeIcon.UserShield, badgeStart, ref first);
        var race = string.IsNullOrWhiteSpace(currentCharacter.characterSubRace) ? currentCharacter.characterRace
            : $"{currentCharacter.characterRace} ({currentCharacter.characterSubRace})";
        DrawSheetBadge(race, ImGuiColors.DalamudViolet, FontAwesomeIcon.Dna, badgeStart, ref first);
        var gender = string.IsNullOrWhiteSpace(currentCharacter.characterPronouns) ? currentCharacter.characterGender
            : $"{currentCharacter.characterGender} ({currentCharacter.characterPronouns})";
        DrawSheetBadge(gender, ImGuiColors.ParsedGreen, FontAwesomeIcon.VenusMars, badgeStart, ref first);
        var age = string.IsNullOrWhiteSpace(currentCharacter.characterAge) ? ""
            : string.Format(SheetLabel("AgeYearsFormat"), currentCharacter.characterAge);
        DrawSheetBadge(age, ImGuiColors.DalamudWhite, FontAwesomeIcon.HourglassHalf, badgeStart, ref first);
        // Place the row explicitly: Spacing() resets the cursor X to the child window's left edge.
        ImGui.SetCursorScreenPos(GetHeroBirthplacePosition(portraitPos, portraitSize, compact,
            first ? badgeY : ImGui.GetItemRectMax().Y, scale));
        ImGui.BeginGroup();
        ImGui.TextColored(SheetMuted, SheetLabel("CharBirthplaceField"));
        ImGui.SameLine();
        ImGui.TextWrapped(string.IsNullOrWhiteSpace(currentCharacter.characterHomeland) ? "—" : currentCharacter.characterHomeland);
        ImGui.EndGroup();
        sheetHeroHeight = Math.Max(portraitSize.Y + 28 * scale, ImGui.GetCursorPosY() + 14 * scale);
    }

    internal static Vector2 GetHeroBirthplacePosition(Vector2 portraitPosition, Vector2 portraitSize,
        bool compact, float badgesBottom, float scale)
    {
        return new Vector2(portraitPosition.X + (compact ? 0 : portraitSize.X + 22 * scale),
            Math.Max(badgesBottom + 10 * scale, compact ? portraitPosition.Y + portraitSize.Y + 12 * scale : portraitPosition.Y));
    }

    private static void DrawSheetBadge(string text, Vector4 color, FontAwesomeIcon icon, float start, ref bool first)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var scale = ImGuiHelpers.GlobalScale;
        var width = ImGui.CalcTextSize(text).X + 46 * scale;
        if (!first && ImGui.GetItemRectMax().X + width + 8 * scale < ImGui.GetWindowPos().X + ImGui.GetWindowContentRegionMax().X)
            ImGui.SameLine(0, 8 * scale);
        else
            ImGui.SetCursorPosX(start);
        UiUtils.PillBadge(text, new Vector4(color.X * 0.16f, color.Y * 0.16f, color.Z * 0.16f, 0.95f), color, icon);
        first = false;
    }

    private void DrawSheetPortrait(Vector2 size)
    {
        if (currentCharacter == null) return;
        var pos = ImGui.GetCursorScreenPos();
        var drawList = ImGui.GetWindowDrawList();
        var scale = ImGuiHelpers.GlobalScale;
        var texture = ImageHelper.GetTexture(currentCharacter.characterPictureUrl);
        if (texture == null)
            ImageHelper.DrawThumbnailOrPlaceholder(null, size, "RP", SheetGold, 7 * scale);
        else
        {
            // Crop to fill the frame without stretching the portrait.
            var fit = Math.Max(size.X / texture.Width, size.Y / texture.Height);
            var visible = size / (new Vector2(texture.Width, texture.Height) * fit);
            var uvMin = (Vector2.One - visible) * 0.5f;
            drawList.AddImageRounded(texture.Handle, pos, pos + size, uvMin, Vector2.One - uvMin, 0xFFFFFFFF, 7 * scale);
            ImGui.Dummy(size);
        }
        drawList.AddRect(pos - new Vector2(4 * scale), pos + size + new Vector2(4 * scale),
            ImGui.ColorConvertFloat4ToU32(SheetGold with { W = 0.4f }), 10 * scale);
        drawList.AddRect(pos, pos + size, ImGui.ColorConvertFloat4ToU32(SheetGold), 7 * scale, ImDrawFlags.None, 2 * scale);
    }

    private void DrawSheetIdentity()
    {
        if (currentCharacter == null) return;
        DrawSheetPanel("##SheetIdentity", SheetLabel("SheetGeneralInformation"), FontAwesomeIcon.User, () =>
        {
            using var table = ImRaii.Table("##SheetIdentityFields", 2, ImGuiTableFlags.SizingStretchSame);
            if (!table.Success) return;
            DrawSheetField("CharFullnameField", ref currentCharacter.characterFullName, "CharacterFullName", FontAwesomeIcon.IdCard, SheetGold);
            DrawSheetField("CharNicknameField", ref currentCharacter.characterNickName, "CharacterNickName", FontAwesomeIcon.QuoteRight, SheetGold);
            DrawSheetField("CharSpecieField", ref currentCharacter.characterRace, "CharacterRace", FontAwesomeIcon.Dna, ImGuiColors.DalamudViolet);
            DrawSheetField("CharSubSpecieField", ref currentCharacter.characterSubRace, "CharacterSubRace", FontAwesomeIcon.Dna, ImGuiColors.DalamudViolet);
            DrawSheetField("CharClassField", ref currentCharacter.characterJob, "CharacterJob", FontAwesomeIcon.UserShield, ImGuiColors.ParsedBlue);
            DrawSheetField("CharAgeField", ref currentCharacter.characterAge, "CharacterAge", FontAwesomeIcon.HourglassHalf, ImGuiColors.DalamudWhite);
            DrawSheetField("CharSexField", ref currentCharacter.characterSex, "CharacterSex", FontAwesomeIcon.VenusMars, ImGuiColors.ParsedGreen);
            DrawSheetField("CharGenderField", ref currentCharacter.characterGender, "CharacterGender", FontAwesomeIcon.VenusMars, ImGuiColors.ParsedGreen);
            DrawSheetField("CharPronounsField", ref currentCharacter.characterPronouns, "CharacterPronouns", FontAwesomeIcon.CommentDots, ImGuiColors.ParsedGreen);
            DrawSheetField("CharBirthplaceField", ref currentCharacter.characterHomeland, "CharacterHomeland", FontAwesomeIcon.MapMarkerAlt, SheetGold);
            DrawSheetField("DiceSysLinkedLabel", ref currentCharacter.linkedDiceSystem, "CharacterLinkedSystem", FontAwesomeIcon.DiceD20, SheetGold);
        });
    }

    private void DrawSheetField(string labelKey, ref string value, string fieldName, FontAwesomeIcon icon, Vector4 color)
    {
        ImGui.TableNextColumn();
        var scale = ImGuiHelpers.GlobalScale;
        using var bg = ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.085f, 0.10f, 0.125f, 1));
        using var border = ImRaii.PushColor(ImGuiCol.Border, SoulstoneTheme.Border);
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6 * scale);
        using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10, 7) * scale);
        using var tile = ImRaii.Child("##SheetField" + fieldName, new Vector2(0, (editingCharsheet ? 70 : 62) * scale), true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        if (!tile.Success) return;
        using (ImRaii.PushFont(UiBuilder.IconFont)) ImGui.TextColored(color, icon.ToIconString());
        ImGui.SameLine(0, 8 * scale);
        var textX = ImGui.GetCursorPosX();
        // Reserve the right edge for privacy controls, including when values are long.
        var toggleX = ImGui.GetWindowContentRegionMax().X - 22 * scale;
        ImGui.PushClipRect(ImGui.GetCursorScreenPos(), ImGui.GetWindowPos() + new Vector2(toggleX - 4 * scale, 32 * scale), true);
        ImGui.TextColored(SheetMuted, SheetLabel(labelKey));
        ImGui.PopClipRect();
        ImGui.SameLine(toggleX);
        DrawVisibilityToggle(fieldName);
        ImGui.SetCursorPosX(textX);
        if (editingCharsheet)
            UiUtils.StyledInputText("Value", ref value, 500, width: -1);
        else
        {
            ImGui.TextUnformatted(string.IsNullOrWhiteSpace(value) ? "—" : value);
            if (ImGui.IsItemHovered() && !string.IsNullOrWhiteSpace(value)) UiUtils.SetTooltip(value);
        }
    }

    private void DrawSheetDescription()
    {
        if (currentCharacter == null) return;
        DrawSheetPanel("##SheetDescription", SheetLabel("SheetDescription"), FontAwesomeIcon.FileAlt, () =>
        {
            DrawFieldLabelWithToggle(SheetLabel("CharBackgroundField"), "CharacterBackground");
            if (editingCharsheet)
                UiUtils.ManageBigInputField(ref currentCharacter.characterBackground, "SheetBackgroundText", true, 125);
            else
                ImGui.TextWrapped(string.IsNullOrWhiteSpace(currentCharacter.characterBackground) ? SheetLabel("NoneText") : currentCharacter.characterBackground);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            DrawFieldLabelWithToggle(SheetLabel("CharPictureField"), "CharacterPictureUrl");
            var imageWidth = Math.Min(240 * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X);
            var previewPos = ImGui.GetCursorScreenPos();
            var previewSize = new Vector2(imageWidth, imageWidth * 0.55f);
            ImageHelper.DrawThumbnailOrPlaceholder(currentCharacter.characterPictureUrl, previewSize, "RP", SheetGold);
            ImGui.SetCursorScreenPos(previewPos);
            ImGui.Dummy(previewSize);
            if (editingCharsheet)
            {
                UiUtils.StyledInputText("SheetPictureUrl", ref currentCharacter.characterPictureUrl, 500, width: -1);
                if (UiUtils.IconButton("SheetBrowsePicture", FontAwesomeIcon.FolderOpen, SheetLabel("CharPictureBrowse")))
                {
                    var sheet = currentCharacter;
                    plugin.OpenFilePicker(SheetLabel("ChooseCharPicPickerTitle"), ".png;.jpg;.jpeg;.bmp;.webp;.gif", path =>
                        sheet.characterPictureUrl = ImageHelper.CopyImageToLocalFolder(path, "portraits"));
                }
                ImGui.SameLine();
                if (UiUtils.IconButton("SheetClearPicture", FontAwesomeIcon.Trash, SheetLabel("CharPictureClear")))
                    currentCharacter.characterPictureUrl = string.Empty;
            }
        });
    }

    private void DrawSheetAppearance()
    {
        if (currentCharacter == null) return;
        DrawSheetPanel("##SheetAppearance", SheetLabel("PhysicalAppearanceTab"), FontAwesomeIcon.PaintBrush, () =>
        {
            using var table = ImRaii.Table("##SheetAppearanceFields", 2, ImGuiTableFlags.SizingStretchSame);
            if (!table.Success) return;
            DrawSheetDetail("CharHeightField", ref currentCharacter.characterHeight, "CharacterHeight");
            DrawSheetDetail("CharHairColorField", ref currentCharacter.characterHairColor, "CharacterHairColor");
            DrawSheetDetail("CharBuildField", ref currentCharacter.characterBuild, "CharacterBuild");
            DrawSheetDetail("CharWeightField", ref currentCharacter.characterWeight, "CharacterWeight");
            DrawSheetDetail("CharSkinColorField", ref currentCharacter.characterSkinTone, "CharacterSkinTone");
            DrawSheetDetail("CharScarsField", ref currentCharacter.characterScars, "CharacterScars");
            DrawSheetDetail("CharEyeColorField", ref currentCharacter.characterEyeColor, "CharacterEyeColor");
            DrawSheetDetail("CharTatooField", ref currentCharacter.characterTattoos, "CharacterTattoos");
        });
        if (editingCharsheet || !string.IsNullOrWhiteSpace(currentCharacter.characterDistinctiveFeatures))
        {
            ImGui.Spacing();
            DrawSheetPanel("##SheetDistinctiveFeatures", SheetLabel("CharOtherQuirkField"), FontAwesomeIcon.Star, () =>
            {
                DrawFieldLabelWithToggle(SheetLabel("CharOtherQuirkField"), "CharacterDistinctiveFeatures");
                UiUtils.ManageBigInputField(ref currentCharacter.characterDistinctiveFeatures, "SheetDistinctiveFeaturesText", editingCharsheet, 50);
            });
        }
    }

    private void DrawSheetQuickLook()
    {
        if (currentCharacter == null) return;
        DrawSheetPanel("##SheetQuickLook", SheetLabel("QuickLookSectionTitle"), FontAwesomeIcon.TheaterMasks, () =>
        {
            var labels = new[] { SheetLabel("QuickLookField1"), SheetLabel("QuickLookField2"), SheetLabel("QuickLookField3"),
                SheetLabel("QuickLookField4"), SheetLabel("QuickLookField5") };
            using var table = ImRaii.Table("##SheetQuickLookFields", 1, ImGuiTableFlags.SizingStretchSame);
            if (!table.Success) return;
            // Keep the five RP traits and their existing serialization and visibility names.
            DrawSheetTrait(labels[0], ref currentCharacter.characterQuickLook1, "CharacterQuickLook1");
            DrawSheetTrait(labels[1], ref currentCharacter.characterQuickLook2, "CharacterQuickLook2");
            DrawSheetTrait(labels[2], ref currentCharacter.characterQuickLook3, "CharacterQuickLook3");
            DrawSheetTrait(labels[3], ref currentCharacter.characterQuickLook4, "CharacterQuickLook4");
            DrawSheetTrait(labels[4], ref currentCharacter.characterQuickLook5, "CharacterQuickLook5");
        });
    }

    private void DrawSheetDetail(string labelKey, ref string value, string fieldName)
    {
        ImGui.TableNextColumn();
        ImGui.PushID(fieldName);
        DrawFieldLabelWithToggle(SheetLabel(labelKey), fieldName);
        if (editingCharsheet)
            UiUtils.StyledInputText("Value", ref value, 500, width: -1);
        else
            ImGui.TextWrapped(string.IsNullOrWhiteSpace(value) ? "—" : value);
        ImGui.Spacing();
        ImGui.PopID();
    }

    private void DrawSheetTrait(string label, ref string value, string fieldName)
    {
        ImGui.TableNextColumn();
        DrawFieldLabelWithToggle(label, fieldName);
        if (editingCharsheet)
            UiUtils.StyledInputText(fieldName, ref value, 500, width: -1);
        else
            ImGui.TextWrapped(string.IsNullOrWhiteSpace(value) ? "—" : value);
        ImGui.Spacing();
    }
}
