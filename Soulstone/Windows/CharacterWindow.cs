using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.ImGuiMethods;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Dalamud.Interface.Windowing.Window;
using static System.Net.Mime.MediaTypeNames;

namespace Soulstone.Windows
{
    internal class CharacterWindow
    {
        private float defaultNextToSpace = 3.0f;
        private float defaultFieldSpacing = 10.0f;
        private float defaultInputWidth = 175.0f;

        private float defaultContentHeight = 150.0f;


        private bool editingCharsheet = false;

        CharacterSheet currentCharacter = null;
        public CharacterWindow()
        {}

        public void Dispose() { }

        public void ManageInputField(ref string field, string fieldname)
        {
            if (editingCharsheet)
            {
                ImGui.SetNextItemWidth(defaultInputWidth);
                ImGui.InputText($"##{fieldname}", ref field, 100);
            }
            else
            {
                ImGui.Text(field);
            }
        }

        public void ManageBigInputField(ref string field, string fieldname)
        {
            if (editingCharsheet)
            {
                ImGui.SetNextItemWidth(-1.0f);
                ImGui.InputTextMultiline($"##{fieldname}", ref field, 5000, new Vector2(0.0f, 100.0f));
            }
            else
            {
                ImGui.TextWrapped(field);
            }
        }

        public void DrawCharTab(Plugin plugin)
        {
            if (CharacterManager.Instance.CharacterSheet != null)
            {
                currentCharacter = CharacterManager.Instance.CharacterSheet;
            }

            if (currentCharacter != null)
            {
                if (ImGui.Checkbox("Éditer la fiche de personnage", ref editingCharsheet))
                { }
                if (ImGui.Button("Sauvegarder la fiche de personnage"))
                {
                    CharacterSheet.SaveSheet(currentCharacter);
                }
                using (var child = ImRaii.Child("##Identity", new Vector2(0.0f,defaultContentHeight), true))
                {
                    ImGui.Text("Nom/Prénom :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterFullName, "FullName");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Surnom :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterNickName, "NickName");

                    ImGui.Text("Race :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterRace, "CharacterRace");
                    ImGui.SameLine(0.0f, defaultFieldSpacing);

                    ImGui.Text("Sous-Race :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterSubRace, "CharacterSubRace");

                    ImGui.Text("Classe :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterJob, "CharacterJob");

                    ImGui.Text("Sexe :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterSex, "CharacterSex");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Genre :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterGender, "CharacterGender");

                    ImGui.Text("Pronoms :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterPronouns, "CharacterPronouns");

                    ImGui.Text("Âge :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterAge, "CharacterAge");
                }

                using (var child = ImRaii.Child("##HRP", new Vector2(0.0f, defaultContentHeight), true))
                {
                    ImGui.Text("Infos HRP :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterInfo, "CharacterHrpInfo");

                    ImGui.Text("Timezone :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.playerTimezone, "PlayerTimezone");

                    ImGui.Text("Disponibilités :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.playerAvailability, "PlayerAvailability");
                }

                using (var child = ImRaii.Child("##Appearance", new Vector2(0.0f, defaultContentHeight), true))
                {
                    ImGui.Text("Taille :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterHeight, "CharacterHeight");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Poids :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterWeight, "CharacterWeight");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Corpulence :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterBuild, "CharacterBuild");

                    ImGui.Text("Couleur des yeux :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterEyeColor, "CharacterEyeColor");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Couleur des cheveux :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterHairColor, "CharacterHairColor");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Couleur de peau :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterSkinTone, "CharacterSkinTone");

                    ImGui.Text("Cicatrices :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterScars, "CharacterScars");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Tatouages :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterTattoos, "CharacterTattoos");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Autre(s) particularité(s) :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterDistinctiveFeatures, "CharacterDistinctiveFeatures");
                }

                using (var child = ImRaii.Child("##QuickLook", new Vector2(0.0f, defaultContentHeight), true))
                {
                    ImGui.Text("Aperçu rapide :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterQuickLook1, "CharacterQuickLook");

                    ImGui.Text("Aperçu rapide 2 :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterQuickLook2, "CharacterQuickLook2");

                    ImGui.Text("Aperçu rapide 3 :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterQuickLook3, "CharacterQuickLook3");

                    ImGui.Text("Aperçu rapide 4 :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterQuickLook4, "CharacterQuickLook4");

                    ImGui.Text("Aperçu rapide 5 :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterQuickLook5, "CharacterQuickLook5");
                }

                using (var child = ImRaii.Child("##Background", new Vector2(0.0f, defaultContentHeight), true))
                {
                    ImGui.Text("Lieu de Naissance :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterHomeland, "CharacterHomeland");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Origine :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterOrigin, "CharacterOrigin");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Affiliation :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterAffiliation, "CharacterAffiliation");

                    ImGui.SameLine(0.0f, defaultFieldSpacing);
                    ImGui.Text("Métier :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageInputField(ref currentCharacter.characterOccupation, "CharacterOccupation");

                    ImGui.Text("Réputation :");
                    ImGuiEx.Tooltip("La réputation de votre personnage dans son environnement social et professionnel.");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageBigInputField(ref currentCharacter.characterReputation, "CharacterReputation");

                    //TODO : Implémenter fonctionnement dictionnaires pour les relations

                    ImGui.Text("Histoire personnelle :");
                    ImGui.SameLine(0.0f, defaultNextToSpace);
                    ManageBigInputField(ref currentCharacter.characterBackground, "CharacterBackground");

                }
            }
        }
    }
}
