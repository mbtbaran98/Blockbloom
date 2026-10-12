using System.Collections.Generic;
using Blockbloom.Core;

namespace Blockbloom.Sim
{
    public enum BotKind { Random, Greedy, Smart, Planner }

    public interface IBot
    {
        /// <summary>Yasal hamle listesinden (yuva, y, x artan) bir indeks secer. predictedTotal: botun tahmin ettigi
        /// anlik puan (acgozlu), tahmini yoksa -1.</summary>
        int Choose(ISimGame g, List<(int slot, Cell anchor)> moves, out int predictedTotal);
    }

    /// <summary>GDD §10: LegalMoves icinden tekduze secim; kendi Pcg32(seed + 1_000_000) ornegi (oyun RNG'sinden ayri).</summary>
    public sealed class RandomBot : IBot
    {
        public const ulong SeedOffset = 1_000_000UL;
        private readonly Pcg32 _rng;

        public RandomBot(ulong gameSeed) { _rng = new Pcg32(unchecked(gameSeed + SeedOffset)); }

        public int Choose(ISimGame g, List<(int slot, Cell anchor)> moves, out int predictedTotal)
        {
            predictedTotal = -1;
            return _rng.NextInt(moves.Count);
        }
    }

    /// <summary>
    /// GDD §10 acgozlu bot: en yuksek anlik ScoreBreakdown.Total; esitlikte hamle sonrasi FilledCount kucuk olan,
    /// sonra yuva, y, x kucuk olan (LegalMoves sirasi zaten bu oldugundan ilk en iyi korunur). Deterministik.
    /// Puan tahmini bit tahtasi uzerinde Core kurallarinin aynisidir; gercek sonucla esitligi kosucuda denetlenir (S4).
    /// </summary>
    public sealed class GreedyBot : IBot
    {
        public int Choose(ISimGame g, List<(int slot, Cell anchor)> moves, out int predictedTotal)
        {
            ulong bits = g.Bits;
            int nextCombo = g.Combo + 1;
            int best = -1, bestTotal = int.MinValue, bestFilled = int.MaxValue;
            for (int i = 0; i < moves.Count; i++)
            {
                (int s, Cell a) = moves[i];
                PieceDef def = g.Slot(s).Value.Def;
                ulong after = bits | def.MaskAt(a.X, a.Y);
                byte rows = BitUtil.FullRows(after);
                byte cols = BitUtil.FullCols(after);
                int n = BitUtil.PopCount(rows) + BitUtil.PopCount(cols);
                ulong rem = n > 0 ? after & ~BitUtil.ClearMask(rows, cols) : after;
                int total = def.CellCount * CoreConfig.PointsPerCell;
                if (n > 0)
                {
                    total += CoreConfig.LineBasePoints * n * n * nextCombo;
                    if (rem == 0UL) total += CoreConfig.PerfectClearBonus;
                }
                int filled = BitUtil.PopCount(rem);
                if (total > bestTotal || (total == bestTotal && filled < bestFilled))
                {
                    best = i;
                    bestTotal = total;
                    bestFilled = filled;
                }
            }
            predictedTotal = bestTotal;
            return best;
        }
    }

    /// <summary>
    /// GDD disi, yalniz bilgi amacli "sezgisel" bot: becerili bir oyuncuya yaklasik ust sinir vermek icin.
    /// Deger = anlik puan + hareketlilik (hamle sonrasi tahtaya sigan katalog parcalarinin taban agirlik toplami, 0..124)
    ///         - 8 x tek hucrelik delik - 500 x tepside kalip artik sigmayan parca. Esitlikte LegalMoves sirasi. Deterministik.
    /// </summary>
    public sealed class SmartBot : IBot
    {
        private const ulong Col0 = 0x0101010101010101UL;
        private const ulong Col7 = 0x8080808080808080UL;
        private const ulong Row0 = 0xFFUL;
        private const ulong Row7 = 0xFF00000000000000UL;

        public int Choose(ISimGame g, List<(int slot, Cell anchor)> moves, out int predictedTotal)
        {
            ulong bits = g.Bits;
            int nextCombo = g.Combo + 1;
            int best = -1, bestTotal = 0;
            long bestValue = long.MinValue;
            IReadOnlyList<PieceDef> cat = PieceCatalog.All;
            for (int i = 0; i < moves.Count; i++)
            {
                (int s, Cell a) = moves[i];
                PieceDef def = g.Slot(s).Value.Def;
                ulong after = bits | def.MaskAt(a.X, a.Y);
                byte rows = BitUtil.FullRows(after);
                byte cols = BitUtil.FullCols(after);
                int n = BitUtil.PopCount(rows) + BitUtil.PopCount(cols);
                ulong rem = n > 0 ? after & ~BitUtil.ClearMask(rows, cols) : after;
                int total = def.CellCount * CoreConfig.PointsPerCell;
                if (n > 0)
                {
                    total += CoreConfig.LineBasePoints * n * n * nextCombo;
                    if (rem == 0UL) total += CoreConfig.PerfectClearBonus;
                }

                int stuck = 0;
                for (int k = 0; k < Tray.SlotCount; k++)
                {
                    if (k == s) continue;
                    TrayPiece? o = g.Slot(k);
                    if (o.HasValue && !Fits(o.Value.Def, rem)) stuck++;
                }
                int mobility = 0;
                for (int k = 0; k < cat.Count; k++) if (Fits(cat[k], rem)) mobility += cat[k].BaseWeight;
                ulong F = rem;
                ulong holes = ~F & ((F << 1) | Col0) & ((F >> 1) | Col7) & ((F << 8) | Row0) & ((F >> 8) | Row7);
                long value = total + mobility - 8L * BitUtil.PopCount(holes) - 500L * stuck;
                if (value > bestValue)
                {
                    bestValue = value;
                    best = i;
                    bestTotal = total;
                }
            }
            predictedTotal = bestTotal;
            return best;
        }

        private static bool Fits(PieceDef p, ulong bits)
        {
            ulong[] masks = p.AnchorMasks;
            for (int i = 0; i < masks.Length; i++) if ((bits & masks[i]) == 0UL) return true;
            return false;
        }
    }
}
