namespace Blockbloom.Core
{
    /// <summary>
    /// Tum ayarlanabilir sabitler tek yerde (GDD: KALIN_BUYUK adli degerler).
    /// Degerler GDD v0.1 baslangic degerleridir; denge simulasyonuyla ayarlanir (GDD §10).
    /// Bir degeri degistirmek eski tohumlarin oyunlarini degistirebilir; karar kaydi gerekir.
    /// </summary>
    public static class CoreConfig
    {
        // --- Izgara ve tepsi (GDD §3) ---
        public const int BoardSize = 8;
        public const int CellCount = BoardSize * BoardSize;
        public const int TraySlotCount = 3;
        /// <summary>Renk kimlikleri 1..ColorCount; 0 = bos (GDD §3.1).</summary>
        public const int ColorCount = 6;

        // --- Puanlama (GDD §6) ---
        public const int PointsPerCell = 1;
        /// <summary>Patlatma: LineBasePoints x n^2 x K.</summary>
        public const int LineBasePoints = 10;
        /// <summary>TAM_TEMIZLIK_BONUSU; kombo ile carpilmaz.</summary>
        public const int PerfectClearBonus = 300;

        // --- Parca siniflari (GDD §4) ---
        /// <summary>K (kucuk): hucre sayisi bu degere esit veya kucuk.</summary>
        public const int SmallMaxCells = 3;
        /// <summary>O (orta): hucre sayisi tam olarak bu deger. Ustu B (buyuk).</summary>
        public const int MediumCells = 4;

        // --- Uretim (GDD §5.4) ---
        /// <summary>Ayni parca bir tepside en fazla bu kadar gecer.</summary>
        public const int MaxSamePiecePerTray = 2;

        // --- Zorluk esikleri (GDD §5.3) ---
        public const int ReliefMinFilled = 32;
        public const int ReliefMinTraysWithoutClear = 2;
        public const int OpenMaxFilled = 15;

        // --- Zorluk profilleri (GDD §5.3; carpanlar onda bir, tamsayi) ---
        public const int ReliefSmallMul = 15;
        public const int ReliefMediumMul = 10;
        public const int ReliefLargeMul = 5;
        public const int ReliefFitMul = 3;
        public const int ReliefMinFitting = 2;
        public const int ReliefMaxLarge = 1;

        public const int OpenSmallMul = 8;
        public const int OpenMediumMul = 10;
        public const int OpenLargeMul = 13;
        public const int OpenFitMul = 1;
        public const int OpenMinFitting = 1;
        public const int OpenMaxLarge = 3;

        public const int NormalSmallMul = 10;
        public const int NormalMediumMul = 10;
        public const int NormalLargeMul = 10;
        public const int NormalFitMul = 1;
        public const int NormalMinFitting = 1;
        public const int NormalMaxLarge = 3;

        // --- RNG (GDD §5.1) ---
        public const ulong DefaultRngStream = 0xDA3E39CB94B95BDBUL;

        // --- Anlik goruntu ---
        public const int SnapshotVersion = 1;
    }
}
