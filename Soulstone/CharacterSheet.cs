using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Soulstone
{
    internal class CharacterSheet
    {
        //Character identity static fields
        private string characterFullName;
        private string characterNickName;
        private string characterRace;
        private string characterSubRace;
        private string characterSex;
        private string characterGender;
        private string characterPronouns;
        private string characterAge;

        //Character physical description static fields
        private string characterHeight;
        private string characterWeight;
        private string characterBuild;
        private string characterEyeColor;
        private string characterHairColor;
        private string characterSkinTone;
        private string characterScars;
        private string characterTattoos;

        //Character background static fields
        private string characterHomeland;
        private string characterOrigin;
        private string characterAffiliation;
        private string characterOccupation;
        private string characterBackground;

        //Character OOC fields
        private string characterNotes;
        private string characterInfo;
        private string playerAvailability;
        private string playerTimezone;
        private string playerNotes;

        //Character dynamic background fields
        private Dictionary<string, string> characterFamily;
        private Dictionary<string, string> characterFriends;
        private Dictionary<string, string> characterEnnemies;

        //Character dynamic inventory fields
        // TODO : Implement inventory system

        //Character Dynamic ability fields
        private Dictionary<string, int> characterAttributes;
        private Dictionary<string, int> characterSkills;
        private Dictionary<string, int> characterAbilities;

        public string CharacterFullName { get => characterFullName; set => characterFullName = value; }
        public string CharacterNickName { get => characterNickName; set => characterNickName = value; }
        public string CharacterRace { get => characterRace; set => characterRace = value; }
        public string CharacterSubRace { get => characterSubRace; set => characterSubRace = value; }
        public string CharacterSex { get => characterSex; set => characterSex = value; }
        public string CharacterGender { get => characterGender; set => characterGender = value; }
        public string CharacterPronouns { get => characterPronouns; set => characterPronouns = value; }
        public string CharacterAge { get => characterAge; set => characterAge = value; }
        public string CharacterHeight { get => characterHeight; set => characterHeight = value; }
        public string CharacterWeight { get => characterWeight; set => characterWeight = value; }
        public string CharacterBuild { get => characterBuild; set => characterBuild = value; }
        public string CharacterEyeColor { get => characterEyeColor; set => characterEyeColor = value; }
        public string CharacterHairColor { get => characterHairColor; set => characterHairColor = value; }
        public string CharacterSkinTone { get => characterSkinTone; set => characterSkinTone = value; }
        public string CharacterScars { get => characterScars; set => characterScars = value; }
        public string CharacterTattoos { get => characterTattoos; set => characterTattoos = value; }
        public string CharacterHomeland { get => characterHomeland; set => characterHomeland = value; }
        public string CharacterOrigin { get => characterOrigin; set => characterOrigin = value; }
        public string CharacterAffiliation { get => characterAffiliation; set => characterAffiliation = value; }
        public string CharacterOccupation { get => characterOccupation; set => characterOccupation = value; }
        public string CharacterBackground { get => characterBackground; set => characterBackground = value; }
        public string CharacterNotes { get => characterNotes; set => characterNotes = value; }
        public string CharacterInfo { get => characterInfo; set => characterInfo = value; }
        public string PlayerAvailability { get => playerAvailability; set => playerAvailability = value; }
        public string PlayerTimezone { get => playerTimezone; set => playerTimezone = value; }
        public string PlayerNotes { get => playerNotes; set => playerNotes = value; }
        public Dictionary<string, string> CharacterFamily { get => characterFamily; set => characterFamily = value; }
        public Dictionary<string, string> CharacterFriends { get => characterFriends; set => characterFriends = value; }
        public Dictionary<string, string> CharacterEnnemies { get => characterEnnemies; set => characterEnnemies = value; }
        public Dictionary<string, int> CharacterAttributes { get => characterAttributes; set => characterAttributes = value; }
        public Dictionary<string, int> CharacterSkills { get => characterSkills; set => characterSkills = value; }
        public Dictionary<string, int> CharacterAbilities { get => characterAbilities; set => characterAbilities = value; }

        public static CharacterSheet LoadSheet(string characterName)
        {
            CharacterSheet loadedSheet = null;
            string formatedName = characterName.Replace(" ", "_").ToLower();
            if (!File.Exists($"{Plugin.dataLocation}/sheets/{formatedName}.json"))
            {
                Plugin.Log.Information("No existing character sheet found, creating a new one.");
                CharacterSheet newsheet = new CharacterSheet();
                newsheet.CharacterFullName = characterName;
                SaveSheet(newsheet);
            }

            string loadedfile = File.ReadAllText($"{Plugin.dataLocation}/sheets/{formatedName}.json");

            if(!string.IsNullOrEmpty(loadedfile))
            {
                loadedSheet = JsonSerializer.Deserialize<CharacterSheet>(loadedfile);
            }

            if (loadedSheet != null)
            {
                return loadedSheet;
            }
            else
            {
                Plugin.Log.Information("Failed to load character sheet.");
            }
            return loadedSheet;
        }

        public static void SaveSheet(CharacterSheet sheet)
        {
            if(!Directory.Exists($"{Plugin.dataLocation}/sheets"))
            {
                Directory.CreateDirectory($"{Plugin.dataLocation}/sheets");
            }
            string characterName = sheet.CharacterFullName.Replace(" ", "_").ToLower();
            File.WriteAllText($"{Plugin.dataLocation}/sheets/{characterName}.json", JsonSerializer.Serialize(sheet));
        }
    }
}
