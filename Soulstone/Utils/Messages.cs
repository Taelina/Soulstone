using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Text;

namespace Soulstone.Utils
{
    // From: https://git.anna.lgbt/anna/XivCommon/src/branch/main/XivCommon/Functions/Chat.cs
    internal class Messages
    {
        public static void PrintEcho(XivChatEntry formatedMessage)
        {
            try
            {
                if (Plugin.ChatGui != null)
                {
                    Plugin.ChatGui.Print(formatedMessage);
                }
                else
                {
                    Plugin.Log?.Information(formatedMessage.Message.ToString());
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warning(ex, $"[Echo] Failed to print echo for message: {formatedMessage.Message}");
            }
        }

        public static void PrintEcho(SeString message)
        {
            PrintEcho(new XivChatEntry
            {
                Message = message,
                Type = XivChatType.Echo
            });
        }

        public static void PrintEcho(string message)
        {
            PrintEcho(new XivChatEntry
            {
                Message = message,
                Type = XivChatType.Echo
            });
        }

        public static unsafe void sendMessageUnsafe(byte[] message, XivChatType? type)
        {
            try
            {
                if (type == XivChatType.Echo)
                {
                    PrintEcho(Encoding.UTF8.GetString(message));
                    return;
                }

                var ui = UIModule.Instance();
                if (ui != null)
                {
                    Utf8String* mes = Utf8String.FromSequence(message);
                    ui->ProcessChatBoxEntry(mes, (byte)(type ?? XivChatType.None));
                    mes->Dtor(true);
                }
                else if (Plugin.ChatGui != null)
                {
                    Plugin.ChatGui.Print(new XivChatEntry
                    {
                        Message = Encoding.UTF8.GetString(message),
                        Type = type ?? XivChatType.Echo
                    });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Debug(ex, "sendMessageUnsafe failed (expected in unit test / headless environment)");
            }
        }

        public static unsafe void SendMessage(XivChatEntry formatedMessage)
        {
            try
            {
                if (formatedMessage.Type == XivChatType.Echo)
                {
                    PrintEcho(formatedMessage);
                    return;
                }

                string message = formatedMessage.Message.ToString();
                byte[] bytes = Encoding.UTF8.GetBytes(message);
                if (bytes.Length == 0)
                {
                    Plugin.Log?.Warning("Cannot send message: message is empty");
                    return;
                }

                if (bytes.Length > 500)
                {
                    Plugin.Log?.Warning("Cannot send message: message is longer than 500 bytes");
                    return;
                }

                if (message.Length != SanitiseText(message).Length)
                {
                    Plugin.Log?.Warning("Cannot send message: message contains invalid characters");
                    return;
                }

                sendMessageUnsafe(bytes, formatedMessage.Type);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to send chat message in Messages.SendMessage");
            }
        }

        private static unsafe string SanitiseText(string text)
        {
            try
            {
                Utf8String* uText = Utf8String.FromString(text);
                if (uText == null) return text;

                uText->SanitizeString((AllowedEntities)0x27F);
                var sanitised = uText->ToString();
                uText->Dtor(true);

                return sanitised;
            }
            catch
            {
                return text;
            }
        }
    }
}
