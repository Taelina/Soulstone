using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;

namespace Soulstone.Utils;

internal static class SoulstoneTheme
{
    internal static readonly Vector4 Background = new(0.035f, 0.045f, 0.06f, 1);
    internal static readonly Vector4 Panel = new(0.055f, 0.068f, 0.085f, 1);
    internal static readonly Vector4 Header = new(0.105f, 0.12f, 0.15f, 1);
    internal static readonly Vector4 Field = new(0.085f, 0.10f, 0.125f, 1);
    internal static readonly Vector4 Gold = new(0.90f, 0.75f, 0.43f, 1);
    internal static readonly Vector4 Border = new(0.52f, 0.45f, 0.31f, 0.75f);
    internal static readonly Vector4 Muted = new(0.58f, 0.64f, 0.73f, 1);
    internal static readonly Vector4 Selection = new(0.36f, 0.28f, 0.13f, 0.95f);
    internal static readonly Vector4 Hover = new(0.28f, 0.25f, 0.18f, 1);
    internal static readonly Vector4 Active = new(0.39f, 0.31f, 0.16f, 1);
    private static readonly Dictionary<uint, float> PanelHeights = new();

    internal static ThemeScope Push() => new();
    internal static void ClearCache() => PanelHeights.Clear();

    internal sealed class ThemeScope : IDisposable
    {
        private int colors;
        private int styles;

        public ThemeScope()
        {
            Color(ImGuiCol.WindowBg, Background);
            Color(ImGuiCol.ChildBg, Panel);
            Color(ImGuiCol.PopupBg, Panel);
            Color(ImGuiCol.Text, new Vector4(0.90f, 0.92f, 0.95f, 1));
            Color(ImGuiCol.TextDisabled, Muted);
            Color(ImGuiCol.Border, Border);
            Color(ImGuiCol.BorderShadow, Vector4.Zero);
            Color(ImGuiCol.TitleBg, Header);
            Color(ImGuiCol.TitleBgActive, Header);
            Color(ImGuiCol.TitleBgCollapsed, Background);
            Color(ImGuiCol.FrameBg, Field);
            Color(ImGuiCol.FrameBgHovered, Hover);
            Color(ImGuiCol.FrameBgActive, Active);
            Color(ImGuiCol.Button, Header);
            Color(ImGuiCol.ButtonHovered, Hover);
            Color(ImGuiCol.ButtonActive, Active);
            Color(ImGuiCol.Header, Selection);
            Color(ImGuiCol.HeaderHovered, Hover);
            Color(ImGuiCol.HeaderActive, Active);
            Color(ImGuiCol.CheckMark, Gold);
            Color(ImGuiCol.SliderGrab, Gold);
            Color(ImGuiCol.SliderGrabActive, Gold);
            Color(ImGuiCol.Separator, Border);
            Color(ImGuiCol.Tab, Header);
            Color(ImGuiCol.TabActive, Selection);
            Color(ImGuiCol.TabHovered, Hover);
            Color(ImGuiCol.TableHeaderBg, Header);
            Color(ImGuiCol.TableBorderStrong, Border);
            Color(ImGuiCol.TableBorderLight, Border with { W = 0.35f });
            Color(ImGuiCol.TableRowBg, Panel);
            Color(ImGuiCol.TableRowBgAlt, Field);
            Color(ImGuiCol.ScrollbarBg, Background);
            Color(ImGuiCol.ScrollbarGrab, Border);
            Color(ImGuiCol.ScrollbarGrabHovered, Gold with { W = 0.65f });
            Color(ImGuiCol.ScrollbarGrabActive, Gold);
            var scale = ImGuiHelpers.GlobalScale;
            Style(ImGuiStyleVar.WindowRounding, 8 * scale);
            Style(ImGuiStyleVar.ChildRounding, 8 * scale);
            Style(ImGuiStyleVar.FrameRounding, 5 * scale);
            Style(ImGuiStyleVar.PopupRounding, 8 * scale);
            Style(ImGuiStyleVar.ScrollbarRounding, 5 * scale);
            Style(ImGuiStyleVar.WindowBorderSize, 1);
            Style(ImGuiStyleVar.ChildBorderSize, 1);
            Style(ImGuiStyleVar.FrameBorderSize, 1);
            Style(ImGuiStyleVar.PopupBorderSize, 1);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12, 10) * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(7, 4) * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6) * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 5) * scale);
            styles += 4;
        }

        private void Color(ImGuiCol key, Vector4 value) { ImGui.PushStyleColor(key, value); colors++; }
        private void Style(ImGuiStyleVar key, float value) { ImGui.PushStyleVar(key, value); styles++; }
        public void Dispose()
        {
            ImGui.PopStyleVar(styles);
            ImGui.PopStyleColor(colors);
            styles = colors = 0;
        }
    }

    internal static PanelScope BeginPanel(string id, string title, FontAwesomeIcon? icon = null,
        Action? headerAction = null, float? height = null, bool bodyHasHeader = false) => new(id, title, icon, headerAction, height, bodyHasHeader);

    internal readonly ref struct PanelScope
    {
        private readonly uint key;
        private readonly bool measured;
        private readonly float scale;
        internal bool Success { get; }

        internal PanelScope(string id, string title, FontAwesomeIcon? icon, Action? headerAction, float? height, bool bodyHasHeader)
        {
            scale = ImGuiHelpers.GlobalScale;
            key = ImGui.GetID(id);
            measured = !height.HasValue;
            var panelHeight = height ?? (PanelHeights.TryGetValue(key, out var cached) ? cached : 240 * scale);
            ImGui.PushStyleColor(ImGuiCol.ChildBg, Panel);
            ImGui.PushStyleColor(ImGuiCol.Border, Border);
            ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8 * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12, 10) * scale);
            Success = ImGui.BeginChild(id, new Vector2(0, panelHeight), true,
                measured ? ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse : ImGuiWindowFlags.None);
            if (!Success) return;
            var pos = ImGui.GetWindowPos();
            var width = ImGui.GetWindowSize().X;
            var draw = ImGui.GetWindowDrawList();
            draw.AddRectFilled(pos + Vector2.One, pos + new Vector2(width - 1, 40 * scale), ImGui.ColorConvertFloat4ToU32(Header), 7 * scale);
            draw.AddLine(pos + new Vector2(10, 40) * scale, pos + new Vector2(width - 10 * scale, 40 * scale), ImGui.ColorConvertFloat4ToU32(Border));
            if (bodyHasHeader) return;
            if (icon.HasValue)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(Gold, icon.Value.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 10 * scale);
            }
            var textPosition = ImGui.GetCursorScreenPos();
            var textRight = pos.X + width - (headerAction == null ? 12 : 48) * scale;
            ImGui.PushClipRect(textPosition, new Vector2(Math.Max(textPosition.X, textRight), pos.Y + 40 * scale), true);
            ImGui.TextColored(Gold, title.Trim().TrimEnd(':').TrimEnd());
            ImGui.PopClipRect();
            if (headerAction != null)
            {
                ImGui.SameLine(width - 40 * scale);
                headerAction();
            }
            ImGui.SetCursorPosY(49 * scale);
        }

        public void Dispose()
        {
            if (Success && measured)
                PanelHeights[key] = ImGui.GetCursorPosY() + 10 * scale;
            ImGui.EndChild();
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(2);
        }
    }
}
