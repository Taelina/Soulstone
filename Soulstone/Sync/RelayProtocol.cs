using Soulstone.Datamodels;
using System;
using System.Text.Json.Serialization;

namespace Soulstone.Sync
{
    public sealed class RelaySessionResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public string HostToken { get; set; } = string.Empty;
        public string MemberToken { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }

    public sealed class RelayInvite
    {
        public int Version { get; set; } = 1;
        public string ServerUrl { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string MemberToken { get; set; } = string.Empty;
        public string RoomKey { get; set; } = string.Empty;
        public string HostPublicKey { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string HostWorld { get; set; } = string.Empty;
    }

    public sealed class RelayEnvelope
    {
        public int Version { get; set; } = 1;
        [JsonPropertyName("destination")]
        public string Destination { get; set; } = "group";
        public string SenderName { get; set; } = string.Empty;
        public string SenderWorld { get; set; } = string.Empty;
        public SyncEventType EventType { get; set; }
        public string Nonce { get; set; } = string.Empty;
        public string Ciphertext { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string EncryptedKey { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;
    }
}