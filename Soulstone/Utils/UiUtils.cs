using Dalamud.Bindings.ImGui;
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

        public static float defaultNextToSpace = 3.0f;
        public static float defaultFieldSpacing = 10.0f;
        public static float defaultInputWidth = 175.0f;

        public static void ManageInputField(ref string field, string fieldname, bool editing)
        {
            if (editing)
            {
                ImGui.SetNextItemWidth(defaultInputWidth);
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
                ImGui.SetNextItemWidth(defaultInputWidth);
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
