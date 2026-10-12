using System;

namespace Blockbloom.Core
{
    /// <summary>Oyun istatistikleri (GDD §7.2). Duz veri; JSON'a uygun.</summary>
    [Serializable]
    public sealed class GameStats
    {
        public int Moves;
        public int LinesCleared;
        public int MaxCombo;
        public int PerfectClears;
        /// <summary>Dagitilan tepsi sayisi; ilk tepsi dahil.</summary>
        public int TraysDealt;

        public GameStats Clone()
        {
            return (GameStats)MemberwiseClone();
        }
    }
}
