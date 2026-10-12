using System;

namespace Blockbloom.Core
{
    /// <summary>Tek hamlenin puan dokumu (GDD §6).</summary>
    public readonly struct ScoreBreakdown
    {
        public ScoreBreakdown(int placementPoints, int linePoints, int perfectClearBonus, int comboAfter)
        {
            PlacementPoints = placementPoints;
            LinePoints = linePoints;
            PerfectClearBonus = perfectClearBonus;
            ComboAfter = comboAfter;
        }

        public int PlacementPoints { get; }
        public int LinePoints { get; }
        public int PerfectClearBonus { get; }
        public int Total => PlacementPoints + LinePoints + PerfectClearBonus;
        public int ComboAfter { get; }
    }

    /// <summary>
    /// Skor ve kombo (GDD §6.2). K = 0 baslar; patlatan hamlede once K+1, sonra 10 x n^2 x K;
    /// patlatmayan hamlede K = 0. Tepsi yenilemesi K'yi etkilemez (yenileme hamle degildir).
    /// </summary>
    public sealed class ScoreKeeper
    {
        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }

        public ScoreKeeper()
        {
        }

        internal ScoreKeeper(int score, int combo, int maxCombo)
        {
            if (score < 0 || combo < 0 || maxCombo < combo) throw new ArgumentException("Gecersiz skor durumu.");
            Score = score;
            Combo = combo;
            MaxCombo = maxCombo;
        }

        /// <summary>Hamle puanini hesaplar ve durumu gunceller.</summary>
        public ScoreBreakdown Apply(PlacementResult r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));
            int placement = r.CellsPlaced * CoreConfig.PointsPerCell;
            int n = r.LineCount;
            int linePoints = 0;
            if (n > 0)
            {
                Combo++;
                linePoints = CoreConfig.LineBasePoints * n * n * Combo;
                if (Combo > MaxCombo) MaxCombo = Combo;
            }
            else
            {
                Combo = 0;
            }
            int bonus = r.BoardCleared ? CoreConfig.PerfectClearBonus : 0;
            var b = new ScoreBreakdown(placement, linePoints, bonus, Combo);
            Score += b.Total;
            return b;
        }
    }
}
