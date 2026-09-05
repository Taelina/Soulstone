using Soulstone.Datamodels;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Soulstone.Sync
{
    public static class RelayCrypto
    {
        private const string InvitePrefix = "SS1-";

        public static string CreateRoomKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        public static (string PublicKey, string PrivateKey) CreateHostKeyPair()
        {
            using RSA rsa = RSA.Create(2048);
            return (
                Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()),
                Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()));
        }

        public static string EncodeInvite(RelayInvite invite)
        {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(invite);
            return InvitePrefix + Base64UrlEncode(json);
        }

        public static bool TryDecodeInvite(string? code, out RelayInvite? invite)
        {
            invite = null;
            if (string.IsNullOrWhiteSpace(code) || !code.StartsWith(InvitePrefix, StringComparison.Ordinal)) return false;

            try
            {
                invite = JsonSerializer.Deserialize<RelayInvite>(Base64UrlDecode(code[InvitePrefix.Length..]));
                return invite is { Version: 1 } && Uri.TryCreate(invite.ServerUrl, UriKind.Absolute, out _) &&
                    !string.IsNullOrWhiteSpace(invite.SessionId) && !string.IsNullOrWhiteSpace(invite.MemberToken) &&
                    Convert.FromBase64String(invite.RoomKey).Length == 32 && !string.IsNullOrWhiteSpace(invite.HostPublicKey) &&
                    !string.IsNullOrWhiteSpace(invite.HostName);
            }
            catch
            {
                invite = null;
                return false;
            }
        }

        public static RelayEnvelope EncryptGroupMessage(PartySyncPacket message, string roomKey)
        {
            return Encrypt(message, Convert.FromBase64String(roomKey), null);
        }

        public static RelayEnvelope EncryptPrivateMessage(PartySyncPacket message, string hostPublicKey)
        {
            byte[] messageKey = RandomNumberGenerator.GetBytes(32);
            using RSA hostKey = RSA.Create();
            hostKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(hostPublicKey), out _);
            string encryptedKey = Convert.ToBase64String(hostKey.Encrypt(messageKey, RSAEncryptionPadding.OaepSHA256));
            return Encrypt(message, messageKey, encryptedKey);
        }

        public static bool TryDecryptMessage(RelayEnvelope envelope, string roomKey, string? hostPrivateKey, out PartySyncPacket? message)
        {
            message = null;
            try
            {
                byte[] key;
                if (!string.IsNullOrEmpty(envelope.EncryptedKey))
                {
                    if (string.IsNullOrWhiteSpace(hostPrivateKey)) return false;
                    using RSA hostKey = RSA.Create();
                    hostKey.ImportPkcs8PrivateKey(Convert.FromBase64String(hostPrivateKey), out _);
                    key = hostKey.Decrypt(Convert.FromBase64String(envelope.EncryptedKey), RSAEncryptionPadding.OaepSHA256);
                }
                else
                {
                    key = Convert.FromBase64String(roomKey);
                }

                byte[] ciphertext = Convert.FromBase64String(envelope.Ciphertext);
                byte[] plaintext = new byte[ciphertext.Length];
                using var aes = new AesGcm(key, 16);
                aes.Decrypt(Convert.FromBase64String(envelope.Nonce), ciphertext, Convert.FromBase64String(envelope.Tag), plaintext, GetAssociatedData(envelope));
                message = JsonSerializer.Deserialize<PartySyncPacket>(plaintext);
                return message is { ProtocolVersion: 1 } && message.EventType == envelope.EventType;
            }
            catch
            {
                message = null;
                return false;
            }
        }

        public static void SignEnvelope(RelayEnvelope envelope, string hostPrivateKey)
        {
            using RSA hostKey = RSA.Create();
            hostKey.ImportPkcs8PrivateKey(Convert.FromBase64String(hostPrivateKey), out _);
            envelope.Signature = Convert.ToBase64String(hostKey.SignData(GetSignatureData(envelope), HashAlgorithmName.SHA256, RSASignaturePadding.Pss));
        }

        public static bool VerifyHostSignature(RelayEnvelope envelope, string hostPublicKey)
        {
            if (string.IsNullOrWhiteSpace(envelope.Signature)) return false;
            try
            {
                using RSA hostKey = RSA.Create();
                hostKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(hostPublicKey), out _);
                return hostKey.VerifyData(GetSignatureData(envelope), Convert.FromBase64String(envelope.Signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
            }
            catch
            {
                return false;
            }
        }

        private static RelayEnvelope Encrypt(PartySyncPacket message, byte[] key, string? encryptedKey)
        {
            var envelope = new RelayEnvelope
            {
                Destination = string.IsNullOrEmpty(encryptedKey) ? "group" : "host",
                SenderName = message.SenderName,
                SenderWorld = message.SenderWorld,
                EventType = message.EventType,
                EncryptedKey = encryptedKey ?? string.Empty
            };
            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(message);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];
            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, GetAssociatedData(envelope));
            envelope.Nonce = Convert.ToBase64String(nonce);
            envelope.Ciphertext = Convert.ToBase64String(ciphertext);
            envelope.Tag = Convert.ToBase64String(tag);
            return envelope;
        }

        private static byte[] GetAssociatedData(RelayEnvelope envelope) => Encoding.UTF8.GetBytes(
            $"{envelope.Version}\n{envelope.Destination}\n{envelope.SenderName}\n{envelope.SenderWorld}\n{(int)envelope.EventType}\n{envelope.EncryptedKey}");

        private static byte[] GetSignatureData(RelayEnvelope envelope) => Encoding.UTF8.GetBytes(
            $"{Convert.ToBase64String(GetAssociatedData(envelope))}\n{envelope.Nonce}\n{envelope.Ciphertext}\n{envelope.Tag}");

        private static string Base64UrlEncode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] Base64UrlDecode(string value)
        {
            string padded = value.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            return Convert.FromBase64String(padded);
        }
    }
}