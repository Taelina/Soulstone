using Soulstone.Localizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Soulstone.Managers
{
    internal class LocalizationManager
    {
        private static LocalizationManager? instance = null;
        private Dictionary<Language, Localization> localizedLanguages = null;
        public Dictionary<Language, Localization> LocalizedLanguages { get => localizedLanguages; set => localizedLanguages = value; }
        private Configuration configuration;

        public LocalizationManager() 
        {
            LocalizedLanguages = new Dictionary<Language, Localization>();
        }

        public static LocalizationManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LocalizationManager();
                }
                return instance;
            }
        }

        public void InitLoc(Plugin plugin)
        {
            configuration = plugin.Configuration;
            Localization French = new Localization();
            French.Language = Language.Français;
            French.LocalizedStrings = new Dictionary<string, string>
            {
                //Generic Buttons
                {"AddButton", "+" },
                {"SupprButton", "-" },
                {"ThrowButton", "Lancer" },
                {"AddConfirmButton", "Ajouter" },
                //Config Window
                {"ConfigButton","Configurer" },
                {"ConfigDetailedRollsCheck", "Jets détaillés" },
                {"ConfigLanguageCombo", "Langue" },
                //Main Window Tabs
                {"RPTab", "Fiche RP" },
                {"DiceRollTab", "Lanceur de dés" },
                {"StatSheetTab","Fiche de Statistiques" },
                {"DiceSystemTab", "Système de dés" },
                //CharSheet Tab First part (Char Info)
                {"EditCharsheetCheck", "Editer la fiche de personnage" },
                {"SaveCharsheetButton", "Sauvegarder la fiche de personnage" },
                {"CharFullnameField", "Nom/Prénom :" },
                {"CharNicknameField", "Surnom :" },
                {"CharSpecieField", "Race :" },
                {"CharSubSpecieField", "Sous-race :" },
                {"CharClassField", "Classe :" },
                {"CharSexField", "Sexe :" },
                {"CharGenderField", "Genre :" },
                {"CharPronounsField", "Pronoms :" },
                {"CharAgeField", "Âge :" },
                //CharSheet Tab Second part (HRP)
                {"PlayerOOCInfo", "Infos HRP :" },
                {"PlayerTimezone", "Fuseau Horaire :" },
                {"PlayerAvailability", "Disponibilité :" },
                //Charsheet Tab, third part (Appearance)
                {"CharHeightField", "Taille :" },
                {"CharWeightField", "Poids :" },
                {"CharBuildField", "Corpulence :" },
                {"CharEyeColorField", "Couleur des yeux :" },
                {"CharHairColorField", "Couleur des cheveux :" },
                {"CharSkinColorField", "Couleur de peau :" },
                {"CharScarsField", "Cicatrices :" },
                {"CharTatooField", "Tatouages :" },
                {"CharOtherQuirkField", "Autre(s) particulrité(s)" },
                //Charsheet Tab, fourth part (Quick Looks)
                {"QuickLookField1", "Aperçu rapide :" },
                {"QuickLookField2", "Aperçu rapide 2 :" },
                {"QuickLookField3", "Aperçu rapide 3 :" },
                {"QuickLookField4", "Aperçu rapide 4 :" },
                {"QuickLookField5", "Aperçu rapide 5 :" },
                //Charsheet Tab, Fifth part (Background)
                {"CharBirthplaceField", "Lieu de Naissance :" },
                {"CharOriginField", "Origine :" },
                {"CharAffiliationField", "Affiliation :" },
                {"CharWorkField", "Metier :" },
                {"CharReputationField", "Réputation :" },
                {"CharFamilyRelationTab", "Relations familiales :" },
                {"CharFriendsTab", "Relations amicales :" },
                {"CharEnemiesTab", "Ennemis :" },
                {"CharBackgroundField", "Histoire personnelle :" },
                //Family member popup
                {"MemberNameField", "Nom du membre" },
                {"MemberDescriptionField", "Description" },
                //Friend popup
                {"FriendNameField", "Nom de l'ami" },
                {"FriendDescriptionField", "Description" },
                //Enemy Popup
                {"EnemyNameField", "Nom de l'ennemi" },
                {"EnemyDescriptionField", "Description" },
                //Dice Tab
                {"RollInputLabel", "Jet" },
                {"AdvantageCheckbox", "Avantage" },
                {"DisadvantageCheckbox", "Désavantage" },
                //Stat Tab
                {"SystemDiceTypeLabel", "Type de dé du système :" },
                {"EditStatCheckbox", "Editer les stats du personnage" },
                {"SaveStatButton", "Sauvegarder la fiche de personnage" },
                {"AdvantageRollCheckbox", "Jet avec avantage" },
                {"DisadvantageRollCheckbox", "Jet avec désavantage" },
                {"AttributeLabel", "Attributs :" },
                {"SkillLabel", "Compétences :" },
                {"AbilityLabel", "Capacités :" },
                {"NewAttributeNameLabel", "Nom de l'attribut" },
                {"NewAttributeValueLabel", "Valeur" },
                {"NewSkillName", "Nom de la compétence" },
                {"NewSkillValue", "Valeur" },
                {"NewLinkedAttribute", "Attribut lié" },
                {"NewAbilityName", "Nom de la capacité" },
                {"NewAbilityValue", "Valeur" },
                {"NewLinkedSkill", "Compétence lié" },
                {"SkillLinkText", "(lié à " },
                {"AbilityLinkText", "(lié à " },
                //Dice System Tab
                {"DiceSystemSaveButton", "Sauvegarder le système de dés" },
                {"DiceSystemNameLabel", "Nom du système de dés :" },
                {"SystemTypeCombo", "Type de système de dés" },
                {"DiceTypeCombo", "Type de dé :" },
                {"SuccessThresholdLabel", "Seuil de réussite (pour les systèmes à pool de dés) :" },
                {"SuccessIntervalLabel", "Interval de réussite (pour les systèmes pourcentage) :" },
                {"DndStyleAttrCheckbox", "Attributs de style DnD" },
                {"SkillUniqueAttrCheckbox", "Compétence liée à un seul attribut" },
                {"AbilityUniqueAttrCheckbox", "Capacité liée à un seul attribut" },
                {"AbilityUniqueSkillCheckbox", "Capacité lieée à une seule compétence" },
                {"DnDStyleSavesCheckbox", "Le système gère les jets de sauvegarde" },
                {"DnDStyleAdvDisadvCheckbox", "Le système gère l'avantage et le désavantage" },
            };

            Localization English = new Localization();
            English.Language = Language.English;
            English.LocalizedStrings = new Dictionary<string, string>
            {
                //Generic Buttons
                {"AddButton", "+" },
                {"SupprButton", "-" },
                {"ThrowButton", "Roll" },
                {"AddConfirmButton", "Add" },
                //Config Window
                {"ConfigButton","Config" },
                {"ConfigDetailedRollsCheck", "Detailed Dice throws" },
                {"ConfigLanguageCombo", "Language" },
                //Main Window Tabs
                {"RPTab", "RP Sheet" },
                {"DiceRollTab", "Dice Thrower" },
                {"StatSheetTab","Stat Sheet" },
                {"DiceSystemTab", "Dice System" },
                //CharSheet Tab First part (Char Info)
                {"EditCharsheetCheck", "Edit Character Sheet" },
                {"SaveCharsheetButton", "Save Character Sheet" },
                {"CharFullnameField", "Fullname :" },
                {"CharNicknameField", "Nickname :" },
                {"CharSpecieField", "Specie :" },
                {"CharSubSpecieField", "Sub-specie :" },
                {"CharClassField", "Job :" },
                {"CharSexField", "Sex :" },
                {"CharGenderField", "Gender :" },
                {"CharPronounsField", "Pronouns :" },
                {"CharAgeField", "Age :" },
                //CharSheet Tab Second part (HRP)
                {"PlayerOOCInfo", "OOC Info :" },
                {"PlayerTimezone", "Timezone :" },
                {"PlayerAvailability", "Availability :" },
                //Charsheet Tab, third part (Appearance)
                {"CharHeightField", "Height :" },
                {"CharWeightField", "Weight :" },
                {"CharBuildField", "Build :" },
                {"CharEyeColorField", "Eye color :" },
                {"CharHairColorField", "Hair color :" },
                {"CharSkinColorField", "Skin color :" },
                {"CharScarsField", "Scars :" },
                {"CharTatooField", "Tatoos :" },
                {"CharOtherQuirkField", "Other Quirks" },
                //Charsheet Tab, fourth part (Quick Looks)
                {"QuickLookField1", "Quick look :" },
                {"QuickLookField2", "Quick look 2 :" },
                {"QuickLookField3", "Quick look 3 :" },
                {"QuickLookField4", "Quick look 4 :" },
                {"QuickLookField5", "Quick look 5 :" },
                //Charsheet Tab, Fifth part (Background)
                {"CharBirthplaceField", "Birth place :" },
                {"CharOriginField", "Origin :" },
                {"CharAffiliationField", "Affiliation :" },
                {"CharWorkField", "Work :" },
                {"CharReputationField", "Reputation :" },
                {"CharFamilyRelationTab", "Family :" },
                {"CharFriendsTab", "Friends :" },
                {"CharEnemiesTab", "Enemies :" },
                {"CharBackgroundField", "Background :" },
                //Family member popup
                {"MemberNameField", "Member Name" },
                {"MemberDescriptionField", "Description" },
                //Friend popup
                {"FriendNameField", "Friend's name" },
                {"FriendDescriptionField", "Description" },
                //Enemy Popup
                {"EnemyNameField", "Enemy's name" },
                {"EnemyDescriptionField", "Description" },
                //Dice Window
                {"RollInputLabel", "Dice Roll" },
                {"AdvantageCheckbox", "Advantage" },
                {"DisadvantageCheckbox", "Disadvantage" },
                //Stat Tab
                {"SystemDiceTypeLabel", "System dice type :" },
                {"EditStatCheckbox", "Edit stats" },
                {"SaveStatButton", "Save stats" },
                {"AdvantageRollCheckbox", "Roll with advantage" },
                {"DisadvantageRollCheckbox", "Roll with disadvantage" },
                {"AttributeLabel", "Attributes :" },
                {"SkillLabel", "Skills :" },
                {"AbilityLabel", "Abilities :" },
                {"NewAttributeNameLabel", "Attribute name" },
                {"NewAttributeValueLabel", "Value" },
                {"NewSkillName", "Skill name" },
                {"NewSkillValue", "Value" },
                {"NewLinkedAttribute", "Linked attribute" },
                {"NewAbilityName", "Skill name" },
                {"NewAbilityValue", "Value" },
                {"NewLinkedSkill", "Linked Skill" },
                {"SkillLinkText", " (linked to " },
                {"AbilityLinkText", " (linked to " },
                //Dice System Tab
                {"DiceSystemSaveButton", "Save dice system" },
                {"DiceSystemNameLabel", "System name :" },
                {"SystemTypeCombo", "System type :" },
                {"DiceTypeCombo", "Dice Type :" },
                {"SuccessThresholdLabel", "Success Threshold (for dice pool systems) :" },
                {"SuccessIntervalLabel", "Success Interval (for percentile dice systems) :" },
                {"DndStyleAttrCheckbox", "DnD style attributes" },
                {"SkillUniqueAttrCheckbox", "Skill linked only to one attribute" },
                {"AbilityUniqueAttrCheckbox", "Ability linked only to one attribute" },
                {"AbilityUniqueSkillCheckbox", "Ability linked only to one skill" },
                {"DnDStyleSavesCheckbox", "System has saving throws" },
                {"DnDStyleAdvDisadvCheckbox", "System handles advantage and disadvantage" },

            };

            instance.LocalizedLanguages.Add(Language.Français, French);
            instance.LocalizedLanguages.Add(Language.English, English);

        }

        public string GetLocalizedString(string fieldName)
        {
            string value = "";
            Language language = configuration.Language;
            if(instance != null)
            {
                var locstrings = instance.localizedLanguages[language];
                
                if (locstrings != null)
                {
                    if (language != locstrings.Language)
                    {
                        Plugin.Log.Error($"Error : Language mismatch between {language.ToString()} and {locstrings.Language.ToString()}");
                    }
                    else
                        value = locstrings.LocalizedStrings[fieldName];
                }
                else
                {
                    Plugin.Log.Error("Error : Language not found.");
                }
            }

            return value;
        }
    }
}
