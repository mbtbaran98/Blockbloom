using Blockbloom.Core;

namespace Blockbloom.Sim
{
    /// <summary>
    /// Duyarlilik taramasi icin Core'un zorluk kurallarinin (GDD §5.3) arac icindeki parametreli kopyasi.
    /// Core DEGISTIRILMEZ; varyantlar Generator.NextTray(grid, WeightProfile) ile uygulanir.
    /// Baseline() Core ile ayni sonucu vermelidir (kosucuda denetlenir).
    /// </summary>
    public sealed class SimDifficulty
    {
        public string Name;
        public string Description;
        public bool AlwaysNormal;
        public int ReliefMinFilled = CoreConfig.ReliefMinFilled;
        public int ReliefMinT = CoreConfig.ReliefMinTraysWithoutClear;
        public int OpenMaxFilled = CoreConfig.OpenMaxFilled;
        public WeightProfile Open = Difficulty.Profile(DifficultyMode.Open);
        public WeightProfile Normal = Difficulty.Profile(DifficultyMode.Normal);
        public WeightProfile Relief = Difficulty.Profile(DifficultyMode.Relief);

        public DifficultyMode Evaluate(int filled, int t)
        {
            if (AlwaysNormal) return DifficultyMode.Normal;
            if (filled >= ReliefMinFilled || t >= ReliefMinT) return DifficultyMode.Relief;
            if (filled <= OpenMaxFilled && t == 0) return DifficultyMode.Open;
            return DifficultyMode.Normal;
        }

        public WeightProfile Profile(DifficultyMode m)
        {
            switch (m)
            {
                case DifficultyMode.Open: return Open;
                case DifficultyMode.Relief: return Relief;
                default: return Normal;
            }
        }

        private static WeightProfile With(WeightProfile p, int? s = null, int? m = null, int? l = null, int? fit = null, int? minFit = null, int? maxL = null)
            => new WeightProfile(s ?? p.SmallMul, m ?? p.MediumMul, l ?? p.LargeMul, fit ?? p.FitMul, minFit ?? p.MinFitting, maxL ?? p.MaxLarge);

        public static SimDifficulty Baseline() => new SimDifficulty { Name = "baseline", Description = "GDD v0.1 / CoreConfig (zorluk acik)" };
        public static SimDifficulty Off() => new SimDifficulty { Name = "off", Description = "Zorluk kapali: her tepsi NORMAL", AlwaysNormal = true };

        /// <summary>GDD §10 ayar yonleri ve birkac ek varyant.</summary>
        public static SimDifficulty[] SweepVariants()
        {
            var b = Baseline();
            return new[]
            {
                Baseline(),
                Off(),
                new SimDifficulty { Name = "relief28", Description = "RAHATLATMA esigi dolu >= 28 (GDD §10: S6 dusukse)", ReliefMinFilled = 28 },
                new SimDifficulty { Name = "relief24", Description = "RAHATLATMA esigi dolu >= 24", ReliefMinFilled = 24 },
                new SimDifficulty { Name = "reliefT1", Description = "RAHATLATMA T >= 1", ReliefMinT = 1 },
                new SimDifficulty { Name = "reliefFit2", Description = "RAHATLATMA sigma carpani 3 -> 2 (GDD §10: S6/S9 yuksekse)", Relief = With(b.Relief, fit: 2) },
                new SimDifficulty { Name = "reliefFit5", Description = "RAHATLATMA sigma carpani 3 -> 5", Relief = With(b.Relief, fit: 5) },
                new SimDifficulty { Name = "openL15", Description = "ACIK B carpani 13 -> 15 (GDD §10: S6/S9 yuksekse)", Open = With(b.Open, l: 15) },
                new SimDifficulty { Name = "normalL7", Description = "NORMAL B carpani 10 -> 7 (B agirligi dusuk; GDD §10: S6 dusukse)", Normal = With(b.Normal, l: 7) },
                new SimDifficulty { Name = "allL7", Description = "NORMAL B 10 -> 7, ACIK B 13 -> 10 (B agirligi genel dusuk)", Normal = With(b.Normal, l: 7), Open = With(b.Open, l: 10) },
                new SimDifficulty { Name = "combo", Description = "relief28 + NORMAL B 7 + RAHATLATMA sigma 3 -> 4", ReliefMinFilled = 28, Normal = With(b.Normal, l: 7), Relief = With(b.Relief, fit: 4) },
            };
        }
    }
}
