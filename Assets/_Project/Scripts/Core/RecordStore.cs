using System;

namespace Blockbloom.Core
{
    /// <summary>Kalici rekor deposu (GDD §7.3). Unity katmani uygular (ornegin PlayerPrefs).</summary>
    public interface IRecordStore
    {
        int BestScore { get; }
        int GamesPlayed { get; }
        int BestCombo { get; }
        void Save(int bestScore, int gamesPlayed, int bestCombo);
    }

    /// <summary>Bellekte calisan depo (testler, simulasyon, editor).</summary>
    public sealed class MemoryRecordStore : IRecordStore
    {
        public MemoryRecordStore()
        {
        }

        public MemoryRecordStore(int bestScore, int gamesPlayed, int bestCombo)
        {
            Save(bestScore, gamesPlayed, bestCombo);
            SaveCount = 0;
        }

        public int BestScore { get; private set; }
        public int GamesPlayed { get; private set; }
        public int BestCombo { get; private set; }

        /// <summary>Save cagri sayisi (testler icin).</summary>
        public int SaveCount { get; private set; }

        public void Save(int bestScore, int gamesPlayed, int bestCombo)
        {
            if (bestScore < 0 || gamesPlayed < 0 || bestCombo < 0) throw new ArgumentOutOfRangeException(nameof(bestScore));
            BestScore = bestScore;
            GamesPlayed = gamesPlayed;
            BestCombo = bestCombo;
            SaveCount++;
        }
    }
}
