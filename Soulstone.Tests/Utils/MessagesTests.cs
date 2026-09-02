using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using FluentAssertions;
using Soulstone.Utils;
using Xunit;

namespace Soulstone.Tests.Utils
{
    public class MessagesTests
    {
        public MessagesTests()
        {
            TestHelper.EnsureMockServices();
        }

        [Fact]
        public void PrintEcho_WithString_DoesNotThrow()
        {
            var act = () => Messages.PrintEcho("Testing echo print");
            act.Should().NotThrow();
        }

        [Fact]
        public void PrintEcho_WithSeString_DoesNotThrow()
        {
            var seString = new SeStringBuilder().AddText("Testing SeString echo").Build();
            var act = () => Messages.PrintEcho(seString);
            act.Should().NotThrow();
        }

        [Fact]
        public void PrintEcho_WithChatEntry_DoesNotThrow()
        {
            var chatEntry = new XivChatEntry
            {
                Message = "Testing ChatEntry echo",
                Type = XivChatType.Echo
            };
            var act = () => Messages.PrintEcho(chatEntry);
            act.Should().NotThrow();
        }

        [Fact]
        public void SendMessage_WithEchoType_RoutesToPrintEchoWithoutThrowing()
        {
            var chatEntry = new XivChatEntry
            {
                Message = "Testing SendMessage echo routing",
                Type = XivChatType.Echo
            };
            var act = () => Messages.SendMessage(chatEntry);
            act.Should().NotThrow();
        }
    }
}
