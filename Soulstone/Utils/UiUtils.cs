using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
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

        public static void ManageInputField(ref string field, string fieldname, bool editing)
        {
            if (editing)
            {
                ImGui.SetNextItemWidth(DefaultInputWidth);
                ImGui.InputText($"##{fieldname}", ref field, 100);
            }
            else
            {
                ImGui.Text(field);
            }
        }

        public static void ManageInputField(ref int field, string fieldname, bool editing)
        {
            if (editing)
            {
                ImGui.SetNextItemWidth(DefaultInputWidth);
                ImGui.InputInt($"##{fieldname}", ref field, 1);
            }
            else
            {
                ImGui.Text(field.ToString());
            }
        }

        public static void ManageBigInputField(ref string field, string fieldname, bool editing)
        {
            if (editing)
            {
                ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputTextMultiline($"##{fieldname}", ref field, 5000, new Vector2(0.0f, 100.0f));
            }
            else
            {
                ImGui.TextWrapped(field);
            }
        }
    }
}
