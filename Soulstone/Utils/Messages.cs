using Dalamud.Game.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Utils
{
    // From: https://git.anna.lgbt/anna/XivCommon/src/branch/main/XivCommon/Functions/Chat.cs
    internal class Messages
    {
        public static unsafe void sendMessageUnsafe(byte[] message, XivChatType? type)
        {
            Utf8String* mes = Utf8String.FromSequence(message);
            UIModule.Instance()->ProcessChatBoxEntry(mes, (byte)type);
            mes->Dtor(true);
        }

        public static unsafe void SendMessage(XivChatEntry formatedMessage)
        {
            string message = formatedMessage.Message.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            if (bytes.Length == 0)
                throw new ArgumentException("message is empty", nameof(message));

            if (bytes.Length > 500)
                throw new ArgumentException("message is longer than 500 bytes", nameof(message));

            if (message.Length != SanitiseText(message).Length)
                throw new ArgumentException("message contained invalid characters", nameof(message));

            sendMessageUnsafe(bytes, formatedMessage.Type);
        }

        private static unsafe string SanitiseText(string text)
        {
            Utf8String* uText = Utf8String.FromString(text);

            uText->SanitizeString((AllowedEntities)0x27F);
            var sanitised = uText->ToString();
            uText->Dtor(true);

            return sanitised;
        }
    }
}
