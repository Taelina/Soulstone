using Dalamud.Game.Gui.Toast;
using Soulstone.Datamodels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Soulstone.Managers
{
    internal class InitiativeTrackerManager
    {
        private static InitiativeTrackerManager? instance;
        public static InitiativeTrackerManager Instance => instance ??= new InitiativeTrackerManager();

        public List<InitiativeParticipant> Participants { get; set; } = new();
        public int CurrentRound { get; set; } = 1;
        public int CurrentTurnNumber { get; set; } = 1;
        public int ActiveParticipantIndex { get; set; } = 0;
        public bool IsAscendingOrder { get; set; } = false;

        public InitiativeParticipant? ActiveParticipant
        {
            get
            {
                if (Participants.Count == 0) return null;
                if (ActiveParticipantIndex < 0 || ActiveParticipantIndex >= Participants.Count)
                {
                    ActiveParticipantIndex = 0;
                }
                return Participants[ActiveParticipantIndex];
            }
        }

        public void AddParticipant(InitiativeParticipant participant, bool autoSort = true)
        {
            Participants.Add(participant);
            if (autoSort)
            {
                SortParticipants(IsAscendingOrder);
            }
        }

        public InitiativeParticipant AddParticipant(string name, int initiativeValue, int bonusModifier = 0, bool isCurrentChar = false, string notes = "", bool autoSort = true)
        {
            var participant = new InitiativeParticipant(name, initiativeValue, bonusModifier, isCurrentChar, notes);
            AddParticipant(participant, autoSort);
            return participant;
        }

        public bool RemoveParticipant(string id)
        {
            int index = Participants.FindIndex(p => p.Id == id);
            if (index < 0) return false;

            Participants.RemoveAt(index);
            if (ActiveParticipantIndex >= Participants.Count)
            {
                ActiveParticipantIndex = Math.Max(0, Participants.Count - 1);
            }
            return true;
        }

        public void ClearParticipants()
        {
            Participants.Clear();
            ActiveParticipantIndex = 0;
        }

        public void SortParticipants(bool? ascending = null)
        {
            if (ascending.HasValue)
            {
                IsAscendingOrder = ascending.Value;
            }

            string? currentActiveId = (Participants.Count > 0 && ActiveParticipantIndex >= 0 && ActiveParticipantIndex < Participants.Count)
                ? Participants[ActiveParticipantIndex].Id
                : null;

            if (IsAscendingOrder)
            {
                Participants = Participants
                    .OrderBy(p => p.InitiativeValue)
                    .ThenBy(p => p.BonusModifier)
                    .ThenBy(p => p.Name)
                    .ToList();
            }
            else
            {
                Participants = Participants
                    .OrderByDescending(p => p.InitiativeValue)
                    .ThenByDescending(p => p.BonusModifier)
                    .ThenBy(p => p.Name)
                    .ToList();
            }

            if (currentActiveId != null)
            {
                int newIndex = Participants.FindIndex(p => p.Id == currentActiveId);
                if (newIndex >= 0)
                {
                    ActiveParticipantIndex = newIndex;
                }
            }
        }

        public void NextTurn()
        {
            if (Participants.Count == 0) return;

            if (ActiveParticipantIndex >= Participants.Count - 1)
            {
                ActiveParticipantIndex = 0;
                CurrentRound++;
            }
            else
            {
                ActiveParticipantIndex++;
            }

            CurrentTurnNumber++;

            var active = ActiveParticipant;
            if (active != null)
            {
                AnnounceTurn(active.Name, CurrentRound);
            }
        }

        public void PreviousTurn()
        {
            if (Participants.Count == 0) return;

            if (ActiveParticipantIndex <= 0)
            {
                if (CurrentRound > 1)
                {
                    CurrentRound--;
                    ActiveParticipantIndex = Participants.Count - 1;
                }
                else
                {
                    ActiveParticipantIndex = 0;
                }
            }
            else
            {
                ActiveParticipantIndex--;
            }

            if (CurrentTurnNumber > 1)
            {
                CurrentTurnNumber--;
            }
        }

        public void SetActiveIndex(int index)
        {
            if (Participants.Count == 0)
            {
                ActiveParticipantIndex = 0;
                return;
            }
            ActiveParticipantIndex = Math.Clamp(index, 0, Participants.Count - 1);
        }

        public void ResetTurns()
        {
            CurrentRound = 1;
            CurrentTurnNumber = 1;
            ActiveParticipantIndex = 0;
        }

        public void FullReset()
        {
            ResetTurns();
            ClearParticipants();
            IsAscendingOrder = false;
        }

        public void AnnounceTurn(string participantName, int roundNumber)
        {
            try
            {
                if (Plugin.ToastGui != null)
                {
                    var options = new QuestToastOptions
                    {
                        PlaySound = true,
                        DisplayCheckmark = false,
                        IconId = 0
                    };
                    string format = LocalizationManager.Instance.GetLocalizedString("InitiativeTurnNotificationFormat");
                    string message = string.Format(format, participantName);
                    Plugin.ToastGui.ShowQuest(message, options);
                }
                else
                {
                    Plugin.Log?.Information($"[Initiative Round {roundNumber}] {participantName}'s turn!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Information($"Initiative toast notification error: {ex.Message}");
            }
        }

        public void AddOrUpdateCurrentCharacter(CharacterSheet sheet, DiceSystem? diceSystem, int rolledTotal, int bonus)
        {
            string charName = !string.IsNullOrWhiteSpace(sheet.CharacterFullName) ? sheet.CharacterFullName : "Player Character";
            var existing = Participants.FirstOrDefault(p => p.IsCurrentCharacter || string.Equals(p.Name, charName, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Name = charName;
                existing.InitiativeValue = rolledTotal;
                existing.BonusModifier = bonus;
                existing.IsCurrentCharacter = true;
            }
            else
            {
                AddParticipant(new InitiativeParticipant(charName, rolledTotal, bonus, true));
            }
            SortParticipants(IsAscendingOrder);
        }
    }
}
