using System;

namespace Blockbloom.Core
{
    /// <summary>
    /// Oyunun tam durumu; duz veri, JSON'a uygun (public alanlar, Unity JsonUtility ile uyumlu).
    /// GameState.FromSnapshot(s) ile devam eden oyun, kesintisiz oyunla bit duzeyinde aynidir.
    /// </summary>
    [Serializable]
    public sealed class GameSnapshot
    {
        public int Version = CoreConfig.SnapshotVersion;
        public ulong Seed;
        public ulong Bits;
        /// <summary>64 bayt, indeks y*8+x; 0 bos, 1..6 renk.</summary>
        public byte[] Colors = new byte[CoreConfig.CellCount];
        /// <summary>3 oge; bos yuva null (FromSnapshot "" degerini de bos yuva sayar).</summary>
        public string[] TrayIds = new string[CoreConfig.TraySlotCount];
        /// <summary>3 oge; bos yuva 0.</summary>
        public byte[] TrayColors = new byte[CoreConfig.TraySlotCount];
        public int Score;
        public int Combo;
        public int MaxCombo;
        public int TraysWithoutClear;
        /// <summary>
        /// Mevcut tepside su ana kadar patlatma oldu mu. GDD §9 listesinde yok; T'nin dogru
        /// hesaplanmasi (ve S3 determinizmi) icin gerekli oldugundan eklendi.
        /// </summary>
        public bool ClearedInCurrentTray;
        public ulong RngState;
        public ulong RngInc;
        public GameStats Stats = new GameStats();
    }
}
