using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Soulstone.Datamodels;
using Soulstone.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Utils
{
    internal class UiUtils
    {
        private static float defaultNextToSpace = 3.0f;
        private static float defaultFieldSpacing = 10.0f;
        private static float defaultInputWidth = 175.0f;

        public static float DefaultInputWidth { get => defaultInputWidth * ImGuiHelpers.GlobalScale; set => defaultInputWidth = value; }
        public static float DefaultFieldSpacing { get => defaultFieldSpacing * ImGuiHelpers.GlobalScale; set => defaultFieldSpacing = value; }
        public static float DefaultNextToSpace { get => defaultNextToSpace * ImGuiHelpers.GlobalScale; set => defaultNextToSpace = value; }

        public static void ManageInputField(ref string field, string fieldname, bool editing, float width = 175.0f)
        {
            if (editing)
            {
                StyledInputText(fieldname, ref field, 200, width: width);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 0.7f), "—");
                }
                else
                {
                    ImGui.TextUnformatted(field);
                }
            }
        }

        public static void ManageInputField(ref int field, string fieldname, bool editing, float width = 50.0f)
        {
            if (editing)
            {
                StyledInputInt(fieldname, ref field, step: 0, width: width);
            }
            else
            {
                ImGui.TextUnformatted(field.ToString());
            }
        }

        public static void ManageInputField(ref float field, string fieldname, bool editing, float width = 50.0f, string format = "%.2f")
        {
            if (editing)
            {
                StyledInputFloat(fieldname, ref field, step: 0.1f, format: format, width: width);
            }
            else
            {
                ImGui.TextUnformatted(field.ToString("0.##"));
            }
        }

        public static void ManageBigInputField(ref string field, string fieldname, bool editing, float height = 80.0f)
        {
            if (editing)
            {
                StyledInputMultiline(fieldname, ref field, 5000, new Vector2(-1.0f, height * ImGuiHelpers.GlobalScale));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 0.7f), "—");
                }
                else
                {
                    ImGui.TextWrapped(field);
                }
            }
        }

        #region Styled Input Controls

        public static bool StyledInputText(
            string id,
            ref string text,
            int maxLength = 200,
            float width = -1.0f,
            string hint = "",
            FontAwesomeIcon? icon = null,
            bool readOnly = false,
            Vector4? borderColor = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            if (width > 0)
            {
                ImGui.SetNextItemWidth(width * scale);
            }
            else if (width < 0)
            {
                ImGui.SetNextItemWidth(-1.0f);
            }

            if (readOnly)
            {
                ImGui.BeginDisabled();
            }

            var bgCol = new Vector4(0.10f, 0.12f, 0.16f, 0.85f);
            var bgHoverCol = new Vector4(0.15f, 0.18f, 0.24f, 0.95f);
            var bgActiveCol = new Vector4(0.18f, 0.22f, 0.30f, 1.00f);
            var border = borderColor ?? new Vector4(0.26f, 0.30f, 0.40f, 0.70f);

            bool changed;
            using (ImRaii.PushColor(ImGuiCol.FrameBg, bgCol))
            using (ImRaii.PushColor(ImGuiCol.FrameBgHovered, bgHoverCol))
            using (ImRaii.PushColor(ImGuiCol.FrameBgActive, bgActiveCol))
            using (ImRaii.PushColor(ImGuiCol.Border, border))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.5f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1.0f))
            using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(8.0f, 4.5f) * scale))
            {
                if (icon.HasValue)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(border, icon.Value.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 4.0f * scale);
                    if (width > 0)
                    {
                        var iconW = ImGui.CalcTextSize(icon.Value.ToIconString()).X + 4.0f * scale;
                        ImGui.SetNextItemWidth(Math.Max(30.0f * scale, (width * scale) - iconW));
                    }
                }

                if (!string.IsNullOrEmpty(hint))
                {
                    changed = ImGui.InputTextWithHint($"##{id}", hint, ref text, maxLength);
                }
                else
                {
                    changed = ImGui.InputText($"##{id}", ref text, maxLength);
                }
            }

            if (readOnly)
            {
                ImGui.EndDisabled();
            }

            return changed;
        }

        public static bool StyledInputInt(
            string id,
            ref int value,
            int step = 1,
            int stepFast = 100,
            float width = -1.0f,
            int? min = null,
            int? max = null,
            FontAwesomeIcon? icon = null,
            Vector4? borderColor = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            if (width > 0)
            {
                ImGui.SetNextItemWidth(width * scale);
            }
            else if (width < 0)
            {
                ImGui.SetNextItemWidth(-1.0f);
            }

            var bgCol = new Vector4(0.10f, 0.12f, 0.16f, 0.85f);
            var bgHoverCol = new Vector4(0.15f, 0.18f, 0.24f, 0.95f);
            var bgActiveCol = new Vector4(0.18f, 0.22f, 0.30f, 1.00f);
            var border = borderColor ?? new Vector4(0.26f, 0.30f, 0.40f, 0.70f);

            using (ImRaii.PushColor(ImGuiCol.FrameBg, bgCol))
            using (ImRaii.PushColor(ImGuiCol.FrameBgHovered, bgHoverCol))
            using (ImRaii.PushColor(ImGuiCol.FrameBgActive, bgActiveCol))
            using (ImRaii.PushColor(ImGuiCol.Border, border))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.5f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1.0f))
            using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(8.0f, 4.5f) * scale))
            {
                if (icon.HasValue)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(border, icon.Value.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 4.0f * scale);
                    if (width > 0)
                    {
                        var iconW = ImGui.CalcTextSize(icon.Value.ToIconString()).X + 4.0f * scale;
                        ImGui.SetNextItemWidth(Math.Max(30.0f * scale, (width * scale) - iconW));
                    }
                }

                int prev = value;
                bool changed = ImGui.InputInt($"##{id}", ref value, step, stepFast);
                if (min.HasValue && value < min.Value) { value = min.Value; changed = true; }
                if (max.HasValue && value > max.Value) { value = max.Value; changed = true; }
                return changed || (value != prev);
            }
        }

        public static bool StyledInputFloat(
            string id,
            ref float value,
            float step = 0.1f,
            float stepFast = 1.0f,
            string format = "%.2f",
            float width = -1.0f,
            float? min = null,
            float? max = null,
            FontAwesomeIcon? icon = null,
            Vector4? borderColor = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            if (width > 0)
            {
                ImGui.SetNextItemWidth(width * scale);
            }
            else if (width < 0)
            {
                ImGui.SetNextItemWidth(-1.0f);
            }

            var bgCol = new Vector4(0.10f, 0.12f, 0.16f, 0.85f);
            var bgHoverCol = new Vector4(0.15f, 0.18f, 0.24f, 0.95f);
            var bgActiveCol = new Vector4(0.18f, 0.22f, 0.30f, 1.00f);
            var border = borderColor ?? new Vector4(0.26f, 0.30f, 0.40f, 0.70f);

            using (ImRaii.PushColor(ImGuiCol.FrameBg, bgCol))
            using (ImRaii.PushColor(ImGuiCol.FrameBgHovered, bgHoverCol))
            using (ImRaii.PushColor(ImGuiCol.FrameBgActive, bgActiveCol))
            using (ImRaii.PushColor(ImGuiCol.Border, border))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.5f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1.0f))
            using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(8.0f, 4.5f) * scale))
            {
                if (icon.HasValue)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(border, icon.Value.ToIconString());
                    ImGui.PopFont();
                    ImGui.SameLine(0, 4.0f * scale);
                    if (width > 0)
                    {
                        var iconW = ImGui.CalcTextSize(icon.Value.ToIconString()).X + 4.0f * scale;
                        ImGui.SetNextItemWidth(Math.Max(30.0f * scale, (width * scale) - iconW));
                    }
                }

                float prev = value;
                bool changed = ImGui.InputFloat($"##{id}", ref value, step, stepFast, format);
                if (min.HasValue && value < min.Value) { value = min.Value; changed = true; }
                if (max.HasValue && value > max.Value) { value = max.Value; changed = true; }
                return changed || (Math.Abs(value - prev) > 0.0001f);
            }
        }

        public static bool StyledInputMultiline(
            string id,
            ref string text,
            int maxLength = 5000,
            Vector2? size = null,
            Vector4? borderColor = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var targetSize = size ?? new Vector2(-1.0f, 80.0f * scale);
            if (targetSize.X == 0) targetSize.X = -1.0f;
            if (targetSize.Y <= 0) targetSize.Y = 80.0f * scale;

            var bgCol = new Vector4(0.10f, 0.12f, 0.16f, 0.85f);
            var bgHoverCol = new Vector4(0.15f, 0.18f, 0.24f, 0.95f);
            var bgActiveCol = new Vector4(0.18f, 0.22f, 0.30f, 1.00f);
            var border = borderColor ?? new Vector4(0.26f, 0.30f, 0.40f, 0.70f);

            using (ImRaii.PushColor(ImGuiCol.FrameBg, bgCol))
            using (ImRaii.PushColor(ImGuiCol.FrameBgHovered, bgHoverCol))
            using (ImRaii.PushColor(ImGuiCol.FrameBgActive, bgActiveCol))
            using (ImRaii.PushColor(ImGuiCol.Border, border))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 5.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1.0f))
            using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(8.0f, 6.0f) * scale))
            {
                ImGui.SetNextItemWidth(targetSize.X);
                return ImGui.InputTextMultiline($"##{id}", ref text, maxLength, targetSize);
            }
        }

        public static bool StyledCollapsingHeader(
            string label,
            bool defaultOpen = false,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            string? badgeText = null,
            Vector4? badgeColor = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var accent = accentColor ?? ImGuiColors.ParsedGold;

            var headerBg = new Vector4(0.12f, 0.14f, 0.18f, 0.90f);
            var headerHover = new Vector4(0.18f, 0.21f, 0.27f, 0.95f);
            var headerActive = new Vector4(0.22f, 0.26f, 0.34f, 1.0f);
            var headerBorder = new Vector4(0.28f, 0.32f, 0.42f, 0.70f);

            var flags = ImGuiTreeNodeFlags.Framed | ImGuiTreeNodeFlags.SpanAvailWidth;
            if (defaultOpen)
            {
                flags |= ImGuiTreeNodeFlags.DefaultOpen;
            }

            string headerLabel = icon.HasValue ? $"      {label}" : label;

            using (ImRaii.PushColor(ImGuiCol.Header, headerBg))
            using (ImRaii.PushColor(ImGuiCol.HeaderHovered, headerHover))
            using (ImRaii.PushColor(ImGuiCol.HeaderActive, headerActive))
            using (ImRaii.PushColor(ImGuiCol.Border, headerBorder))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 6.0f * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(10.0f, 7.0f) * scale))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 1.0f))
            {
                var startPos = ImGui.GetCursorScreenPos();
                bool isOpen = ImGui.CollapsingHeader(headerLabel, flags);

                var headerHeight = ImGui.GetFrameHeight();
                var drawList = ImGui.GetWindowDrawList();

                // Left accent stripe
                drawList.AddRectFilled(
                    startPos + new Vector2(2.0f * scale, 3.0f * scale),
                    startPos + new Vector2(5.5f * scale, headerHeight - 3.0f * scale),
                    ImGui.ColorConvertFloat4ToU32(accent),
                    2.0f * scale);

                // Draw icon next to collapsing arrow
                if (icon.HasValue)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    var iconStr = icon.Value.ToIconString();
                    var iconH = ImGui.CalcTextSize(iconStr).Y;
                    var iconPos = startPos + new Vector2(24.0f * scale, (headerHeight - iconH) * 0.5f);
                    drawList.AddText(iconPos, ImGui.ColorConvertFloat4ToU32(accent), iconStr);
                    ImGui.PopFont();
                }

                if (!string.IsNullOrEmpty(badgeText))
                {
                    var bColor = badgeColor ?? accent;
                    var textSize = ImGui.CalcTextSize(badgeText);
                    var badgeW = textSize.X + 16.0f * scale;
                    var badgeH = 18.0f * scale;
                    var availW = ImGui.GetContentRegionAvail().X;
                    var badgePos = startPos + new Vector2(availW - badgeW - 10.0f * scale, (headerHeight - badgeH) * 0.5f);

                    drawList.AddRectFilled(badgePos, badgePos + new Vector2(badgeW, badgeH), ImGui.ColorConvertFloat4ToU32(new Vector4(bColor.X * 0.25f, bColor.Y * 0.25f, bColor.Z * 0.25f, 0.85f)), badgeH * 0.5f);
                    drawList.AddRect(badgePos, badgePos + new Vector2(badgeW, badgeH), ImGui.ColorConvertFloat4ToU32(bColor), badgeH * 0.5f);
                    drawList.AddText(badgePos + new Vector2(8.0f * scale, (badgeH - textSize.Y) * 0.5f), ImGui.ColorConvertFloat4ToU32(bColor), badgeText);
                }

                return isOpen;
            }
        }

        #endregion

        public static void SectionHeader(string title, Vector4? titleColor = null)
        {
            DrawSectionHeader(title, null, titleColor);
        }

        public static void DrawSectionHeader(
            string title,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            string? badgeText = null,
            Vector4? badgeColor = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var accent = accentColor ?? ImGuiColors.ParsedGold;
            ImGui.Spacing();

            var startPos = ImGui.GetCursorScreenPos();
            var lineHeight = ImGui.GetTextLineHeight();
            var barWidth = 3.5f * scale;
            var drawList = ImGui.GetWindowDrawList();

            // Accent bar on the left
            drawList.AddRectFilled(
                startPos,
                new Vector2(startPos.X + barWidth, startPos.Y + lineHeight),
                ImGui.ColorConvertFloat4ToU32(accent),
                2.0f * scale);

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + barWidth + 6.0f * scale);

            if (icon.HasValue)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(accent, icon.Value.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 6.0f * scale);
            }

            ImGui.TextColored(accent, title);

            if (!string.IsNullOrWhiteSpace(badgeText))
            {
                ImGui.SameLine(0, 8.0f * scale);
                var bg = badgeColor ?? new Vector4(accent.X, accent.Y, accent.Z, 0.25f);
                var textCol = new Vector4(Math.Min(1f, accent.X * 1.3f), Math.Min(1f, accent.Y * 1.3f), Math.Min(1f, accent.Z * 1.3f), 1.0f);
                PillBadge(badgeText, bg, textCol);
            }

            ImGui.Separator();
            ImGui.Spacing();
        }

        public static void PillBadge(string text, Vector4 bgCol, Vector4 textCol, FontAwesomeIcon? icon = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var padding = new Vector2(8.0f, 3.0f) * scale;
            float iconWidth = 0;
            if (icon.HasValue)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                iconWidth = ImGui.CalcTextSize(icon.Value.ToIconString()).X + 4.0f * scale;
                ImGui.PopFont();
            }

            var textSize = ImGui.CalcTextSize(text);
            var size = new Vector2(textSize.X + iconWidth + padding.X * 2.0f, textSize.Y + padding.Y * 2.0f);
            var pos = ImGui.GetCursorScreenPos();

            var drawList = ImGui.GetWindowDrawList();
            var col = ImGui.ColorConvertFloat4ToU32(bgCol);
            var borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(textCol.X, textCol.Y, textCol.Z, 0.45f));

            drawList.AddRectFilled(pos, pos + size, col, size.Y * 0.5f);
            drawList.AddRect(pos, pos + size, borderCol, size.Y * 0.5f, ImDrawFlags.None, 1.0f);

            var textPos = pos + padding;
            if (icon.HasValue)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(textCol), icon.Value.ToIconString());
                ImGui.PopFont();
                textPos.X += iconWidth;
            }

            drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(textCol), text);
            ImGui.Dummy(size);
        }

        public static void DrawStatCard(
            string label,
            string value,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            float width = 110.0f,
            string tooltip = "")
        {
            var scale = ImGuiHelpers.GlobalScale;
            var w = width * scale;
            var h = 52.0f * scale;
            var accent = accentColor ?? ImGuiColors.ParsedGold;
            var pos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            var isHovered = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(w, h));
            var bgCol = isHovered 
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.18f, 0.22f, 0.95f))
                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.12f, 0.14f, 0.90f));
            var borderCol = isHovered
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X, accent.Y, accent.Z, 0.7f))
                : ImGui.ColorConvertFloat4ToU32(new Vector4(0.28f, 0.28f, 0.32f, 0.5f));

            drawList.AddRectFilled(pos, pos + new Vector2(w, h), bgCol, 6.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(w, h), borderCol, 6.0f * scale, ImDrawFlags.None, isHovered ? 1.5f : 1.0f);

            // Left accent bar
            drawList.AddRectFilled(
                pos + new Vector2(2f * scale, 6f * scale),
                pos + new Vector2(4.5f * scale, h - 6f * scale),
                ImGui.ColorConvertFloat4ToU32(accent),
                2.0f * scale);

            // Inner content
            ImGui.SetCursorScreenPos(pos + new Vector2(10.0f * scale, 6.0f * scale));
            ImGui.BeginGroup();
            if (icon.HasValue)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(accent, icon.Value.ToIconString());
                ImGui.PopFont();
                ImGui.SameLine(0, 4.0f * scale);
            }
            ImGui.TextColored(accent, value);
            ImGui.TextColored(ImGuiColors.DalamudGrey, label);
            ImGui.EndGroup();

            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(new Vector2(w, h));

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }

        public static void DrawProgressBar(
            float current,
            float max,
            string overlayText = "",
            Vector2? size = null,
            Vector4? fillColor = null,
            Vector4? bgColor = null,
            Vector4? borderColor = null,
            float rounding = 4.0f)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var w = size?.X ?? ImGui.GetContentRegionAvail().X;
            if (w < 0) w = ImGui.GetContentRegionAvail().X;
            var h = size?.Y ?? (18.0f * scale);
            var pos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            var fraction = max > 0 ? Math.Clamp(current / max, 0f, 1f) : 0f;
            var fill = fillColor ?? ImGuiColors.ParsedGreen;
            var bg = bgColor ?? new Vector4(0.10f, 0.10f, 0.13f, 0.95f);
            var border = borderColor ?? new Vector4(0.28f, 0.28f, 0.35f, 0.75f);

            float skew = Math.Min(w * 0.12f, Math.Max(4.0f * scale, 6.0f * scale));

            // Outer / background track parallelogram vertices:
            // P0 (top-left), P1 (top-right), P2 (bottom-right), P3 (bottom-left)
            Vector2 p0 = new Vector2(pos.X + skew, pos.Y);
            Vector2 p1 = new Vector2(pos.X + w, pos.Y);
            Vector2 p2 = new Vector2(pos.X + w - skew, pos.Y + h);
            Vector2 p3 = new Vector2(pos.X, pos.Y + h);

            // Fill background track
            drawList.AddQuadFilled(p0, p1, p2, p3, ImGui.ColorConvertFloat4ToU32(bg));

            // Filled bar portion
            if (fraction > 0f)
            {
                float fillWidth = Math.Max(skew + 2.0f * scale, w * fraction);
                if (fillWidth > w) fillWidth = w;

                Vector2 fp0 = new Vector2(pos.X + skew, pos.Y);
                Vector2 fp1 = new Vector2(pos.X + fillWidth, pos.Y);
                Vector2 fp2 = new Vector2(pos.X + Math.Max(0, fillWidth - skew), pos.Y + h);
                Vector2 fp3 = new Vector2(pos.X, pos.Y + h);

                drawList.AddQuadFilled(fp0, fp1, fp2, fp3, ImGui.ColorConvertFloat4ToU32(fill));

                // Top highlight line for polished glass / specular depth look
                var highlightCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.25f));
                drawList.AddLine(
                    new Vector2(fp0.X + 1.0f * scale, pos.Y + 1.5f * scale),
                    new Vector2(fp1.X - 1.0f * scale, pos.Y + 1.5f * scale),
                    highlightCol,
                    1.0f * scale);
            }

            // Outer border quad
            drawList.AddQuad(p0, p1, p2, p3, ImGui.ColorConvertFloat4ToU32(border), 1.2f * scale);

            // Overlay text centered
            var label = !string.IsNullOrEmpty(overlayText) ? overlayText : $"{current:0.#} / {max:0.#}";
            var labelSize = ImGui.CalcTextSize(label);
            var textPos = pos + new Vector2((w - labelSize.X) * 0.5f, (h - labelSize.Y) * 0.5f);

            // Drop shadow + text
            drawList.AddText(textPos + new Vector2(1, 1), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.95f)), label);
            drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), label);

            ImGui.Dummy(new Vector2(w, h));
        }

        public static bool DrawSectionedBar(
            int current,
            int max,
            string overlayText = "",
            Vector2? size = null,
            Vector4? activeColor = null,
            Vector4? inactiveColor = null,
            Vector4? borderColor = null,
            float gap = 3.0f,
            float rounding = 3.0f,
            Action<int>? onSegmentClick = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var totalWidth = size?.X ?? ImGui.GetContentRegionAvail().X;
            if (totalWidth < 0) totalWidth = ImGui.GetContentRegionAvail().X;
            var height = size?.Y ?? (18.0f * scale);
            var startPos = ImGui.GetCursorScreenPos();
            var drawList = ImGui.GetWindowDrawList();

            int segments = Math.Max(1, max);
            float scaledGap = gap * scale;
            float skew = Math.Min(height * 0.35f, 5.0f * scale);

            var fillCol = activeColor ?? ImGuiColors.ParsedGold;
            var emptyCol = inactiveColor ?? new Vector4(0.12f, 0.12f, 0.16f, 0.9f);
            var borderCol = borderColor ?? new Vector4(0.30f, 0.30f, 0.38f, 0.70f);

            bool clicked = false;
            if (segments <= 20)
            {
                float totalGaps = (segments - 1) * scaledGap;
                float segWidth = Math.Max(5.0f * scale, (totalWidth - totalGaps) / segments);

                for (int i = 0; i < segments; i++)
                {
                    float segLeft = startPos.X + i * (segWidth + scaledGap);
                    Vector2 p0 = new Vector2(segLeft + skew, startPos.Y);
                    Vector2 p1 = new Vector2(segLeft + segWidth, startPos.Y);
                    Vector2 p2 = new Vector2(segLeft + segWidth - skew, startPos.Y + height);
                    Vector2 p3 = new Vector2(segLeft, startPos.Y + height);

                    Vector2 hitMin = new Vector2(segLeft, startPos.Y);
                    Vector2 hitMax = new Vector2(segLeft + segWidth, startPos.Y + height);

                    bool isHovered = ImGui.IsMouseHoveringRect(hitMin, hitMax);
                    if (isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                    {
                        onSegmentClick?.Invoke(i + 1 == current ? i : i + 1);
                        clicked = true;
                    }

                    bool isActive = i < current;
                    var bg = isActive ? fillCol : emptyCol;
                    if (isHovered)
                    {
                        bg = new Vector4(Math.Min(1f, bg.X * 1.25f), Math.Min(1f, bg.Y * 1.25f), Math.Min(1f, bg.Z * 1.25f), 1.0f);
                    }

                    var bCol = isHovered
                        ? ImGui.ColorConvertFloat4ToU32(new Vector4(fillCol.X, fillCol.Y, fillCol.Z, 0.95f))
                        : (isActive
                            ? ImGui.ColorConvertFloat4ToU32(new Vector4(fillCol.X, fillCol.Y, fillCol.Z, 0.6f))
                            : ImGui.ColorConvertFloat4ToU32(borderCol));

                    drawList.AddQuadFilled(p0, p1, p2, p3, ImGui.ColorConvertFloat4ToU32(bg));
                    drawList.AddQuad(p0, p1, p2, p3, bCol, isHovered ? 1.5f * scale : 1.0f * scale);

                    if (isActive)
                    {
                        var hlCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.35f));
                        drawList.AddLine(
                            new Vector2(p0.X + 1.0f * scale, p0.Y + 1.5f * scale),
                            new Vector2(p1.X - 1.0f * scale, p1.Y + 1.5f * scale),
                            hlCol,
                            1.0f * scale);
                    }
                }
            }
            else
            {
                // For high counter counts, fallback to smooth progress bar with section subdivisions
                DrawProgressBar(current, max, overlayText, new Vector2(totalWidth, height), fillCol, emptyCol, borderCol, rounding);
                return false;
            }

            if (!string.IsNullOrEmpty(overlayText))
            {
                var labelSize = ImGui.CalcTextSize(overlayText);
                var textPos = startPos + new Vector2((totalWidth - labelSize.X) * 0.5f, (height - labelSize.Y) * 0.5f);
                drawList.AddText(textPos + new Vector2(1, 1), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.95f)), overlayText);
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)), overlayText);
            }

            ImGui.Dummy(new Vector2(totalWidth, height));
            return clicked;
        }

        public static bool DrawSearchInput(string id, ref string searchText, string hint = "", float width = 200.0f)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var searchHint = !string.IsNullOrEmpty(hint) ? hint : LocalizationManager.Instance.GetLocalizedString("SearchFilterHint");

            bool changed = StyledInputText(id, ref searchText, 100, width: width, hint: searchHint, icon: FontAwesomeIcon.Search);

            if (!string.IsNullOrEmpty(searchText))
            {
                ImGui.SameLine(0, 4.0f * scale);
                if (IconButton($"ClearSearch_{id}", FontAwesomeIcon.Times, LocalizationManager.Instance.GetLocalizedString("ClearSearchTooltip"), new Vector2(22, 22) * scale))
                {
                    searchText = string.Empty;
                    changed = true;
                }
            }

            return changed;
        }

        public static bool DrawFilterChip(string label, bool isSelected, int? count = null, FontAwesomeIcon? icon = null, Vector4? activeColor = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var accent = activeColor ?? ImGuiColors.ParsedGold;
            var displayLabel = count.HasValue ? $"{label} ({count.Value})" : label;

            var padding = new Vector2(10.0f, 4.0f) * scale;
            float iconWidth = 0;
            if (icon.HasValue)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                iconWidth = ImGui.CalcTextSize(icon.Value.ToIconString()).X + 4.0f * scale;
                ImGui.PopFont();
            }

            var textSize = ImGui.CalcTextSize(displayLabel);
            var size = new Vector2(textSize.X + iconWidth + padding.X * 2.0f, textSize.Y + padding.Y * 2.0f);
            var pos = ImGui.GetCursorScreenPos();

            var isHovered = ImGui.IsMouseHoveringRect(pos, pos + size);
            bool clicked = isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left);

            var bgCol = isSelected
                ? new Vector4(accent.X, accent.Y, accent.Z, 0.35f)
                : (isHovered ? new Vector4(0.25f, 0.25f, 0.28f, 0.8f) : new Vector4(0.14f, 0.14f, 0.16f, 0.8f));

            var borderCol = isSelected
                ? accent
                : (isHovered ? new Vector4(0.5f, 0.5f, 0.55f, 0.8f) : new Vector4(0.28f, 0.28f, 0.32f, 0.6f));

            var textCol = isSelected ? new Vector4(1f, 1f, 1f, 1f) : (isHovered ? ImGuiColors.DalamudWhite : ImGuiColors.DalamudGrey);

            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(pos, pos + size, ImGui.ColorConvertFloat4ToU32(bgCol), size.Y * 0.5f);
            drawList.AddRect(pos, pos + size, ImGui.ColorConvertFloat4ToU32(borderCol), size.Y * 0.5f, ImDrawFlags.None, isSelected ? 1.5f : 1.0f);

            var textPos = pos + padding;
            if (icon.HasValue)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(textCol), icon.Value.ToIconString());
                ImGui.PopFont();
                textPos.X += iconWidth;
            }

            drawList.AddText(textPos, ImGui.ColorConvertFloat4ToU32(textCol), displayLabel);
            ImGui.Dummy(size);

            return clicked;
        }

        public static CardScope BeginCard(string id, Vector2 size, Vector4? bgColor = null, Vector4? borderColor = null, float rounding = 6.0f)
        {
            return new CardScope(id, size, bgColor, borderColor, rounding);
        }

        public ref struct CardScope
        {
            private readonly string id;
            private readonly Vector2 size;
            private readonly Vector4? borderColor;
            private readonly float rounding;
            private readonly Vector2 screenPos;
            private readonly bool success;

            public bool Success => success;

            public CardScope(string id, Vector2 size, Vector4? bgColor, Vector4? borderColor, float rounding)
            {
                this.id = id;
                this.size = size;
                this.borderColor = borderColor;
                this.rounding = rounding;
                this.screenPos = ImGui.GetCursorScreenPos();

                var scale = ImGuiHelpers.GlobalScale;
                var bg = bgColor ?? new Vector4(0.12f, 0.12f, 0.14f, 0.90f);

                ImGui.PushStyleColor(ImGuiCol.ChildBg, bg);
                ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, rounding * scale);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8f, 8f) * scale);

                success = ImGui.BeginChild(id, size, true, ImGuiWindowFlags.NoScrollbar);
            }

            public void Dispose()
            {
                ImGui.EndChild();
                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor();

                var scale = ImGuiHelpers.GlobalScale;
                var border = borderColor ?? new Vector4(0.28f, 0.28f, 0.32f, 0.6f);
                var drawList = ImGui.GetWindowDrawList();
                var isHovered = ImGui.IsMouseHoveringRect(screenPos, screenPos + size);
                if (isHovered)
                {
                    border = new Vector4(Math.Min(1f, border.X * 1.4f), Math.Min(1f, border.Y * 1.4f), Math.Min(1f, border.Z * 1.4f), 0.9f);
                }
                drawList.AddRect(screenPos, screenPos + size, ImGui.ColorConvertFloat4ToU32(border), rounding * scale, ImDrawFlags.None, isHovered ? 1.5f : 1.0f);
            }
        }

        public static void HelpMarker(string desc)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        public static bool SmallButton(string label, string tooltip = "", Vector2? size = null, bool enabled = true)
        {
            bool clicked = false;
            if (!enabled)
            {
                ImGui.BeginDisabled();
            }

            var scale = ImGuiHelpers.GlobalScale;
            var textSize = ImGui.CalcTextSize(label);
            if (size.HasValue)
            {
                var requested = size.Value;
                float minW = textSize.X + 8.0f * scale;
                float minH = textSize.Y + 4.0f * scale;
                float finalW = requested.X > 0 ? Math.Max(requested.X, minW) : 0;
                float finalH = requested.Y > 0 ? Math.Max(requested.Y, minH) : 0;

                float padX = finalW > 0 ? Math.Max(2.0f * scale, (finalW - textSize.X) * 0.5f) : ImGui.GetStyle().FramePadding.X;
                float padY = finalH > 0 ? Math.Max(2.0f * scale, (finalH - textSize.Y) * 0.5f) : ImGui.GetStyle().FramePadding.Y;

                using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(padX, padY)))
                {
                    clicked = ImGui.Button(label, new Vector2(finalW, finalH));
                }
            }
            else
            {
                clicked = ImGui.Button(label);
            }

            if (!enabled)
            {
                ImGui.EndDisabled();
            }
            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
            return clicked;
        }

        public static bool IconButton(string id, FontAwesomeIcon icon, string tooltip = "", Vector2? size = null, bool enabled = true)
        {
            bool clicked = false;
            if (!enabled)
            {
                ImGui.BeginDisabled();
            }

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = icon.ToIconString();
            var iconSize = ImGui.CalcTextSize(iconStr);
            var scale = ImGuiHelpers.GlobalScale;

            if (size.HasValue)
            {
                var requested = size.Value;
                // Determine minimal dimensions to prevent clipping the glyph
                float minW = iconSize.X + 6.0f * scale;
                float minH = iconSize.Y + 4.0f * scale;

                float finalW = requested.X > 0 ? Math.Max(requested.X, minW) : 0;
                float finalH = requested.Y > 0 ? Math.Max(requested.Y, minH) : 0;

                // Adjust frame padding dynamically so the icon is centered and never truncated
                float padX = finalW > 0 ? Math.Max(2.0f * scale, (finalW - iconSize.X) * 0.5f) : ImGui.GetStyle().FramePadding.X;
                float padY = finalH > 0 ? Math.Max(2.0f * scale, (finalH - iconSize.Y) * 0.5f) : ImGui.GetStyle().FramePadding.Y;

                using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(padX, padY)))
                {
                    clicked = ImGui.Button($"{iconStr}###{id}", new Vector2(finalW, finalH));
                }
            }
            else
            {
                var defaultPad = ImGui.GetStyle().FramePadding;
                float padX = Math.Max(defaultPad.X, 6.0f * scale);
                float padY = Math.Max(defaultPad.Y, 3.5f * scale);
                using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(padX, padY)))
                {
                    clicked = ImGui.Button($"{iconStr}###{id}");
                }
            }
            ImGui.PopFont();

            if (!enabled)
            {
                ImGui.EndDisabled();
            }

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }

            return clicked;
        }

        public static bool IconTextButton(string id, FontAwesomeIcon icon, string text, string tooltip = "", Vector2? size = null, bool enabled = true)
        {
            bool clicked = false;
            if (!enabled)
            {
                ImGui.BeginDisabled();
            }

            var scale = ImGuiHelpers.GlobalScale;
            var style = ImGui.GetStyle();

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = icon.ToIconString();
            var iconSize = ImGui.CalcTextSize(iconStr);
            ImGui.PopFont();

            var textSize = ImGui.CalcTextSize(text);
            float spacing = 6.0f * scale;
            float contentW = iconSize.X + spacing + textSize.X;
            float contentH = Math.Max(iconSize.Y, textSize.Y);

            float padX = Math.Max(style.FramePadding.X, 8.0f * scale);
            float padY = Math.Max(style.FramePadding.Y, 4.0f * scale);

            float finalW = size.HasValue && size.Value.X > 0 ? Math.Max(size.Value.X, contentW + padX * 2.0f) : contentW + padX * 2.0f;
            float finalH = size.HasValue && size.Value.Y > 0 ? Math.Max(size.Value.Y, contentH + padY * 2.0f) : contentH + padY * 2.0f;

            var pos = ImGui.GetCursorScreenPos();
            clicked = ImGui.Button($"###{id}", new Vector2(finalW, finalH));

            var isHovered = ImGui.IsItemHovered();
            var drawList = ImGui.GetWindowDrawList();

            float startX = pos.X + (finalW - contentW) * 0.5f;
            float iconY = pos.Y + (finalH - iconSize.Y) * 0.5f;
            float textY = pos.Y + (finalH - textSize.Y) * 0.5f;

            var textCol = ImGui.GetColorU32(ImGuiCol.Text);

            ImGui.PushFont(UiBuilder.IconFont);
            drawList.AddText(new Vector2(startX, iconY), textCol, iconStr);
            ImGui.PopFont();

            drawList.AddText(new Vector2(startX + iconSize.X + spacing, textY), textCol, text);

            if (!enabled)
            {
                ImGui.EndDisabled();
            }

            if (!string.IsNullOrEmpty(tooltip) && isHovered)
            {
                ImGui.SetTooltip(tooltip);
            }

            return clicked;
        }

        #region Styled Dropdown / Combo Box Controls

        public static bool StyledCombo(
            string id,
            ref int selectedIndex,
            string[] items,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            float width = -1)
        {
            if (items == null || items.Length == 0) return false;
            if (selectedIndex < 0) selectedIndex = 0;
            if (selectedIndex >= items.Length) selectedIndex = items.Length - 1;

            string preview = items[selectedIndex];
            bool changed = false;

            if (BeginStyledCombo(id, preview, icon, accentColor, width: width))
            {
                for (int i = 0; i < items.Length; i++)
                {
                    bool isSelected = (i == selectedIndex);
                    if (StyledSelectable(items[i], isSelected, accentColor: accentColor))
                    {
                        selectedIndex = i;
                        changed = true;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                EndStyledCombo();
            }

            return changed;
        }

        public static bool StyledCombo<T>(
            string id,
            ref T selectedItem,
            T[] items,
            Func<T, string>? formatItem = null,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            float width = -1) where T : Enum
        {
            if (items == null || items.Length == 0) return false;
            int currentIdx = Array.IndexOf(items, selectedItem);
            if (currentIdx < 0) currentIdx = 0;

            string preview = formatItem != null ? formatItem(items[currentIdx]) : items[currentIdx].ToString();
            bool changed = false;

            if (BeginStyledCombo(id, preview, icon, accentColor, width: width))
            {
                for (int i = 0; i < items.Length; i++)
                {
                    bool isSelected = (i == currentIdx);
                    string label = formatItem != null ? formatItem(items[i]) : items[i].ToString();
                    if (StyledSelectable(label, isSelected, accentColor: accentColor))
                    {
                        selectedItem = items[i];
                        changed = true;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                EndStyledCombo();
            }

            return changed;
        }

        public static bool StyledCombo(
            string id,
            ref string selectedItem,
            IEnumerable<string> items,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            float width = -1,
            string emptyLabel = "—")
        {
            var itemList = items?.ToList() ?? new List<string>();
            string preview = string.IsNullOrEmpty(selectedItem) ? emptyLabel : selectedItem;
            bool changed = false;

            if (BeginStyledCombo(id, preview, icon, accentColor, width: width))
            {
                for (int i = 0; i < itemList.Count; i++)
                {
                    string item = itemList[i];
                    string label = string.IsNullOrEmpty(item) ? emptyLabel : item;
                    bool isSelected = string.Equals(selectedItem, item, StringComparison.OrdinalIgnoreCase);

                    if (StyledSelectable(label, isSelected, accentColor: accentColor))
                    {
                        selectedItem = item;
                        changed = true;
                    }
                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
                EndStyledCombo();
            }

            return changed;
        }

        public static bool BeginStyledCombo(
            string id,
            string previewValue,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            ImGuiComboFlags flags = ImGuiComboFlags.None,
            float width = -1)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var accent = accentColor ?? ImGuiColors.ParsedGold;

            if (width > 0)
            {
                ImGui.SetNextItemWidth(width * scale);
            }

            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.11f, 0.12f, 0.16f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.18f, 0.22f, 0.30f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.22f, 0.28f, 0.38f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(accent.X, accent.Y, accent.Z, 0.55f));
            ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.09f, 0.10f, 0.13f, 0.98f));
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(accent.X * 0.35f, accent.Y * 0.35f, accent.Z * 0.35f, 0.85f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(accent.X * 0.55f, accent.Y * 0.55f, accent.Z * 0.55f, 0.90f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(accent.X * 0.75f, accent.Y * 0.75f, accent.Z * 0.75f, 0.95f));

            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5.0f * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 6.0f * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1.2f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8.0f, 4.5f) * scale);
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6.0f, 4.0f) * scale);

            string displayPreview = icon.HasValue ? $"{icon.Value.ToIconString()}  {previewValue}" : previewValue;
            bool opened = ImGui.BeginCombo(id, displayPreview, flags);

            if (!opened)
            {
                ImGui.PopStyleVar(6);
                ImGui.PopStyleColor(9);
            }

            return opened;
        }

        public static void EndStyledCombo()
        {
            ImGui.EndCombo();
            ImGui.PopStyleVar(6);
            ImGui.PopStyleColor(9);
        }

        public static bool StyledSelectable(
            string label,
            bool isSelected,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            ImGuiSelectableFlags flags = ImGuiSelectableFlags.None,
            Vector2 size = default)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var accent = accentColor ?? ImGuiColors.ParsedGold;

            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 4.0f * scale))
            using (ImRaii.PushColor(ImGuiCol.Text, isSelected ? ImGuiColors.DalamudWhite : (Vector4)ImGuiColors.DalamudGrey))
            {
                string displayLabel = isSelected ? $"✓  {label}" : $"   {label}";
                if (icon.HasValue)
                {
                    displayLabel = isSelected ? $"✓ {icon.Value.ToIconString()} {label}" : $"  {icon.Value.ToIconString()} {label}";
                }

                return ImGui.Selectable(displayLabel, isSelected, flags, size);
            }
        }

        #endregion

        public static void Badge(string text, Vector4 bgCol, Vector4 textCol)
        {
            var padding = new Vector2(6.0f, 2.0f) * ImGuiHelpers.GlobalScale;
            var textSize = ImGui.CalcTextSize(text);
            var size = textSize + padding * 2.0f;
            var pos = ImGui.GetCursorScreenPos();

            var drawList = ImGui.GetWindowDrawList();
            var col = ImGui.ColorConvertFloat4ToU32(bgCol);
            drawList.AddRectFilled(pos, pos + size, col, 4.0f * ImGuiHelpers.GlobalScale);

            drawList.AddText(pos + padding, ImGui.ColorConvertFloat4ToU32(textCol), text);
            ImGui.Dummy(size);
        }

        public static void DrawStatModifierEditor(
            GearItem item,
            CharacterSheet? sheet,
            DiceSystem? system,
            StatModifierEditorState state,
            string idPrefix = "ModEditor")
        {
            string[] categories = new[]
            {
                LocalizationManager.Instance.GetLocalizedString("StatCategoryAttribute"),
                LocalizationManager.Instance.GetLocalizedString("StatCategorySkill"),
                LocalizationManager.Instance.GetLocalizedString("StatCategoryAbility"),
                LocalizationManager.Instance.GetLocalizedString("StatCategoryResource"),
                LocalizationManager.Instance.GetLocalizedString("StatCategoryCustom")
            };

            List<string> availableStats = new();
            if (state.SelectedCategoryIndex == 0) // Attribute
            {
                if (sheet?.characterAttributes != null && sheet.characterAttributes.Count > 0)
                {
                    availableStats = sheet.characterAttributes.Keys.ToList();
                }
            }
            else if (state.SelectedCategoryIndex == 1) // Skill
            {
                if (sheet?.characterSkills != null && sheet.characterSkills.Count > 0)
                {
                    availableStats = sheet.characterSkills.Keys.ToList();
                }
            }
            else if (state.SelectedCategoryIndex == 2) // Ability
            {
                if (sheet?.characterAbilities != null && sheet.characterAbilities.Count > 0)
                {
                    availableStats = sheet.characterAbilities.Keys.ToList();
                }
            }
            else if (state.SelectedCategoryIndex == 3) // Resource
            {
                var resList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (sheet?.characterResources != null)
                {
                    foreach (var r in sheet.characterResources.Keys) resList.Add(r);
                }
                if (system != null)
                {
                    foreach (var r in system.GetEffectiveResources()) resList.Add(r.Name);
                }
                if (resList.Count == 0)
                {
                    resList.Add("Health");
                    resList.Add("Mana");
                }
                availableStats = resList.ToList();
            }

            ImGui.TextColored(ImGuiColors.ParsedGreen, LocalizationManager.Instance.GetLocalizedString("StatModifiersLabel"));

            if (StyledCombo($"##{idPrefix}_CategoryCombo", ref state.SelectedCategoryIndex, categories, width: 120.0f))
            {
                state.SelectedStatIndex = 0;
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            string chosenStatName = string.Empty;

            if (state.SelectedCategoryIndex < 4 && availableStats.Count > 0)
            {
                if (state.SelectedStatIndex >= availableStats.Count) state.SelectedStatIndex = 0;
                var statsArr = availableStats.ToArray();
                StyledCombo($"##{idPrefix}_StatCombo", ref state.SelectedStatIndex, statsArr, width: 140.0f);
                chosenStatName = statsArr[state.SelectedStatIndex];
            }
            else
            {
                UiUtils.StyledInputText($"{idPrefix}_CustomStatName", ref state.CustomStatName, 50, width: 140.0f, hint: LocalizationManager.Instance.GetLocalizedString("StatNameHint"));
                chosenStatName = state.CustomStatName.Trim();
            }

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            UiUtils.StyledInputInt($"{idPrefix}_ModVal", ref state.ModifierValue, step: 0, width: 60.0f);

            ImGui.SameLine(0, 6.0f * ImGuiHelpers.GlobalScale);
            if (UiUtils.IconTextButton($"{idPrefix}_AddBtn", FontAwesomeIcon.Plus, LocalizationManager.Instance.GetLocalizedString("AddStatModifierButton")))
            {
                if (!string.IsNullOrWhiteSpace(chosenStatName))
                {
                    item.SetStatModifier(chosenStatName, state.ModifierValue);
                    state.CustomStatName = string.Empty;
                    state.ModifierValue = 1;
                }
            }

            if (item.StatModifiers != null && item.StatModifiers.Count > 0)
            {
                ImGui.Spacing();
                string? modToRemove = null;
                foreach (var mod in item.StatModifiers)
                {
                    var modCol = mod.Value >= 0 ? ImGuiColors.ParsedBlue : ImGuiColors.DalamudRed;
                    var modBg = mod.Value >= 0 ? new Vector4(0.12f, 0.22f, 0.38f, 0.85f) : new Vector4(0.35f, 0.12f, 0.12f, 0.85f);
                    string modText = $"{(mod.Value >= 0 ? "+" : "")}{mod.Value} {mod.Key}";
                    Badge(modText, modBg, modCol);
                    ImGui.SameLine(0, 4.0f * ImGuiHelpers.GlobalScale);
                    if (IconButton($"{idPrefix}_DelMod_{mod.Key}", FontAwesomeIcon.Trash, LocalizationManager.Instance.GetLocalizedString("RemoveTooltip"), new Vector2(20, 20) * ImGuiHelpers.GlobalScale))
                    {
                        modToRemove = mod.Key;
                    }
                    ImGui.SameLine(0, 8.0f * ImGuiHelpers.GlobalScale);
                }
                ImGui.NewLine();

                if (modToRemove != null)
                {
                    item.RemoveStatModifier(modToRemove);
                }
            }
        }
        public static Vector4 GetRarityColor(string rarity)
        {
            return rarity?.ToLowerInvariant() switch
            {
                "uncommon" => ImGuiColors.ParsedGreen,
                "rare" => ImGuiColors.ParsedBlue,
                "epic" => ImGuiColors.ParsedPurple,
                "legendary" => ImGuiColors.ParsedOrange,
                "artifact" => new Vector4(0.95f, 0.25f, 0.35f, 1.0f),
                _ => ImGuiColors.DalamudWhite
            };
        }

        public static void DrawRarityBadge(string rarity)
        {
            var col = GetRarityColor(rarity);
            var bg = new Vector4(col.X * 0.2f, col.Y * 0.2f, col.Z * 0.2f, 0.85f);
            PillBadge(rarity, bg, col);
        }

        public static void DrawOrnamentalDivider(string? centerText = null, FontAwesomeIcon? centerIcon = null, Vector4? accentColor = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var accent = accentColor ?? ImGuiColors.ParsedGold;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var height = 20.0f * scale;
            var centerY = pos.Y + height * 0.5f;
            var centerX = pos.X + availWidth * 0.5f;
            var drawList = ImGui.GetWindowDrawList();

            var lineColCenter = ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X, accent.Y, accent.Z, 0.75f));
            var lineColEdge = ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X, accent.Y, accent.Z, 0.05f));

            float emblemWidth = 0f;
            if (centerIcon.HasValue || !string.IsNullOrEmpty(centerText))
            {
                ImGui.PushFont(UiBuilder.IconFont);
                float iconW = centerIcon.HasValue ? ImGui.CalcTextSize(centerIcon.Value.ToIconString()).X + 6.0f * scale : 0f;
                ImGui.PopFont();
                float textW = !string.IsNullOrEmpty(centerText) ? ImGui.CalcTextSize(centerText).X : 0f;
                emblemWidth = iconW + textW + 16.0f * scale;
            }
            else
            {
                emblemWidth = 16.0f * scale;
            }

            float halfLine = Math.Max(10.0f * scale, (availWidth - emblemWidth) * 0.5f);

            // Left gradient line
            drawList.AddRectFilledMultiColor(
                new Vector2(pos.X, centerY - 0.75f * scale),
                new Vector2(centerX - emblemWidth * 0.5f, centerY + 0.75f * scale),
                lineColEdge, lineColCenter, lineColCenter, lineColEdge);

            // Right gradient line
            drawList.AddRectFilledMultiColor(
                new Vector2(centerX + emblemWidth * 0.5f, centerY - 0.75f * scale),
                new Vector2(pos.X + availWidth, centerY + 0.75f * scale),
                lineColCenter, lineColEdge, lineColEdge, lineColCenter);

            // Center emblem diamond / text
            if (centerIcon.HasValue || !string.IsNullOrEmpty(centerText))
            {
                var emblemBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.12f, 0.15f, 0.95f));
                var emblemBorder = ImGui.ColorConvertFloat4ToU32(accent);
                var emblemMin = new Vector2(centerX - emblemWidth * 0.5f, centerY - 10.0f * scale);
                var emblemMax = new Vector2(centerX + emblemWidth * 0.5f, centerY + 10.0f * scale);

                drawList.AddRectFilled(emblemMin, emblemMax, emblemBg, 4.0f * scale);
                drawList.AddRect(emblemMin, emblemMax, emblemBorder, 4.0f * scale, ImDrawFlags.None, 1.0f);

                float curX = centerX - emblemWidth * 0.5f + 8.0f * scale;
                if (centerIcon.HasValue)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    var iconStr = centerIcon.Value.ToIconString();
                    var iconH = ImGui.CalcTextSize(iconStr).Y;
                    drawList.AddText(new Vector2(curX, centerY - iconH * 0.5f), emblemBorder, iconStr);
                    curX += ImGui.CalcTextSize(iconStr).X + 6.0f * scale;
                    ImGui.PopFont();
                }

                if (!string.IsNullOrEmpty(centerText))
                {
                    var txtH = ImGui.CalcTextSize(centerText).Y;
                    drawList.AddText(new Vector2(curX, centerY - txtH * 0.5f), ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudWhite), centerText);
                }
            }
            else
            {
                // Small gold diamond in center
                var dSize = 4.0f * scale;
                var col = ImGui.ColorConvertFloat4ToU32(accent);
                drawList.AddQuadFilled(
                    new Vector2(centerX, centerY - dSize),
                    new Vector2(centerX + dSize, centerY),
                    new Vector2(centerX, centerY + dSize),
                    new Vector2(centerX - dSize, centerY),
                    col);
            }

            ImGui.Dummy(new Vector2(availWidth, height));
        }

        public static void DrawWindowHeroBanner(
            string title,
            string subtitle,
            string? badgeText = null,
            Vector4? badgeColor = null,
            FontAwesomeIcon icon = FontAwesomeIcon.ShieldAlt,
            Vector4? accentColor = null,
            Action? onAction = null,
            string actionLabel = "",
            float? progressFraction = null,
            string? progressLabel = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var accent = accentColor ?? ImGuiColors.ParsedGold;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var height = 72.0f * scale;
            var drawList = ImGui.GetWindowDrawList();

            // Background card with ornate dark gradient
            var bgCol = ImGui.ColorConvertFloat4ToU32(new Vector4(0.11f, 0.12f, 0.15f, 0.98f));
            var borderCol = ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X, accent.Y, accent.Z, 0.65f));
            var highlightCol = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.15f));

            drawList.AddRectFilled(pos, pos + new Vector2(availWidth, height), bgCol, 8.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(availWidth, height), borderCol, 8.0f * scale, ImDrawFlags.None, 1.5f);

            // Left gold banner accent bar
            drawList.AddRectFilled(
                pos + new Vector2(3.0f * scale, 6.0f * scale),
                pos + new Vector2(7.0f * scale, height - 6.0f * scale),
                ImGui.ColorConvertFloat4ToU32(accent),
                2.0f * scale);

            // Glass top highlight
            drawList.AddLine(
                pos + new Vector2(10.0f * scale, 2.0f * scale),
                pos + new Vector2(availWidth - 10.0f * scale, 2.0f * scale),
                highlightCol,
                1.0f * scale);

            // Emblem box on the left
            var emblemSize = 46.0f * scale;
            var emblemPos = pos + new Vector2(16.0f * scale, (height - emblemSize) * 0.5f);
            var emblemBg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.18f, 0.22f, 0.95f));
            var emblemBorder = ImGui.ColorConvertFloat4ToU32(accent);

            drawList.AddRectFilled(emblemPos, emblemPos + new Vector2(emblemSize, emblemSize), emblemBg, 8.0f * scale);
            drawList.AddRect(emblemPos, emblemPos + new Vector2(emblemSize, emblemSize), emblemBorder, 8.0f * scale, ImDrawFlags.None, 1.5f);

            ImGui.PushFont(UiBuilder.IconFont);
            var iconStr = icon.ToIconString();
            var iconSize = ImGui.CalcTextSize(iconStr);
            var iconCenter = emblemPos + new Vector2((emblemSize - iconSize.X) * 0.5f, (emblemSize - iconSize.Y) * 0.5f);
            drawList.AddText(iconCenter, emblemBorder, iconStr);
            ImGui.PopFont();

            // Inner header details
            float contentLeft = 16.0f * scale + emblemSize + 12.0f * scale;
            ImGui.SetCursorScreenPos(pos + new Vector2(contentLeft, 8.0f * scale));

            ImGui.BeginGroup();
            {
                ImGui.TextColored(accent, title);
                if (!string.IsNullOrEmpty(badgeText))
                {
                    ImGui.SameLine(0, 8.0f * scale);
                    var bColor = badgeColor ?? accent;
                    PillBadge(badgeText, new Vector4(bColor.X * 0.25f, bColor.Y * 0.25f, bColor.Z * 0.25f, 0.9f), bColor, FontAwesomeIcon.Medal);
                }

                if (!string.IsNullOrEmpty(subtitle))
                {
                    ImGui.TextColored(ImGuiColors.DalamudGrey, subtitle);
                }
            }
            ImGui.EndGroup();

            // Right side action & progress label
            float rightElementsWidth = 0f;
            float actBtnW = !string.IsNullOrEmpty(actionLabel) ? ImGui.CalcTextSize(actionLabel).X + 24.0f * scale : 0f;
            float progLabelW = !string.IsNullOrEmpty(progressLabel) ? ImGui.CalcTextSize(progressLabel).X + 20.0f * scale : 0f;

            rightElementsWidth = actBtnW + (progLabelW > 0 ? progLabelW + 8.0f * scale : 0f) + 16.0f * scale;

            if (availWidth - contentLeft > rightElementsWidth + 40.0f * scale && (actBtnW > 0 || progLabelW > 0))
            {
                float rightStart = pos.X + availWidth - rightElementsWidth;
                ImGui.SetCursorScreenPos(new Vector2(rightStart, pos.Y + 14.0f * scale));

                ImGui.BeginGroup();
                {
                    if (!string.IsNullOrEmpty(progressLabel))
                    {
                        PillBadge(progressLabel, new Vector4(0.12f, 0.25f, 0.38f, 0.9f), ImGuiColors.ParsedBlue, FontAwesomeIcon.CheckCircle);
                    }

                    if (!string.IsNullOrEmpty(actionLabel) && onAction != null)
                    {
                        if (!string.IsNullOrEmpty(progressLabel)) ImGui.SameLine(0, 8.0f * scale);
                        if (ImGui.Button($"{actionLabel}###HeroBannerActionBtn"))
                        {
                            onAction.Invoke();
                        }
                    }
                }
                ImGui.EndGroup();
            }

            // Bottom progress bar spanning the banner width
            if (progressFraction.HasValue)
            {
                float fraction = Math.Clamp(progressFraction.Value, 0f, 1f);
                float barHeight = 4.0f * scale;
                var barPos = pos + new Vector2(contentLeft, height - 10.0f * scale);
                float barWidth = Math.Max(20.0f * scale, availWidth - contentLeft - 16.0f * scale);

                drawList.AddRectFilled(barPos, barPos + new Vector2(barWidth, barHeight), ImGui.ColorConvertFloat4ToU32(new Vector4(0.18f, 0.18f, 0.22f, 0.9f)), 2.0f * scale);
                if (fraction > 0)
                {
                    drawList.AddRectFilled(barPos, barPos + new Vector2(barWidth * fraction, barHeight), ImGui.ColorConvertFloat4ToU32(accent), 2.0f * scale);
                }
            }

            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(new Vector2(availWidth, height));
        }

        public static void DrawStyledCard(
            string title,
            string? subtitle = null,
            string? description = null,
            FontAwesomeIcon? icon = null,
            Vector4? accentColor = null,
            string? badgeText = null,
            Vector4? badgeColor = null,
            string? secondaryBadge = null,
            Vector4? secondaryBadgeColor = null,
            float? progress = null,
            string? progressOverlay = null,
            Action? onAction1 = null,
            string action1Label = "",
            FontAwesomeIcon? action1Icon = null,
            Action? onAction2 = null,
            string action2Label = "",
            FontAwesomeIcon? action2Icon = null,
            float height = 82.0f,
            bool isHighlighted = false,
            Action? onClick = null)
        {
            var scale = ImGuiHelpers.GlobalScale;
            var pos = ImGui.GetCursorScreenPos();
            var availWidth = ImGui.GetContentRegionAvail().X;
            var h = height * scale;
            var drawList = ImGui.GetWindowDrawList();
            var accent = accentColor ?? ImGuiColors.ParsedGold;

            var isHovered = ImGui.IsMouseHoveringRect(pos, pos + new Vector2(availWidth, h));
            if (isHovered && onClick != null && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                onClick.Invoke();
            }

            // Card background & borders
            var bgCol = isHovered
                ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.16f, 0.17f, 0.21f, 0.96f))
                : (isHighlighted 
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.13f, 0.14f, 0.17f, 0.95f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(0.09f, 0.10f, 0.12f, 0.90f)));

            var borderCol = isHighlighted
                ? (isHovered ? ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X, accent.Y, accent.Z, 1f)) : ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X, accent.Y, accent.Z, 0.85f)))
                : (isHovered ? ImGui.ColorConvertFloat4ToU32(new Vector4(accent.X, accent.Y, accent.Z, 0.75f)) : ImGui.ColorConvertFloat4ToU32(new Vector4(0.24f, 0.25f, 0.29f, 0.65f)));

            drawList.AddRectFilled(pos, pos + new Vector2(availWidth, h), bgCol, 6.0f * scale);
            drawList.AddRect(pos, pos + new Vector2(availWidth, h), borderCol, 6.0f * scale, ImDrawFlags.None, isHighlighted || isHovered ? 1.5f : 1.0f);

            // Left colored category accent stripe
            drawList.AddRectFilled(
                pos + new Vector2(2.0f * scale, 6.0f * scale),
                pos + new Vector2(5.5f * scale, h - 6.0f * scale),
                ImGui.ColorConvertFloat4ToU32(accent),
                2.0f * scale);

            float textStartX = 12.0f * scale;

            // Framed icon box on the left
            if (icon.HasValue)
            {
                var iconBoxSize = 44.0f * scale;
                var iconBoxPos = pos + new Vector2(12.0f * scale, (h - iconBoxSize) * 0.5f);
                var iconBg = isHighlighted
                    ? ImGui.ColorConvertFloat4ToU32(new Vector4(0.20f, 0.20f, 0.24f, 0.95f))
                    : ImGui.ColorConvertFloat4ToU32(new Vector4(0.14f, 0.15f, 0.18f, 0.95f));
                var iconBorder = ImGui.ColorConvertFloat4ToU32(accent);

                drawList.AddRectFilled(iconBoxPos, iconBoxPos + new Vector2(iconBoxSize, iconBoxSize), iconBg, 6.0f * scale);
                drawList.AddRect(iconBoxPos, iconBoxPos + new Vector2(iconBoxSize, iconBoxSize), iconBorder, 6.0f * scale, ImDrawFlags.None, 1.0f);

                ImGui.PushFont(UiBuilder.IconFont);
                var iconStr = icon.Value.ToIconString();
                var iconTextSize = ImGui.CalcTextSize(iconStr);
                var iconCenter = iconBoxPos + new Vector2((iconBoxSize - iconTextSize.X) * 0.5f, (iconBoxSize - iconTextSize.Y) * 0.5f);
                drawList.AddText(iconCenter, iconBorder, iconStr);
                ImGui.PopFont();

                textStartX += iconBoxSize + 10.0f * scale;
            }

            // Top Header: Title & Badges
            ImGui.SetCursorScreenPos(pos + new Vector2(textStartX, 6.0f * scale));
            ImGui.BeginGroup();
            {
                var titleCol = isHighlighted ? accent : ImGuiColors.DalamudWhite;
                ImGui.TextColored(titleCol, title);

                if (!string.IsNullOrEmpty(badgeText))
                {
                    ImGui.SameLine(0, 8.0f * scale);
                    var bColor = badgeColor ?? accent;
                    PillBadge(badgeText, new Vector4(bColor.X * 0.25f, bColor.Y * 0.25f, bColor.Z * 0.25f, 0.85f), bColor);
                }

                if (!string.IsNullOrEmpty(secondaryBadge))
                {
                    ImGui.SameLine(0, 4.0f * scale);
                    var sbColor = secondaryBadgeColor ?? ImGuiColors.ParsedGold;
                    PillBadge(secondaryBadge, new Vector4(sbColor.X * 0.25f, sbColor.Y * 0.25f, sbColor.Z * 0.25f, 0.85f), sbColor);
                }
            }
            ImGui.EndGroup();

            // Middle: Subtitle / Description
            if (!string.IsNullOrEmpty(subtitle) || !string.IsNullOrEmpty(description))
            {
                ImGui.SetCursorScreenPos(pos + new Vector2(textStartX, 28.0f * scale));
                ImGui.BeginGroup();
                {
                    if (!string.IsNullOrEmpty(subtitle))
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, subtitle);
                    }
                    else if (!string.IsNullOrEmpty(description))
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
                        ImGui.PushTextWrapPos(pos.X + availWidth - 100.0f * scale);
                        ImGui.TextUnformatted(description);
                        ImGui.PopTextWrapPos();
                        ImGui.PopStyleColor();
                    }
                }
                ImGui.EndGroup();
            }

            // Bottom: Progress Bar
            if (progress.HasValue)
            {
                ImGui.SetCursorScreenPos(pos + new Vector2(textStartX, h - 24.0f * scale));
                ImGui.BeginGroup();
                {
                    float progWidth = Math.Min(200.0f * scale, availWidth * 0.45f);
                    DrawProgressBar(progress.Value, 1.0f, progressOverlay ?? $"{progress.Value * 100:0}%", new Vector2(progWidth, 14.0f * scale), accent, null, null, 3.0f);
                }
                ImGui.EndGroup();
            }

            // Action Buttons on Right
            float actionsX = pos.X + availWidth - 70.0f * scale;
            if (onAction1 != null || onAction2 != null)
            {
                ImGui.SetCursorScreenPos(new Vector2(actionsX, pos.Y + (h - 26.0f * scale) * 0.5f));
                ImGui.BeginGroup();
                {
                    if (onAction1 != null)
                    {
                        if (action1Icon.HasValue)
                        {
                            if (IconButton($"CardAct1_{title.GetHashCode()}", action1Icon.Value, action1Label, new Vector2(24, 24) * scale))
                            {
                                onAction1.Invoke();
                            }
                        }
                        else if (!string.IsNullOrEmpty(action1Label))
                        {
                            if (ImGui.Button($"{action1Label}###CardAct1_{title.GetHashCode()}"))
                            {
                                onAction1.Invoke();
                            }
                        }
                    }

                    if (onAction2 != null)
                    {
                        ImGui.SameLine(0, 4.0f * scale);
                        if (action2Icon.HasValue)
                        {
                            if (IconButton($"CardAct2_{title.GetHashCode()}", action2Icon.Value, action2Label, new Vector2(24, 24) * scale))
                            {
                                onAction2.Invoke();
                            }
                        }
                        else if (!string.IsNullOrEmpty(action2Label))
                        {
                            if (ImGui.Button($"{action2Label}###CardAct2_{title.GetHashCode()}"))
                            {
                                onAction2.Invoke();
                            }
                        }
                    }
                }
                ImGui.EndGroup();
            }

            ImGui.SetCursorScreenPos(pos);
            ImGui.Dummy(new Vector2(availWidth, h));
            ImGui.Spacing();
        }
    }

    public class StatModifierEditorState
    {
        public int SelectedCategoryIndex = 0;
        public int SelectedStatIndex = 0;
        public string CustomStatName = string.Empty;
        public int ModifierValue = 1;
    }
}
