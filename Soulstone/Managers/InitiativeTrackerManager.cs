using Dalamud.Game.Gui.Toast;
using Soulstone.Datamodels;
using Soulstone.Utils;
using System;
using System.Collections.Generic;
using System.IO;
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

        private bool isHandlingRemoteUpdate = false;

        public InitiativeTrackerManager()
        {
            PartySyncManager.Instance.OnTurnAdvancedReceived += ApplyRemoteTurnAdvance;
            PartySyncManager.Instance.OnParticipantUpsertReceived += ApplyRemoteParticipantUpsert;
            PartySyncManager.Instance.OnParticipantRemovedReceived += ApplyRemoteParticipantRemove;
            PartySyncManager.Instance.OnInitiativeResetReceived += ApplyRemoteReset;
            PartySyncManager.Instance.OnInitiativeSyncReceived += ApplyRemoteFullSync;
        }

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
            SyncParticipantWithCharacterSheet(participant);
            if (autoSort)
            {
                SortParticipants(IsAscendingOrder);
            }

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader()))
            {
                PartySyncManager.Instance.BroadcastParticipantUpsert(participant);
            }
        }

        public void ImportPartyMembers(IEnumerable<PartyMemberSyncData> members, DiceSystem? system = null)
        {
            if (members == null) return;

            var sheet = CharacterManager.Instance.CharacterSheet;
            var sys = system ?? DiceSystemManager.Instance.CurrentDiceSystem;

            foreach (var member in members)
            {
                if (string.IsNullOrWhiteSpace(member.CharacterName)) continue;

                var existing = Participants.FirstOrDefault(p => string.Equals(p.Name, member.CharacterName, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    if (member.ActiveBuffs != null && member.ActiveBuffs.Count > 0)
                    {
                        foreach (var buff in member.ActiveBuffs)
                        {
                            if (!existing.Buffs.Any(b => string.Equals(b.Name, buff.Name, StringComparison.OrdinalIgnoreCase)))
                            {
                                existing.AddBuff(buff);
                            }
                        }
                    }
                    continue;
                }

                bool isMatch = sheet != null && !string.IsNullOrWhiteSpace(sheet.CharacterFullName) && string.Equals(sheet.CharacterFullName, member.CharacterName, StringComparison.OrdinalIgnoreCase);

                int bonus = 0;
                int initVal = 10;

                if (isMatch && sheet != null)
                {
                    bonus = sheet.GetInitiativeModifier(sys);
                    initVal = sheet.RollInitiative(sys).RollResult;
                }
                else
                {
                    var roll = DiceRoll.RollStatWithSystem(sys, "Initiative", bonus)
                        ?? DiceRoll.RollDiceRegular(1, DiceRoll.GetSystemSides(sys), bonus, "Initiative");
                    initVal = roll.RollResult;
                }

                var buffs = member.ActiveBuffs != null ? new List<Buff>(member.ActiveBuffs) : new List<Buff>();
                var participant = new InitiativeParticipant(member.CharacterName, initVal, bonus, isMatch, member.JobName ?? "", buffs, isNpc: false);
                AddParticipant(participant, false);
            }

            SortParticipants(IsAscendingOrder);
        }

        public InitiativeParticipant AddParticipant(string name, int initiativeValue, int bonusModifier = 0, bool isCurrentChar = false, string notes = "", List<Buff>? buffs = null, CharacterSheet? characterSheet = null, string? sheetFilePath = null, bool autoSort = true, bool isNpc = false)
        {
            var sheet = CharacterManager.Instance.CharacterSheet;
            bool isMatch = isCurrentChar || (sheet != null && !string.IsNullOrWhiteSpace(sheet.CharacterFullName) && string.Equals(sheet.CharacterFullName, name, StringComparison.OrdinalIgnoreCase));

            List<Buff> initialBuffs = buffs != null ? new List<Buff>(buffs) : new List<Buff>();
            if (isMatch && initialBuffs.Count == 0 && sheet?.ActiveBuffs != null && sheet.ActiveBuffs.Count > 0)
            {
                initialBuffs = new List<Buff>(sheet.ActiveBuffs);
            }
            else if (characterSheet?.ActiveBuffs != null && characterSheet.ActiveBuffs.Count > 0 && initialBuffs.Count == 0)
            {
                initialBuffs = new List<Buff>(characterSheet.ActiveBuffs);
            }

            var participant = new InitiativeParticipant(name, initiativeValue, bonusModifier, isMatch, notes, initialBuffs, characterSheet, sheetFilePath, isNpc);
            AddParticipant(participant, autoSort);
            return participant;
        }

        public DiceRoll? RerollParticipant(string participantId, DiceSystem? diceSystem = null, bool advantage = false, bool disadvantage = false, bool detailedRoll = false)
        {
            var participant = Participants.FirstOrDefault(p => p.Id == participantId);
            if (participant == null) return null;

            var sys = diceSystem ?? DiceSystemManager.Instance.CurrentDiceSystem;
            var localSheet = CharacterManager.Instance.CharacterSheet;

            DiceRoll roll;
            if (participant.IsCurrentCharacter && localSheet != null)
            {
                roll = localSheet.RollInitiative(sys, advantage, disadvantage, detailedRoll);
                participant.InitiativeValue = roll.RollResult;
                participant.BonusModifier = localSheet.GetInitiativeModifier(sys);
                if (localSheet.ActiveBuffs != null)
                {
                    participant.Buffs = new List<Buff>(localSheet.ActiveBuffs);
                }
            }
            else
            {
                roll = participant.RollInitiative(sys, advantage, disadvantage, detailedRoll);
            }

            SyncParticipantWithCharacterSheet(participant);
            SortParticipants(IsAscendingOrder);

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader() || participant.IsCurrentCharacter))
            {
                PartySyncManager.Instance.BroadcastParticipantUpsert(participant);
            }

            return roll;
        }

        public void AttachSheetToParticipant(string participantId, CharacterSheet sheet, string? filePath = null)
        {
            var participant = Participants.FirstOrDefault(p => p.Id == participantId);
            if (participant == null || sheet == null) return;

            participant.CharacterSheet = sheet;
            participant.SheetFilePath = filePath;
            if (string.IsNullOrWhiteSpace(participant.Name) && !string.IsNullOrWhiteSpace(sheet.CharacterFullName))
            {
                participant.Name = sheet.CharacterFullName;
            }

            var sys = DiceSystemManager.Instance.CurrentDiceSystem;
            participant.BonusModifier = sheet.GetInitiativeModifier(sys);

            if (sheet.ActiveBuffs != null && sheet.ActiveBuffs.Count > 0)
            {
                foreach (var b in sheet.ActiveBuffs)
                {
                    if (!participant.Buffs.Any(pb => string.Equals(pb.Name, b.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        participant.AddBuff(b);
                    }
                }
            }

            SyncParticipantWithCharacterSheet(participant);

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader()))
            {
                PartySyncManager.Instance.BroadcastParticipantUpsert(participant);
            }
        }

        public void DetachSheetFromParticipant(string participantId)
        {
            var participant = Participants.FirstOrDefault(p => p.Id == participantId);
            if (participant == null) return;

            participant.CharacterSheet = null;
            participant.SheetFilePath = null;

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader()))
            {
                PartySyncManager.Instance.BroadcastParticipantUpsert(participant);
            }
        }

        public static List<string> GetAvailablePremadeSheetFiles()
        {
            var result = new List<string>();
            try
            {
                string dir = $"{Plugin.dataLocation}/sheets";
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*.json");
                    foreach (var f in files)
                    {
                        result.Add(f);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to get available sheet files");
            }
            return result;
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

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader()))
            {
                PartySyncManager.Instance.BroadcastParticipantRemove(id);
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

            var currentActor = ActiveParticipant;
            if (currentActor != null)
            {
                var expired = currentActor.TickBuffs();
                foreach (var buff in expired)
                {
                    AnnounceBuffExpired(currentActor.Name, buff.Name);
                }
                SyncParticipantWithCharacterSheet(currentActor);
            }

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

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader()))
            {
                string echoMsg = active != null ? LocalizationManager.Instance.GetLocalizedString("InitiativeTurnEcho", CurrentRound, CurrentTurnNumber, active.Name) : "";
                PartySyncManager.Instance.BroadcastInitiativeTurn(CurrentRound, CurrentTurnNumber, active?.Id, echoMsg);
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

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader()))
            {
                var active = ActiveParticipant;
                PartySyncManager.Instance.BroadcastInitiativeTurn(CurrentRound, CurrentTurnNumber, active?.Id);
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

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader()))
            {
                PartySyncManager.Instance.BroadcastInitiativeReset(LocalizationManager.Instance.GetLocalizedString("InitiativeResetEcho"));
            }
        }

        public void FullReset()
        {
            ResetTurns();
            ClearParticipants();
            IsAscendingOrder = false;

            if (!isHandlingRemoteUpdate && (PartySyncManager.Instance.IsSessionHost || PartySyncManager.Instance.IsLocalPlayerPartyLeader()))
            {
                PartySyncManager.Instance.BroadcastInitiativeReset(LocalizationManager.Instance.GetLocalizedString("InitiativeClearedEcho"));
            }
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
                Plugin.Log?.Warning(ex, $"Initiative toast notification error: {ex.Message}");
            }
        }

        public void SyncParticipantWithCharacterSheet(InitiativeParticipant? participant)
        {
            if (participant == null) return;
            var sheet = CharacterManager.Instance.CharacterSheet;
            if (sheet != null && (participant.IsCurrentCharacter || (!string.IsNullOrWhiteSpace(sheet.CharacterFullName) && string.Equals(participant.Name, sheet.CharacterFullName, StringComparison.OrdinalIgnoreCase))))
            {
                participant.IsCurrentCharacter = true;
                sheet.ActiveBuffs = new List<Buff>(participant.Buffs ?? new List<Buff>());
            }
            else if (participant.CharacterSheet != null)
            {
                participant.CharacterSheet.ActiveBuffs = new List<Buff>(participant.Buffs ?? new List<Buff>());
            }
        }

        public void AnnounceBuffExpired(string participantName, string buffName)
        {
            try
            {
                if (Plugin.ToastGui != null)
                {
                    var options = new QuestToastOptions
                    {
                        PlaySound = false,
                        DisplayCheckmark = false,
                        IconId = 0
                    };
                    string format = LocalizationManager.Instance.GetLocalizedString("BuffExpiredNotificationFormat");
                    string message = string.Format(format, buffName, participantName);
                    Plugin.ToastGui.ShowQuest(message, options);
                }
                else
                {
                    Plugin.Log?.Information($"[Buff Expired] '{buffName}' on {participantName} has expired.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Warning(ex, $"Buff toast notification error: {ex.Message}");
            }
        }

        public void AddBuffToParticipant(string participantId, Buff buff)
        {
            var p = Participants.FirstOrDefault(x => x.Id == participantId);
            if (p != null)
            {
                p.AddBuff(buff);
                SyncParticipantWithCharacterSheet(p);
            }
        }

        public bool RemoveBuffFromParticipant(string participantId, string buffId)
        {
            var p = Participants.FirstOrDefault(x => x.Id == participantId);
            if (p != null)
            {
                bool res = p.RemoveBuff(buffId);
                SyncParticipantWithCharacterSheet(p);
                return res;
            }
            return false;
        }

        public List<Buff> TickParticipantBuffs(string participantId, int turns = 1)
        {
            var p = Participants.FirstOrDefault(x => x.Id == participantId);
            if (p != null)
            {
                var expired = p.TickBuffs(turns);
                foreach (var buff in expired)
                {
                    AnnounceBuffExpired(p.Name, buff.Name);
                }
                SyncParticipantWithCharacterSheet(p);
                return expired;
            }
            return new List<Buff>();
        }

        public Dictionary<string, List<Buff>> TickAllBuffs(int turns = 1)
        {
            var results = new Dictionary<string, List<Buff>>();
            foreach (var p in Participants)
            {
                var expired = p.TickBuffs(turns);
                if (expired.Count > 0)
                {
                    results[p.Id] = expired;
                    foreach (var buff in expired)
                    {
                        AnnounceBuffExpired(p.Name, buff.Name);
                    }
                }
                SyncParticipantWithCharacterSheet(p);
            }
            return results;
        }

        public void AddOrUpdateCurrentCharacter(CharacterSheet sheet, DiceSystem? diceSystem, int rolledTotal, int bonus)
        {
            string charName = !string.IsNullOrWhiteSpace(sheet.CharacterFullName) ? sheet.CharacterFullName : "Player Character";
            var existing = Participants.FirstOrDefault(p => p.IsCurrentCharacter || string.Equals(p.Name, charName, StringComparison.OrdinalIgnoreCase));
            InitiativeParticipant p;
            if (existing != null)
            {
                existing.Name = charName;
                existing.InitiativeValue = rolledTotal;
                existing.BonusModifier = bonus;
                existing.IsCurrentCharacter = true;
                if (sheet.ActiveBuffs != null)
                {
                    existing.Buffs = new List<Buff>(sheet.ActiveBuffs);
                }
                p = existing;
            }
            else
            {
                p = new InitiativeParticipant(charName, rolledTotal, bonus, true, isNpc: false);
                if (sheet.ActiveBuffs != null)
                {
                    p.Buffs = new List<Buff>(sheet.ActiveBuffs);
                }
                AddParticipant(p);
            }
            SortParticipants(IsAscendingOrder);

            // Broadcast self initiative to party
            PartySyncManager.Instance.BroadcastParticipantUpsert(p, $"[Initiative] {charName} rolled {rolledTotal} for initiative!");
        }

        public void ApplyRemoteTurnAdvance(InitiativeTurnPayload payload)
        {
            if (payload == null) return;
            isHandlingRemoteUpdate = true;
            try
            {
                CurrentRound = payload.Round;
                CurrentTurnNumber = payload.TurnNumber;
                if (!string.IsNullOrWhiteSpace(payload.ActiveParticipantId))
                {
                    int index = Participants.FindIndex(p => p.Id == payload.ActiveParticipantId);
                    if (index >= 0)
                    {
                        ActiveParticipantIndex = index;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to apply remote turn advance in InitiativeTrackerManager");
            }
            finally
            {
                isHandlingRemoteUpdate = false;
            }
        }

        public void ApplyRemoteParticipantUpsert(InitiativeParticipant participant)
        {
            if (participant == null) return;
            isHandlingRemoteUpdate = true;
            try
            {
                int index = Participants.FindIndex(p => p.Id == participant.Id || (!string.IsNullOrWhiteSpace(p.Name) && string.Equals(p.Name, participant.Name, StringComparison.OrdinalIgnoreCase)));
                if (index >= 0)
                {
                    Participants[index] = participant;
                }
                else
                {
                    Participants.Add(participant);
                }
                SyncParticipantWithCharacterSheet(participant);
                SortParticipants(IsAscendingOrder);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to apply remote participant upsert for '{participant?.Name}'");
            }
            finally
            {
                isHandlingRemoteUpdate = false;
            }
        }

        public void ApplyRemoteParticipantRemove(string participantId)
        {
            if (string.IsNullOrWhiteSpace(participantId)) return;
            isHandlingRemoteUpdate = true;
            try
            {
                int index = Participants.FindIndex(p => p.Id == participantId || string.Equals(p.Name, participantId, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    Participants.RemoveAt(index);
                    if (ActiveParticipantIndex >= Participants.Count)
                    {
                        ActiveParticipantIndex = Math.Max(0, Participants.Count - 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, $"Failed to apply remote participant remove for '{participantId}'");
            }
            finally
            {
                isHandlingRemoteUpdate = false;
            }
        }

        public void ApplyRemoteReset()
        {
            isHandlingRemoteUpdate = true;
            try
            {
                CurrentRound = 1;
                CurrentTurnNumber = 1;
                ActiveParticipantIndex = 0;
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to apply remote initiative reset");
            }
            finally
            {
                isHandlingRemoteUpdate = false;
            }
        }

        public void ApplyRemoteFullSync(InitiativeSyncPayload payload)
        {
            if (payload == null) return;
            isHandlingRemoteUpdate = true;
            try
            {
                CurrentRound = payload.Round;
                CurrentTurnNumber = payload.TurnNumber;
                if (payload.Participants != null)
                {
                    Participants = new List<InitiativeParticipant>(payload.Participants);
                    foreach (var p in Participants)
                    {
                        SyncParticipantWithCharacterSheet(p);
                    }
                    SortParticipants(IsAscendingOrder);
                }
                if (!string.IsNullOrWhiteSpace(payload.ActiveParticipantId))
                {
                    int index = Participants.FindIndex(p => p.Id == payload.ActiveParticipantId);
                    if (index >= 0)
                    {
                        ActiveParticipantIndex = index;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex, "Failed to apply remote full initiative sync");
            }
            finally
            {
                isHandlingRemoteUpdate = false;
            }
        }
    }
}
