using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Soulstone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone
{
    internal class CharacterManager
    {
        private static CharacterManager? instance = null;
        private static readonly object padlock = new object();

        private CharacterSheet? characterSheet;

        private CharacterManager()
        {
            // Private constructor to prevent instantiation
        }

        public static CharacterManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CharacterManager();
                }
                return instance;
            }
        }

        internal CharacterSheet? CharacterSheet { get => characterSheet; set => characterSheet = value; }

        public void Init()
        {
            IPlayerCharacter localPlayer = Plugin.ClientState.LocalPlayer;
            SeString playerName = localPlayer.Name;
            Plugin.Log.Information($"Loading character data for {playerName.TextValue}");
            instance.CharacterSheet = instance.LoadCharacterData(playerName.TextValue);
        }

        private CharacterSheet LoadCharacterData(string charName)
        {

            CharacterSheet = CharacterSheet.LoadSheet(charName);
            if (CharacterSheet != null)
            {
                return CharacterSheet;
            }
            else
            {
                Plugin.Log.Warning("Failed to load character sheet.");
            }
            return null;
        }
    }
}
