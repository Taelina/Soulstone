using Soulstone.Datamodels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Soulstone.Managers
{
    public class DiceHistoryEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string CharacterName { get; set; } = string.Empty;
        public string RolledBy { get; set; } = string.Empty;
        public string TargetCharacterName { get; set; } = string.Empty;
        public string RollName { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public int Total { get; set; }
        public string Details { get; set; } = string.Empty;
        public bool IsCriticalSuccess { get; set; }
        public bool IsCriticalFailure { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsLocal { get; set; }
        public string ResultDisplay { get; set; } = string.Empty;
    }

    public class DiceHistoryManager
    {
        private static DiceHistoryManager? instance;
        public static DiceHistoryManager Instance => instance ??= new DiceHistoryManager();

        private readonly List<DiceHistoryEntry> history = new();
        private readonly object lockObj = new();
        public const int MaxHistory = 50;

        public event Action? OnHistoryChanged;

        public IReadOnlyList<DiceHistoryEntry> GetHistory()
        {
            lock (lockObj)
            {
                return history.ToList();
            }
        }

        public void AddEntry(DiceHistoryEntry entry)
        {
            lock (lockObj)
            {
                history.Insert(0, entry);
                if (history.Count > MaxHistory)
                {
                    history.RemoveRange(MaxHistory, history.Count - MaxHistory);
                }
            }
            OnHistoryChanged?.Invoke();
        }

        public void Clear()
        {
            lock (lockObj)
            {
                history.Clear();
            }
            OnHistoryChanged?.Invoke();
        }
    }
}
