using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Soulstone.Datamodels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Managers
{
    internal class CharacterManager
    {
        private static CharacterManager? instance = null;
        private static readonly object padlock = new object();

        private static bool charLoaded = false;

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
            try
            {
                if (Plugin.ObjectTable == null) return;
                var localPlayer = Plugin.ObjectTable.LocalPlayer;
                if (localPlayer != null)
                {
                    SeString playerName = localPlayer.Name;
                    Plugin.Log?.Information($"Loading character data for {playerName.TextValue}");
                    CharacterSheet = LoadCharacterData(playerName.TextValue);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to initialize character data in CharacterManager.Init()");
            }
        }

        public void Reset()
        {
            charLoaded = false;
            characterSheet = null;
        }

        public void ForceLoadCharData(string charName)
        {
            try
            {
                Plugin.Log?.Information($"Force loading character data for {charName}");
                CharacterSheet = CharacterSheet.LoadSheet(charName);
                if (CharacterSheet != null)
                {
                    charLoaded = true;
                }
                else
                {
                    Plugin.Log?.Warning($"Failed to force load character sheet for '{charName}'.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Exception in ForceLoadCharData for '{charName}'");
            }
        }

        private CharacterSheet? LoadCharacterData(string charName)
        {
            try
            {
                if (!charLoaded || CharacterSheet == null || !string.Equals(CharacterSheet.CharacterFullName, charName, StringComparison.OrdinalIgnoreCase))
                {
                    CharacterSheet = CharacterSheet.LoadSheet(charName);
                }
                if (CharacterSheet != null)
                {
                    charLoaded = true;
                    return CharacterSheet;
                }
                else
                {
                    Plugin.Log?.Warning($"Failed to load character sheet for '{charName}'.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Exception in LoadCharacterData for '{charName}'");
            }
            return null;
        }
    }
}
