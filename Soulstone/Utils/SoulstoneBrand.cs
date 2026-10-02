using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Soulstone.Utils;

internal static class SoulstoneBrand
{
    internal const string IconResourceName = "Soulstone.Assets.soulstone.png";

    internal static void DrawIcon(float size)
    {
        var texture = Plugin.TextureProvider?.GetFromManifestResource(typeof(Plugin).Assembly, IconResourceName).GetWrapOrDefault();
        if (texture != null)
            ImGui.Image(texture.Handle, new Vector2(size));
        else
            ImGui.Dummy(new Vector2(size));
    }
}
