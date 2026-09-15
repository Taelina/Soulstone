using Soulstone.Managers;
using System;
using Xunit;

namespace Soulstone.Tests.Managers
{
    [Collection("NonParallelCollection")]
    public class DiceHistoryManagerTests
    {
        [Fact]
        public void AddEntry_And_Clear_WorksCorrectly()
        {
            var mgr = DiceHistoryManager.Instance;
            mgr.Clear();

            Assert.Empty(mgr.GetHistory());

            mgr.AddEntry(new DiceHistoryEntry
            {
                CharacterName = "Player One",
                RolledBy = "Player One",
                RollName = "Stealth",
                Formula = "1d20+5",
                Total = 18,
                Details = "13 + 5",
                IsPrivate = true,
                IsLocal = true,
                ResultDisplay = "18 (13 + 5)"
            });

            var history = mgr.GetHistory();
            Assert.Single(history);
            Assert.Equal("Player One", history[0].CharacterName);
            Assert.Equal("Stealth", history[0].RollName);
            Assert.True(history[0].IsPrivate);
            Assert.True(history[0].IsLocal);

            mgr.Clear();
            Assert.Empty(mgr.GetHistory());
        }

        [Fact]
        public void MaxHistory_TruncatesOldEntries()
        {
            var mgr = DiceHistoryManager.Instance;
            mgr.Clear();

            for (int i = 0; i < DiceHistoryManager.MaxHistory + 10; i++)
            {
                mgr.AddEntry(new DiceHistoryEntry
                {
                    CharacterName = $"Char_{i}",
                    RollName = $"Roll_{i}",
                    Total = i
                });
            }

            var history = mgr.GetHistory();
            Assert.Equal(DiceHistoryManager.MaxHistory, history.Count);
            Assert.Equal($"Roll_{DiceHistoryManager.MaxHistory + 9}", history[0].RollName);

            mgr.Clear();
        }
    }
}
