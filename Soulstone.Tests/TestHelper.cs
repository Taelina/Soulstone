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
        }
    }
}
