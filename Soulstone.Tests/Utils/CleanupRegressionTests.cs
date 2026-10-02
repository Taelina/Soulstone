using FluentAssertions;
using Soulstone.Datamodels;
using Soulstone.Managers;
using Soulstone.Sync;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Soulstone.Tests.Utils;

[Collection("NonParallel")]
public class CleanupRegressionTests
{
    public CleanupRegressionTests() => TestHelper.EnsureMockServices();

    [Fact]
    public void PublicProfile_RemovesHiddenFieldsAndPrivateGameplayData_WithoutMutatingTheSheet()
    {
        var sheet = new CharacterSheet
        {
            CharacterFullName = "Public Name", CharacterBackground = "secret background", PlayerNotes = "secret notes",
            CharacterClass = "private class", CharacterPictureUrl = @"C:\private\portrait.png"
        };
        sheet.SetFieldHidden("CharacterBackground", true);
        sheet.SetFieldHidden("PlayerNotes", true);
        sheet.CharacterResources["Secret"] = new CharacterResource { Name = "Secret", CurrentValue = 7 };
        sheet.SetFieldHidden("Resource_Secret", true);
        var json = PublicCharacterProfile.FromSheet(sheet).ToJson();
        json.Should().Contain("Public Name").And.NotContain("secret background").And.NotContain("secret notes")
            .And.NotContain("private class").And.NotContain("private\\\\portrait");
        using var document = JsonDocument.Parse(json);
        document.RootElement.TryGetProperty("CharacterAttributes", out _).Should().BeFalse();
        document.RootElement.GetProperty("CharacterResources").TryGetProperty("Secret", out _).Should().BeFalse();
        sheet.CharacterBackground.Should().Be("secret background");
        sheet.CharacterResources.Should().ContainKey("Secret");
        JsonSerializer.Deserialize<CharacterSheet>(json)!.CharacterFullName.Should().Be("Public Name");
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData(@"..\outside")]
    [InlineData(@"C:\outside")]
    [InlineData("name/child")]
    public void StorageNames_CannotEscapeTheStorageDirectory(string name)
    {
        Action save = () => StoragePath.ForJson(Path.GetTempPath(), name);
        save.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void InitiativeSerialization_ExcludesLocalIdentityAndFilePaths()
    {
        var participant = new InitiativeParticipant("Someone", 10) { IsCurrentCharacter = true, SheetFilePath = @"C:\private\sheet.json" };
        var json = JsonSerializer.Serialize(participant);
        json.Should().NotContain("IsCurrentCharacter").And.NotContain("SheetFilePath");
        var legacy = JsonSerializer.Deserialize<InitiativeParticipant>("{\"Name\":\"Someone\",\"IsCurrentCharacter\":true,\"SheetFilePath\":\"secret\"}")!;
        legacy.IsCurrentCharacter.Should().BeFalse();
        legacy.SheetFilePath.Should().BeNull();
    }

    [Fact]
    public void UnsignedInitiativeSnapshot_IsRejected_ButSignedHostSnapshotIsAccepted()
    {
        var keys = RelayCrypto.CreateHostKeyPair();
        var configuration = new Soulstone.Configuration { SyncRoomKey = RelayCrypto.CreateRoomKey(), SyncHostPublicKey = keys.PublicKey };
        WithConfiguration(configuration, manager =>
        {
            int received = 0;
            void Handler(InitiativeSyncPayload _) => received++;
            manager.OnInitiativeSyncReceived += Handler;
            try
            {
                var packet = new PartySyncPacket { SenderName = "Host", EventType = SyncEventType.InitiativeSync, PayloadJson = JsonSerializer.Serialize(new InitiativeSyncPayload()) };
                var envelope = RelayCrypto.EncryptGroupMessage(packet, configuration.SyncRoomKey);
                manager.ProcessRelayMessage(JsonSerializer.Serialize(envelope));
                received.Should().Be(0);
                RelayCrypto.SignEnvelope(envelope, keys.PrivateKey);
                manager.ProcessRelayMessage(JsonSerializer.Serialize(envelope));
                received.Should().Be(1);
            }
            finally { manager.OnInitiativeSyncReceived -= Handler; }
        });
    }

    [Fact]
    public void HostAddressedRollRequest_IsNotQueuedForAMember()
    {
        WithConfiguration(new Soulstone.Configuration { SyncHostName = "Different Host" }, manager =>
        {
            var request = new RollRequestPayload { TargetName = "Different Host", Formula = "1d20" };
            manager.HandleIncomingPacket(new PartySyncPacket { EventType = SyncEventType.RollRequest, PayloadJson = JsonSerializer.Serialize(request) }, "Different Host");
            manager.PendingRollRequests.Should().NotContainKey(request.RequestId);
        });
    }

    [Fact]
    public async Task PublishingANonActiveSheet_DoesNotSendAnUpload()
    {
        (await PartySyncManager.Instance.PublishCharacterSheetAsync(new CharacterSheet())).Should().BeFalse();
    }

    private static void WithConfiguration(Soulstone.Configuration configuration, Action<PartySyncManager> action)
    {
        var manager = PartySyncManager.Instance;
        var field = typeof(PartySyncManager).GetField("configuration", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = field.GetValue(manager);
        field.SetValue(manager, configuration);
        try { action(manager); }
        finally { field.SetValue(manager, original); }
    }
}
