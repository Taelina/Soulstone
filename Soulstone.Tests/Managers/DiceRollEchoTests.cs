using Dalamud.Game.Text;
using Dalamud.Plugin.Services;
using FluentAssertions;
using Moq;
using Soulstone.Datamodels;
using Soulstone.Localizations;
using Soulstone.Managers;
using System.Text.Json;
using Xunit;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallelCollection")]
    public class DiceRollEchoTests : IDisposable
    {
        private readonly IChatGui previousChatGui = Plugin.ChatGui;
        private readonly List<string> echoes = new();

        public DiceRollEchoTests()
        {
            TestHelper.EnsureMockServices();
            var chatGui = new Mock<IChatGui>();
            chatGui.Setup(chat => chat.Print(It.IsAny<XivChatEntry>()))
                .Callback<XivChatEntry>(entry => echoes.Add(entry.Message.TextValue));
            Plugin.ChatGui = chatGui.Object;
            PartySyncManager.Instance.Init(new Soulstone.Configuration());
        }

        public void Dispose()
        {
            Plugin.ChatGui = previousChatGui;
            LocalizationManager.Instance.InitLoc(new Soulstone.Configuration { Language = Language.English });
        }

        [Theory]
        [InlineData("[Soulstone] Stealth: 23", "ignored", "Stealth: 23")]
        [InlineData("", "[Soulstone] Detailed roll: 16 + 7 = 23", "Detailed roll: 16 + 7 = 23")]
        [InlineData("", "", "Stealth: 23 (16, 7)")]
        public void ReceivedRoll_AttributesPacketPayloadAndFallbackEchoes(string packetEcho, string payloadEcho, string expectedResult)
        {
            var roll = new DiceRollPayload
            {
                CharacterName = "Thancred Waters",
                RollName = "Stealth",
                Total = 23,
                Details = "16, 7",
                EchoMessage = payloadEcho
            };

            Receive(roll, packetEcho);

            echoes.Should().ContainSingle().Which.Should()
                .Be($"[Soulstone] [Thancred Waters] {expectedResult}");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LocalRoll_AttributesCustomEchoEvenWithoutSession(bool isPrivate)
        {
            string roller = PartySyncManager.Instance.GetLocalPlayerName();
            PartySyncManager.Instance.BroadcastDiceRoll("Stealth", 23, "16, 7",
                echoText: "[Soulstone] Stealth: 23", isPrivate: isPrivate);

            string prefix = isPrivate ? "[Private] " : "";
            echoes.Should().ContainSingle().Which.Should()
                .Be($"{prefix}[Soulstone] [{roller}] Stealth: 23");
        }

        [Fact]
        public void ReceivedRollForAnotherCharacter_ShowsRollerAndCharacter()
        {
            var roll = new DiceRollPayload
            {
                CharacterName = "Training Dummy",
                RolledBy = "Thancred Waters",
                EchoMessage = "[Soulstone] Attack: 23"
            };

            Receive(roll);

            echoes.Should().ContainSingle().Which.Should()
                .Be("[Soulstone] [Thancred Waters → Training Dummy] Attack: 23");
        }

        [Theory]
        [InlineData("[Private] [Soulstone] Stealth: 23")]
        [InlineData("[Privé] [Soulstone] [Thancred Waters] Stealth: 23")]
        [InlineData("[Private] [Soulstone] [Thancred Waters] Stealth: 23")]
        public void ReceivedPrivateRoll_NormalizesTagsAndDoesNotDuplicateAttribution(string echo)
        {
            var roll = new DiceRollPayload
            {
                CharacterName = "Thancred Waters",
                IsPrivate = true,
                TargetCharacterName = PartySyncManager.Instance.GetLocalPlayerName()
            };

            Receive(roll, echo);

            echoes.Should().ContainSingle().Which.Should()
                .Be("[Private] [Soulstone] [Thancred Waters] Stealth: 23");
        }

        [Theory]
        [InlineData("en", "Private")]
        [InlineData("fr", "Privé")]
        public void RollEcho_FormatsAttributionInBothLanguages(string language, string privateTag)
        {
            LocalizationManager.Instance.InitLoc(new Soulstone.Configuration { Language = LanguageExtensions.FromCode(language) });
            var roll = new DiceRollPayload { CharacterName = "Thancred Waters", IsPrivate = true };

            PartySyncManager.FormatDiceRollEcho(roll, "Stealth: 23").Should()
                .Be($"[{privateTag}] [Soulstone] [Thancred Waters] Stealth: 23");
        }

        [Fact]
        public void ReceivedPrivateRoll_ForOtherPlayers_DoesNotEcho()
        {
            Receive(new DiceRollPayload { CharacterName = "Thancred Waters", IsPrivate = true }, "Stealth: 23");

            echoes.Should().BeEmpty();
        }

        private static void Receive(DiceRollPayload roll, string echo = "")
        {
            PartySyncManager.Instance.HandleIncomingPacket(new PartySyncPacket
            {
                EventType = SyncEventType.DiceRoll,
                SenderName = "Thancred Waters",
                PayloadJson = JsonSerializer.Serialize(roll),
                EchoMessage = echo
            });
        }
    }
}
