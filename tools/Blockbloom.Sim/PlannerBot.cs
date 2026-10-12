using System.Collections.Generic;
using Blockbloom.Core;

namespace Blockbloom.Sim
{
    /// <summary>
    /// GDD disi, yalniz bilgi amacli "planlayici" bot: tepside kalan parcalarin TUM sira ve capa kombinasyonlarini
    /// (kombo dahil puanla) arar; yaprak degeri = toplanan puan + hareketlilik (sigan katalog agirligi, 0..124)
    /// - 8 x tek hucrelik delik; yerlestirilemeyen her parca -1000. (bits, kalan yuvalar, kombo) uzerinden bellekli.
    /// Becerili bir oyuncunun yaklasik ust sinirini gormek icindir; S kriterlerine girmez. Deterministik.
    /// </summary>
    public sealed class PlannerBot : IBot
    {
        private const ulong Col0 = 0x0101010101010101UL;
        private const ulong Col7 = 0x8080808080808080UL;
        private const ulong Row0 = 0xFFUL;
        private const ulong Row7 = 0xFF00000000000000UL;

        private readonly Dictionary<(ulong, int, int), (long value, int slot, int anchor)> _memo = new Dictionary<(ulong, int, int), (long, int, int)>();
        private readonly Dictionary<ulong, long> _leaf = new Dictionary<ulong, long>();
        private readonly PieceDef[] _defs = new PieceDef[Tray.SlotCount];

        public int Choose(ISimGame g, List<(int slot, Cell anchor)> moves, out int predictedTotal)
        {
            int rem = 0;
            for (int s = 0; s < Tray.SlotCount; s++)
            {
                TrayPiece? p = g.Slot(s);
                _defs[s] = p.HasValue ? p.Value.Def : null;
                if (p.HasValue) rem |= 1 << s;
            }
            _memo.Clear();
            if (_leaf.Count > 2_000_000) _leaf.Clear();
            (long _, int bs, int ba) = Search(g.Bits, rem, g.Combo);
            predictedTotal = -1;
            Cell[] cells = _defs[bs].AnchorCells;
            Cell target = cells[ba];
            for (int i = 0; i < moves.Count; i++)
                if (moves[i].slot == bs && moves[i].anchor.X == target.X && moves[i].anchor.Y == target.Y) return i;
            return 0; // ulasilmamali
        }

        private (long value, int slot, int anchor) Search(ulong bits, int rem, int combo)
        {
            if (rem == 0) return (Leaf(bits), -1, -1);
            var key = (bits, rem, combo);
            if (_memo.TryGetValue(key, out var hit)) return hit;

            long best = long.MinValue;
            int bs = -1, ba = -1;
            for (int s = 0; s < Tray.SlotCount; s++)
            {
                if ((rem & (1 << s)) == 0) continue;
                PieceDef def = _defs[s];
                ulong[] masks = def.AnchorMasks;
                for (int i = 0; i < masks.Length; i++)
                {
                    if ((bits & masks[i]) != 0UL) continue;
                    ulong after = bits | masks[i];
                    byte rows = BitUtil.FullRows(after);
                    byte cols = BitUtil.FullCols(after);
                    int n = BitUtil.PopCount(rows) + BitUtil.PopCount(cols);
                    ulong next = n > 0 ? after & ~BitUtil.ClearMask(rows, cols) : after;
                    int nc = n > 0 ? combo + 1 : 0;
                    long gain = def.CellCount * CoreConfig.PointsPerCell;
                    if (n > 0)
                    {
                        gain += CoreConfig.LineBasePoints * n * n * nc;
                        if (next == 0UL) gain += CoreConfig.PerfectClearBonus;
                    }
                    long v = gain + Search(next, rem & ~(1 << s), nc).value;
                    if (v > best) { best = v; bs = s; ba = i; }
                }
            }
            if (bs < 0)
            {
                // Kalan parcalarin hicbiri sigmiyor: tepsi tamamlanamaz -> oyun biter.
                int left = 0;
                for (int s = 0; s < Tray.SlotCount; s++) if ((rem & (1 << s)) != 0) left++;
                best = Leaf(bits) - 1000L * left;
            }
            var r = (best, bs, ba);
            _memo[key] = r;
            return r;
        }

        private long Leaf(ulong bits)
        {
            if (_leaf.TryGetValue(bits, out long v)) return v;
            int mobility = 0;
            IReadOnlyList<PieceDef> cat = PieceCatalog.All;
            for (int k = 0; k < cat.Count; k++)
            {
                ulong[] masks = cat[k].AnchorMasks;
                for (int i = 0; i < masks.Length; i++)
                    if ((bits & masks[i]) == 0UL) { mobility += cat[k].BaseWeight; break; }
            }
            ulong F = bits;
            ulong holes = ~F & ((F << 1) | Col0) & ((F >> 1) | Col7) & ((F << 8) | Row0) & ((F >> 8) | Row7);
            v = mobility - 8L * BitUtil.PopCount(holes);
            _leaf[bits] = v;
            return v;
        }
    }
}
