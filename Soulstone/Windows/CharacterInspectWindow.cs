using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Soulstone.Windows
{
    internal class CharacterInspectWindow : Window, IDisposable
    {
        private readonly Plugin plugin;
        public CharacterSheet? InspectedCharacter { get; set; }
        public string InspectedCharacterName { get; set; } = string.Empty;
        public string InspectedWorldName { get; set; } = string.Empty;
        public bool IsLoading { get; set; } = false;
        public string? ErrorMessage { get; set; } = null;

        public CharacterInspectWindow(Plugin plugin)
            : base("Soulstone - Roleplay Sheet###SoulstoneInspectWin", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.plugin = plugin;
            Size = new Vector2(700, 600);
            SizeCondition = ImGuiCond.FirstUseEver;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(450, 350),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose() { }

        public void OpenFor(string characterName, string? worldName, CharacterSheet sheet)
        {
            InspectedCharacterName = characterName;
            InspectedWorldName = worldName ?? string.Empty;
            InspectedCharacter = sheet;
            IsLoading = false;
            ErrorMessage = null;
            IsOpen = true;
        }

        public void OpenLoading(string characterName, string? worldName)
        {
            InspectedCharacterName = characterName;
            InspectedWorldName = worldName ?? string.Empty;
            InspectedCharacter = null;
            IsLoading = true;
            ErrorMessage = null;
            IsOpen = true;
        }

        public void SetError(string characterName, string? worldName, string error)
        {
            InspectedCharacterName = characterName;
            InspectedWorldName = worldName ?? string.Empty;
            InspectedCharacter = null;
            IsLoading = false;
            ErrorMessage = error;
            IsOpen = true;
        }

        public override void Draw()
        {
            var scale = ImGuiHelpers.GlobalScale;

            if (IsLoading)
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.ParsedGold, LocalizationManager.Instance.GetLocalizedString("CharSheetFetching"));
                ImGui.Spacing();
                ImGui.TextUnformatted($"{InspectedCharacterName} ({(string.IsNullOrEmpty(InspectedWorldName) ? "?" : InspectedWorldName)})");
                return;
            }

            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ImGui.Spacing();
                ImGui.TextColored(ImGuiColors.DPSRed, ErrorMessage);
                ImGui.Spacing();
                ImGui.TextUnformatted($"{InspectedCharacterName} ({(string.IsNullOrEmpty(InspectedWorldName) ? "?" : InspectedWorldName)})");
                return;
            }

            if (InspectedCharacter == null)
            {
                ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("CharSheetNotFoundServer"));
                return;
            }

            DrawInspectHeader();
            ImGui.Spacing();

            using (var scrollChild = ImRaii.Child("##InspectScrollableContent", new Vector2(0, 0), false))
            {
                if (scrollChild.Success)
                {
                    DrawHeroCard();
                    ImGui.Spacing();
                    DrawIdentitySection();
                    ImGui.Spacing();
                    DrawOocSection();
                    ImGui.Spacing();
                    DrawAppearanceSection();
                    ImGui.Spacing();
                    DrawQuickLookSection();
                    ImGui.Spacing();
                    DrawBackgroundSection();
                }
            }
        }

        private void DrawInspectHeader()
        {
            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var barHeight = 36.0f * scale;
            var drawList = ImGui.GetWindowDrawList();

            var bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.10f, 0.12f, 0.15f, 0.95f));
            var borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.24f, 0.28f, 0.35f, 0.75f));
            drawList.AddRectFilled(pos, pos + new Vector2(availWidth, barHeight), bgCol, 6.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(availWidth, barHeight), borderCol, 6.0f * scale, ImDrawFlags.None, 1.2f);

            ImGui.SetCursorScreenPos(pos + new Vector2(10.0f * scale, 6.0f * scale));

            ImGui.BeginGroup();
            {
                UiUtils.PillBadge(LocalizationManager.Instance.GetLocalizedString("BadgeViewing"), new Vector4(0.18f, 0.32f, 0.50f, 0.85f), ImGuiColors.ParsedBlue, FontAwesomeIcon.Eye);
                ImGui.SameLine(0, 8.0f * scale);
                string worldSuffix = !string.IsNullOrEmpty(InspectedWorldName) ? $" ({InspectedWorldName})" : "";
                ImGui.TextColored(ImGuiColors.ParsedGold, $"{InspectedCharacterName}{worldSuffix}");
            }
            ImGui.EndGroup();

            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(new Vector2(availWidth, barHeight));
        }

        private void DrawHeroCard()
        {
            if (InspectedCharacter == null) return;

            var scale = ImGuiHelpers.GlobalScale;
            var portraitWidth = 135.0f * scale;
            var portraitHeight = 165.0f * scale;

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.11f, 0.14f, 0.95f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.80f, 0.65f, 0.25f, 0.85f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(14.0f, 12.0f) * scale))
            using (var card = ImRaii.Child("##HeroCardInspect", new Vector2(0, 190.0f * scale), true, ImGuiWindowFlags.NoScrollbar))
            {
                if (!card.Success) return;

                var drawList = ImGui.GetWindowDrawList();
                var cardPos = ImGui.GetWindowPos();
                var cardSize = ImGui.GetWindowSize();

                drawList.AddRectFilled(
                    cardPos + new Vector2(2.5f * scale, 6.0f * scale),
                    cardPos + new Vector2(6.0f * scale, cardSize.Y - 6.0f * scale),
                    ImGui.ColorConvertFloat4ToU32(ImGuiColors.ParsedGold),
                    2.0f * scale);

                ImGui.BeginGroup();
                {
                    var placeholder = !string.IsNullOrWhiteSpace(InspectedCharacter.characterFullName)
                        ? (InspectedCharacter.characterFullName.Length > 2 ? InspectedCharacter.characterFullName[..2].ToUpper() : InspectedCharacter.characterFullName.ToUpper())
                        : "RP";
                    ImageHelper.DrawThumbnailOrPlaceholder(InspectedCharacter.characterPictureUrl, new Vector2(portraitWidth, portraitHeight), placeholder, ImGuiColors.ParsedGold, 6.0f);
                }
                ImGui.EndGroup();

                ImGui.SameLine(0, 18.0f * scale);

                ImGui.BeginGroup();
                {
                    var displayName = !string.IsNullOrWhiteSpace(InspectedCharacter.characterFullName) ? InspectedCharacter.characterFullName : LocalizationManager.Instance.GetLocalizedString("UnnamedCharacter");
                    ImGui.TextColored(ImGuiColors.ParsedGold, displayName);

                    if (!InspectedCharacter.IsFieldHidden("CharacterNickName") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterNickName))
                    {
                        ImGui.SameLine(0, 8.0f * scale);
                        ImGui.TextColored(ImGuiColors.DalamudGrey, $"\"{InspectedCharacter.characterNickName}\"");
                    }

                    ImGui.Spacing();

                    // Badges row (skip if field is hidden)
                    if (!InspectedCharacter.IsFieldHidden("CharacterJob") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterJob))
                    {
                        UiUtils.PillBadge(InspectedCharacter.characterJob, new Vector4(0.20f, 0.35f, 0.60f, 0.85f), ImGuiColors.ParsedBlue, FontAwesomeIcon.UserShield);
                        ImGui.SameLine(0, 6.0f * scale);
                    }
                    if (!InspectedCharacter.IsFieldHidden("CharacterRace") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterRace))
                    {
                        var raceText = (!InspectedCharacter.IsFieldHidden("CharacterSubRace") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterSubRace))
                            ? $"{InspectedCharacter.characterRace} ({InspectedCharacter.characterSubRace})"
                            : InspectedCharacter.characterRace;
                        UiUtils.PillBadge(raceText, new Vector4(0.35f, 0.20f, 0.50f, 0.85f), ImGuiColors.DalamudViolet, FontAwesomeIcon.Dna);
                        ImGui.SameLine(0, 6.0f * scale);
                    }
                    if (!InspectedCharacter.IsFieldHidden("CharacterGender") && (!string.IsNullOrWhiteSpace(InspectedCharacter.characterGender) || !string.IsNullOrWhiteSpace(InspectedCharacter.characterPronouns)))
                    {
                        var genderText = (!InspectedCharacter.IsFieldHidden("CharacterPronouns") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterPronouns))
                            ? $"{InspectedCharacter.characterGender} ({InspectedCharacter.characterPronouns})"
                            : InspectedCharacter.characterGender;
                        if (!string.IsNullOrWhiteSpace(genderText))
                        {
                            UiUtils.PillBadge(genderText, new Vector4(0.18f, 0.40f, 0.28f, 0.85f), ImGuiColors.ParsedGreen, FontAwesomeIcon.VenusMars);
                            ImGui.SameLine(0, 6.0f * scale);
                        }
                    }
                    if (!InspectedCharacter.IsFieldHidden("CharacterAge") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterAge))
                    {
                        UiUtils.PillBadge(string.Format(LocalizationManager.Instance.GetLocalizedString("AgeYearsFormat"), InspectedCharacter.characterAge), new Vector4(0.28f, 0.28f, 0.35f, 0.85f), ImGuiColors.DalamudWhite, FontAwesomeIcon.HourglassHalf);
                    }

                    ImGui.NewLine();
                    ImGui.Spacing();

                    if (!InspectedCharacter.IsFieldHidden("CharacterOccupation") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterOccupation))
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharWorkField")} ");
                        ImGui.SameLine(0, 4.0f * scale);
                        ImGui.TextUnformatted(InspectedCharacter.characterOccupation);
                    }
                    if (!InspectedCharacter.IsFieldHidden("CharacterAffiliation") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterAffiliation))
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharAffiliationField")} ");
                        ImGui.SameLine(0, 4.0f * scale);
                        ImGui.TextUnformatted(InspectedCharacter.characterAffiliation);
                    }
                    if (!InspectedCharacter.IsFieldHidden("CharacterHomeland") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterHomeland))
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, $"{LocalizationManager.Instance.GetLocalizedString("CharBirthplaceField")} ");
                        ImGui.SameLine(0, 4.0f * scale);
                        ImGui.TextUnformatted(InspectedCharacter.characterHomeland);
                    }
                }
                ImGui.EndGroup();
            }
        }

        private void DrawIdentitySection()
        {
            if (InspectedCharacter == null) return;

            var items = new List<(string Label, string? Value, FontAwesomeIcon Icon, Vector4 Color)>();

            if (!InspectedCharacter.IsFieldHidden("CharacterFullName"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharFullnameField"), InspectedCharacter.characterFullName, FontAwesomeIcon.IdCard, ImGuiColors.ParsedGold));

            if (!InspectedCharacter.IsFieldHidden("CharacterNickName"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharNicknameField"), InspectedCharacter.characterNickName, FontAwesomeIcon.QuoteRight, ImGuiColors.ParsedGold));

            if (!InspectedCharacter.IsFieldHidden("CharacterRace"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharSpecieField"), InspectedCharacter.characterRace, FontAwesomeIcon.Dna, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterSubRace"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharSubSpecieField"), InspectedCharacter.characterSubRace, FontAwesomeIcon.Dna, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterJob"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharClassField"), InspectedCharacter.characterJob, FontAwesomeIcon.UserShield, ImGuiColors.ParsedBlue));

            if (!InspectedCharacter.IsFieldHidden("CharacterAge"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharAgeField"), InspectedCharacter.characterAge, FontAwesomeIcon.HourglassHalf, ImGuiColors.DalamudWhite));

            if (!InspectedCharacter.IsFieldHidden("CharacterSex"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharSexField"), InspectedCharacter.characterSex, FontAwesomeIcon.VenusMars, ImGuiColors.ParsedGreen));

            if (!InspectedCharacter.IsFieldHidden("CharacterGender"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharGenderField"), InspectedCharacter.characterGender, FontAwesomeIcon.VenusMars, ImGuiColors.ParsedGreen));

            if (!InspectedCharacter.IsFieldHidden("CharacterPronouns"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharPronounsField"), InspectedCharacter.characterPronouns, FontAwesomeIcon.CommentDots, ImGuiColors.ParsedGreen));

            if (!InspectedCharacter.IsFieldHidden("CharacterLinkedSystem") && !string.IsNullOrWhiteSpace(InspectedCharacter.linkedDiceSystem))
                items.Add((LocalizationManager.Instance.GetLocalizedString("DiceSysLinkedLabel"), InspectedCharacter.linkedDiceSystem, FontAwesomeIcon.DiceD20, ImGuiColors.ParsedGold));

            if (items.Count == 0) return;

            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("CharFullnameField").Replace(":", "").Trim(), defaultOpen: true, icon: FontAwesomeIcon.IdCard, accentColor: ImGuiColors.ParsedGold))
            {
                using var table = ImRaii.Table("##IdentityInspectGrid", 2, ImGuiTableFlags.SizingStretchSame);
                if (table.Success)
                {
                    foreach (var item in items)
                    {
                        ImGui.TableNextColumn();
                        DrawPropertyCard(item.Label, item.Value, item.Icon, item.Color);
                    }
                }
            }
        }

        private void DrawOocSection()
        {
            if (InspectedCharacter == null) return;

            var items = new List<(string Label, string? Value, FontAwesomeIcon Icon, Vector4 Color)>();

            if (!InspectedCharacter.IsFieldHidden("PlayerAvailability"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("PlayerAvailability"), InspectedCharacter.playerAvailability, FontAwesomeIcon.Clock, ImGuiColors.ParsedBlue));

            if (!InspectedCharacter.IsFieldHidden("PlayerTimezone"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("PlayerTimezone"), InspectedCharacter.playerTimezone, FontAwesomeIcon.Globe, ImGuiColors.ParsedBlue));

            if (!InspectedCharacter.IsFieldHidden("CharacterInfo"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("PlayerOOCInfo"), InspectedCharacter.characterInfo, FontAwesomeIcon.UserCircle, ImGuiColors.ParsedBlue));

            if (!InspectedCharacter.IsFieldHidden("CharacterNotes"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharNotesField"), InspectedCharacter.characterNotes, FontAwesomeIcon.StickyNote, ImGuiColors.ParsedBlue));

            bool showNotes = !InspectedCharacter.IsFieldHidden("PlayerNotes") && !string.IsNullOrWhiteSpace(InspectedCharacter.playerNotes);

            if (items.Count == 0 && !showNotes) return;

            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("PlayerOOCInfo").Replace(":", "").Trim(), defaultOpen: false, icon: FontAwesomeIcon.UserFriends, accentColor: ImGuiColors.ParsedBlue))
            {
                if (items.Count > 0)
                {
                    using var table = ImRaii.Table("##OOCInspectGrid", 2, ImGuiTableFlags.SizingStretchSame);
                    if (table.Success)
                    {
                        foreach (var item in items)
                        {
                            ImGui.TableNextColumn();
                            DrawPropertyCard(item.Label, item.Value, item.Icon, item.Color);
                        }
                    }
                }

                if (showNotes)
                {
                    ImGui.Spacing();
                    DrawStoryBlock(LocalizationManager.Instance.GetLocalizedString("PlayerOOCNotes"), InspectedCharacter.playerNotes, FontAwesomeIcon.StickyNote, ImGuiColors.ParsedBlue);
                }
            }
        }

        private void DrawAppearanceSection()
        {
            if (InspectedCharacter == null) return;

            var items = new List<(string Label, string? Value, FontAwesomeIcon Icon, Vector4 Color)>();

            if (!InspectedCharacter.IsFieldHidden("CharacterHeight"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharHeightField"), InspectedCharacter.characterHeight, FontAwesomeIcon.RulerVertical, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterWeight"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharWeightField"), InspectedCharacter.characterWeight, FontAwesomeIcon.WeightHanging, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterBuild"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharBuildField"), InspectedCharacter.characterBuild, FontAwesomeIcon.UserTag, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterSkinTone"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharSkinColorField"), InspectedCharacter.characterSkinTone, FontAwesomeIcon.Palette, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterEyeColor"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharEyeColorField"), InspectedCharacter.characterEyeColor, FontAwesomeIcon.Eye, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterHairColor"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharHairColorField"), InspectedCharacter.characterHairColor, FontAwesomeIcon.Magic, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterScars"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharScarsField"), InspectedCharacter.characterScars, FontAwesomeIcon.Cut, ImGuiColors.DalamudViolet));

            if (!InspectedCharacter.IsFieldHidden("CharacterTattoos"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharTatooField"), InspectedCharacter.characterTattoos, FontAwesomeIcon.PaintBrush, ImGuiColors.DalamudViolet));

            bool showDistinctive = !InspectedCharacter.IsFieldHidden("CharacterDistinctiveFeatures") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterDistinctiveFeatures);

            if (items.Count == 0 && !showDistinctive) return;

            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("PhysicalAppearanceTab").Replace(":", "").Trim(), defaultOpen: false, icon: FontAwesomeIcon.User, accentColor: ImGuiColors.DalamudViolet))
            {
                if (items.Count > 0)
                {
                    using var table = ImRaii.Table("##AppearanceInspectGrid", 2, ImGuiTableFlags.SizingStretchSame);
                    if (table.Success)
                    {
                        foreach (var item in items)
                        {
                            ImGui.TableNextColumn();
                            DrawPropertyCard(item.Label, item.Value, item.Icon, item.Color);
                        }
                    }
                }

                if (showDistinctive)
                {
                    ImGui.Spacing();
                    DrawStoryBlock(LocalizationManager.Instance.GetLocalizedString("CharOtherQuirkField"), InspectedCharacter.characterDistinctiveFeatures, FontAwesomeIcon.Star, ImGuiColors.DalamudViolet);
                }
            }
        }

        private void DrawQuickLookSection()
        {
            if (InspectedCharacter == null) return;

            string[] qlFields = { "CharacterQuickLook1", "CharacterQuickLook2", "CharacterQuickLook3", "CharacterQuickLook4", "CharacterQuickLook5" };
            string[] qlValues = {
                InspectedCharacter.characterQuickLook1,
                InspectedCharacter.characterQuickLook2,
                InspectedCharacter.characterQuickLook3,
                InspectedCharacter.characterQuickLook4,
                InspectedCharacter.characterQuickLook5,
            };

            var validItems = new List<(int Index, string Value)>();
            for (int i = 0; i < 5; i++)
            {
                if (!InspectedCharacter.IsFieldHidden(qlFields[i]) && !string.IsNullOrWhiteSpace(qlValues[i]))
                {
                    validItems.Add((i + 1, qlValues[i]));
                }
            }

            if (validItems.Count == 0) return;

            if (UiUtils.StyledCollapsingHeader(LocalizationManager.Instance.GetLocalizedString("QuickLookSectionTitle").Replace(":", "").Trim(), defaultOpen: false, icon: FontAwesomeIcon.Eye, accentColor: ImGuiColors.ParsedGreen))
            {
                var scale = ImGuiHelpers.GlobalScale;
                foreach (var item in validItems)
                {
                    using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.12f, 0.14f, 0.85f)))
                    using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.20f, 0.45f, 0.30f, 0.6f)))
                    using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
                    using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 6.0f) * scale))
                    using (var card = ImRaii.Child($"##QLCardInspect_{item.Index}", new Vector2(0, 36.0f * scale), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
                    {
                        if (card.Success)
                        {
                            UiUtils.Badge($"#{item.Index}", new Vector4(0.18f, 0.40f, 0.28f, 0.85f), ImGuiColors.ParsedGreen);
                            ImGui.SameLine(0, 8.0f * scale);
                            ImGui.TextColored(ImGuiColors.DalamudWhite, item.Value);
                        }
                    }
                    ImGui.Spacing();
                }
            }
        }

        private void DrawBackgroundSection()
        {
            if (InspectedCharacter == null) return;

            var items = new List<(string Label, string? Value, FontAwesomeIcon Icon, Vector4 Color)>();

            if (!InspectedCharacter.IsFieldHidden("CharacterHomeland"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharBirthplaceField"), InspectedCharacter.characterHomeland, FontAwesomeIcon.MapMarkerAlt, ImGuiColors.ParsedGold));

            if (!InspectedCharacter.IsFieldHidden("CharacterOrigin"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharOriginField"), InspectedCharacter.characterOrigin, FontAwesomeIcon.GlobeAmericas, ImGuiColors.ParsedGold));

            if (!InspectedCharacter.IsFieldHidden("CharacterAffiliation"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharAffiliationField"), InspectedCharacter.characterAffiliation, FontAwesomeIcon.Building, ImGuiColors.ParsedGold));

            if (!InspectedCharacter.IsFieldHidden("CharacterOccupation"))
                items.Add((LocalizationManager.Instance.GetLocalizedString("CharWorkField"), InspectedCharacter.characterOccupation, FontAwesomeIcon.Briefcase, ImGuiColors.ParsedGold));

            bool showRep = !InspectedCharacter.IsFieldHidden("CharacterReputation") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterReputation);
            bool showBg = !InspectedCharacter.IsFieldHidden("CharacterBackground") && !string.IsNullOrWhiteSpace(InspectedCharacter.characterBackground);
            bool showFamily = !InspectedCharacter.IsFieldHidden("CharacterFamily") && InspectedCharacter.characterFamily != null && InspectedCharacter.characterFamily.Count > 0;
            bool showFriends = !InspectedCharacter.IsFieldHidden("CharacterFriends") && InspectedCharacter.characterFriends != null && InspectedCharacter.characterFriends.Count > 0;
            bool showEnemies = !InspectedCharacter.IsFieldHidden("CharacterEnnemies") && InspectedCharacter.characterEnnemies != null && InspectedCharacter.characterEnnemies.Count > 0;

            if (items.Count == 0 && !showRep && !showBg && !showFamily && !showFriends && !showEnemies) return;

            if (UiUtils.StyledCollapsingHeader($"{LocalizationManager.Instance.GetLocalizedString("CharBackgroundField").Replace(":", "").Trim()} & {LocalizationManager.Instance.GetLocalizedString("CharFamilyRelationTab").Replace(":", "").Trim()}", defaultOpen: false, icon: FontAwesomeIcon.BookOpen, accentColor: ImGuiColors.ParsedGold))
            {
                if (items.Count > 0)
                {
                    using var table = ImRaii.Table("##BgInspectGrid", 2, ImGuiTableFlags.SizingStretchSame);
                    if (table.Success)
                    {
                        foreach (var item in items)
                        {
                            ImGui.TableNextColumn();
                            DrawPropertyCard(item.Label, item.Value, item.Icon, item.Color);
                        }
                    }
                }

                if (showRep)
                {
                    ImGui.Spacing();
                    DrawStoryBlock(LocalizationManager.Instance.GetLocalizedString("CharReputationField"), InspectedCharacter.characterReputation, FontAwesomeIcon.Award, ImGuiColors.ParsedGold);
                }

                if (showBg)
                {
                    ImGui.Spacing();
                    DrawStoryBlock(LocalizationManager.Instance.GetLocalizedString("CharBackgroundField"), InspectedCharacter.characterBackground, FontAwesomeIcon.BookOpen, ImGuiColors.ParsedGold);
                }

                if (showFamily || showFriends || showEnemies)
                {
                    ImGui.Spacing();
                    DrawInspectRelationsColumns(showFamily, showFriends, showEnemies);
                }
            }
        }

        private void DrawInspectRelationsColumns(bool showFamily, bool showFriends, bool showEnemies)
        {
            if (InspectedCharacter == null) return;

            int visibleColCount = (showFamily ? 1 : 0) + (showFriends ? 1 : 0) + (showEnemies ? 1 : 0);
            if (visibleColCount == 0) return;

            var boxHeight = 160.0f * ImGuiHelpers.GlobalScale;

            using (var table = ImRaii.Table("##RelationsInspectTable", visibleColCount, ImGuiTableFlags.SizingStretchSame))
            {
                if (table.Success)
                {
                    if (showFamily)
                    {
                        ImGui.TableNextColumn();
                        DrawInspectRelationCard("Family", ImGuiColors.ParsedGold,
                            LocalizationManager.Instance.GetLocalizedString("CharFamilyRelationTab"),
                            InspectedCharacter.characterFamily ?? new Dictionary<string, string>(), boxHeight);
                    }

                    if (showFriends)
                    {
                        ImGui.TableNextColumn();
                        DrawInspectRelationCard("Friends", ImGuiColors.ParsedGreen,
                            LocalizationManager.Instance.GetLocalizedString("CharFriendsTab"),
                            InspectedCharacter.characterFriends ?? new Dictionary<string, string>(), boxHeight);
                    }

                    if (showEnemies)
                    {
                        ImGui.TableNextColumn();
                        DrawInspectRelationCard("Enemies", ImGuiColors.DPSRed,
                            LocalizationManager.Instance.GetLocalizedString("CharEnemiesTab"),
                            InspectedCharacter.characterEnnemies ?? new Dictionary<string, string>(), boxHeight);
                    }
                }
            }
        }

        private void DrawInspectRelationCard(string id, Vector4 color, string title, Dictionary<string, string> relations, float height)
        {
            using (var child = ImRaii.Child($"##Inspect{id}Card", new Vector2(0, height), true))
            {
                if (child.Success)
                {
                    ImGui.TextColored(color, title.Replace(":", "").Trim());
                    ImGui.SameLine();
                    UiUtils.Badge(relations.Count.ToString(), new Vector4(0.2f, 0.2f, 0.2f, 0.5f), ImGuiColors.DalamudGrey);
                    ImGui.Separator();

                    if (relations.Count == 0)
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoneText"));
                    }
                    else
                    {
                        using (var relTable = ImRaii.Table($"##Inspect{id}RelTable", 2, ImGuiTableFlags.SizingStretchProp))
                        {
                            if (relTable.Success)
                            {
                                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 75.0f * ImGuiHelpers.GlobalScale);
                                ImGui.TableSetupColumn("Desc", ImGuiTableColumnFlags.WidthStretch, 1.0f);

                                foreach (var kvp in relations)
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();
                                    ImGui.TextColored(color, kvp.Key);
                                    ImGui.TableNextColumn();
                                    ImGui.TextWrapped(kvp.Value);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawPropertyCard(string label, string? value, FontAwesomeIcon icon, Vector4 accentColor, float width = -1f)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var cardWidth = width > 0 ? width : ImGui.GetContentRegionAvail().X;
            var cardHeight = 55.0f * scale;

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.11f, 0.12f, 0.15f, 0.90f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(0.22f, 0.25f, 0.32f, 0.65f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10.0f, 6.0f) * scale))
            using (var child = ImRaii.Child($"##InspectPropCard_{label}", new Vector2(cardWidth, cardHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                if (child.Success)
                {
                    var drawList = ImGui.GetWindowDrawList();
                    var pos = ImGui.GetWindowPos();
                    var size = ImGui.GetWindowSize();

                    drawList.AddRectFilled(
                        pos + new Vector2(2.0f * scale, 4.0f * scale),
                        pos + new Vector2(4.5f * scale, size.Y - 4.0f * scale),
                        ImGui.ColorConvertFloat4ToU32(accentColor),
                        1.5f * scale);

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(accentColor, icon.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * scale);
                    ImGui.TextColored(ImGuiColors.DalamudGrey, label.Replace(":", "").Trim());

                    var displayVal = !string.IsNullOrWhiteSpace(value) ? value : "-";
                    var valCol = !string.IsNullOrWhiteSpace(value) ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey2;
                    ImGui.TextColored(valCol, displayVal);
                }
            }
        }

        private void DrawStoryBlock(string title, string? content, FontAwesomeIcon icon, Vector4 accentColor)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var availWidth = ImGui.GetContentRegionAvail().X;

            using (ImRaii.PushColor(ImGuiCol.ChildBg, new Vector4(0.10f, 0.11f, 0.14f, 0.90f)))
            using (ImRaii.PushColor(ImGuiCol.Border, new Vector4(accentColor.X, accentColor.Y, accentColor.Z, 0.45f)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 6.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(12.0f, 8.0f) * scale))
            using (var child = ImRaii.Child($"##InspectStoryBlock_{title}", new Vector2(availWidth, 0), true, ImGuiWindowFlags.AlwaysAutoResize))
            {
                if (child.Success)
                {
                    var drawList = ImGui.GetWindowDrawList();
                    var pos = ImGui.GetWindowPos();
                    var size = ImGui.GetWindowSize();

                    drawList.AddRectFilled(
                        pos + new Vector2(2.5f * scale, 5.0f * scale),
                        pos + new Vector2(5.5f * scale, size.Y - 5.0f * scale),
                        ImGui.ColorConvertFloat4ToU32(accentColor),
                        1.5f * scale);

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(accentColor, icon.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 6.0f * scale);
                    ImGui.TextColored(accentColor, title.Replace(":", "").Trim());
                    ImGui.Separator();
                    ImGui.Spacing();

                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        ImGui.TextWrapped(content);
                    }
                    else
                    {
                        ImGui.TextDisabled(LocalizationManager.Instance.GetLocalizedString("NoneText"));
                    }
                }
            }
        }
    }
}
