using Dalamud.Plugin.Services;
using Moq;

namespace Soulstone.Tests
{
    public static class TestHelper
    {
        public static void EnsureMockServices()
        {
            if (Plugin.Log == null)
            {
                Plugin.Log = new Mock<IPluginLog>().Object;
            }
            Soulstone.Managers.LocalizationManager.Instance.InitLoc(new Soulstone.Configuration { Language = Soulstone.Localizations.Language.English });
        }
    }
}
