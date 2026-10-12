using System;

namespace Blockbloom.Core
{
    /// <summary>Gorunmez zorluk girdisi T (GDD §5.2): art arda patlatmasiz tepsi sayisi.</summary>
    public sealed class DifficultyState
    {
        public int TraysWithoutClear { get; private set; }

        public DifficultyState()
        {
        }

        internal DifficultyState(int traysWithoutClear)
        {
            if (traysWithoutClear < 0) throw new ArgumentOutOfRangeException(nameof(traysWithoutClear));
            TraysWithoutClear = traysWithoutClear;
        }

        /// <summary>Tepsinin 3 parcasi da yerlestirildiginde, yenilemeden once cagrilir.</summary>
        public void OnTrayCompleted(bool anyClearInTray)
        {
            TraysWithoutClear = anyClearInTray ? 0 : TraysWithoutClear + 1;
        }
    }

    /// <summary>Uretim agirlik profili (GDD §5.3). Carpanlar onda bir, tamsayi.</summary>
    public readonly struct WeightProfile
    {
        public WeightProfile(int smallMul, int mediumMul, int largeMul, int fitMul, int minFitting, int maxLarge)
        {
            if (smallMul < 0 || mediumMul < 0 || largeMul < 0 || fitMul < 1)
                throw new ArgumentOutOfRangeException(nameof(fitMul), "Carpanlar >= 0, fitMul >= 1 olmali.");
            if (minFitting < 0 || minFitting > CoreConfig.TraySlotCount)
                throw new ArgumentOutOfRangeException(nameof(minFitting));
            if (maxLarge < 0 || maxLarge > CoreConfig.TraySlotCount)
                throw new ArgumentOutOfRangeException(nameof(maxLarge));
            SmallMul = smallMul;
            MediumMul = mediumMul;
            LargeMul = largeMul;
            FitMul = fitMul;
            MinFitting = minFitting;
            MaxLarge = maxLarge;
        }

        public int SmallMul { get; }
        public int MediumMul { get; }
        public int LargeMul { get; }
        /// <summary>Tahtaya sigan parcanin agirlik carpani (sigmayan x1).</summary>
        public int FitMul { get; }
        /// <summary>Tepside en az bu kadar parca tahtaya sigmali.</summary>
        public int MinFitting { get; }
        /// <summary>Tepside en fazla bu kadar B (Large) parca.</summary>
        public int MaxLarge { get; }

        public int ClassMul(SizeClass size)
        {
            switch (size)
            {
                case SizeClass.Small: return SmallMul;
                case SizeClass.Medium: return MediumMul;
                default: return LargeMul;
            }
        }
    }

    /// <summary>GDD §5.3 mod secimi ve profilleri.</summary>
    public static class Difficulty
    {
        /// <summary>Kurallar sirayla: RAHATLATMA (dolu &gt;= 32 veya T &gt;= 2), ACIK (dolu &lt;= 15 ve T = 0), aksi NORMAL.</summary>
        public static DifficultyMode Evaluate(int filledCount, DifficultyState s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            int t = s.TraysWithoutClear;
            if (filledCount >= CoreConfig.ReliefMinFilled || t >= CoreConfig.ReliefMinTraysWithoutClear)
                return DifficultyMode.Relief;
            if (filledCount <= CoreConfig.OpenMaxFilled && t == 0)
                return DifficultyMode.Open;
            return DifficultyMode.Normal;
        }

        public static WeightProfile Profile(DifficultyMode m)
        {
            switch (m)
            {
                case DifficultyMode.Relief:
                    return new WeightProfile(CoreConfig.ReliefSmallMul, CoreConfig.ReliefMediumMul, CoreConfig.ReliefLargeMul,
                        CoreConfig.ReliefFitMul, CoreConfig.ReliefMinFitting, CoreConfig.ReliefMaxLarge);
                case DifficultyMode.Open:
                    return new WeightProfile(CoreConfig.OpenSmallMul, CoreConfig.OpenMediumMul, CoreConfig.OpenLargeMul,
                        CoreConfig.OpenFitMul, CoreConfig.OpenMinFitting, CoreConfig.OpenMaxLarge);
                case DifficultyMode.Normal:
                    return new WeightProfile(CoreConfig.NormalSmallMul, CoreConfig.NormalMediumMul, CoreConfig.NormalLargeMul,
                        CoreConfig.NormalFitMul, CoreConfig.NormalMinFitting, CoreConfig.NormalMaxLarge);
                default:
                    throw new ArgumentOutOfRangeException(nameof(m));
            }
        }
    }
}
