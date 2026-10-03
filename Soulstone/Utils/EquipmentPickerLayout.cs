using System;

namespace Soulstone.Utils;

internal static class EquipmentPickerLayout
{
    internal static float GetFooterHeight(float textHeight, float framePaddingY, float itemSpacingY, float scale)
    {
        // IconTextButton uses at least four scaled pixels of vertical padding.
        // Leave room for both the child-to-footer spacing and ImGui.Spacing().
        return textHeight + 2.0f * Math.Max(framePaddingY, 4.0f * scale) + 2.0f * itemSpacingY;
    }
}
