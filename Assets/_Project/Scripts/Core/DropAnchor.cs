using System;

namespace Blockbloom.Core
{
    /// <summary>
    /// Surukle-birak capa secimi (GDD §8 "Capa hesabi"). Saf mantik; Unity katmani her karede cagirir, bellek ayirmaz.
    /// (fx, fy): suruklenen parcanin sol-ust hucre merkezinin tahtaya gore konumu, hucre biriminde (x saga, y asagi).
    /// Adaylar (floor/ceil) x (floor/ceil); CanPlace dogru olanlardan (fx, fy)'ye en yakini; esitlikte once kucuk y,
    /// sonra kucuk x. En yakinin uzakligi maxDistance'tan buyukse gecerli capa yok.
    /// </summary>
    public static class DropAnchor
    {
        // Tahtadan cok uzak parmak konumlarinda int tasmasini onler; sonuc degismez (hicbir aday tahtada olmaz).
        private const float Clamp = 64f;

        public static bool TryFind(Grid grid, PieceDef piece, float fx, float fy, float maxDistance, out Cell anchor)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (piece == null) throw new ArgumentNullException(nameof(piece));
            anchor = default(Cell);
            if (float.IsNaN(fx) || float.IsNaN(fy)) return false;
            if (fx < -Clamp) fx = -Clamp; else if (fx > Clamp) fx = Clamp;
            if (fy < -Clamp) fy = -Clamp; else if (fy > Clamp) fy = Clamp;

            int x0 = (int)Math.Floor(fx), x1 = (int)Math.Ceiling(fx);
            int y0 = (int)Math.Floor(fy), y1 = (int)Math.Ceiling(fy);

            bool found = false;
            float best = 0f;
            int bx = 0, by = 0;
            // Siralama y artan, sonra x artan; esit uzaklikta ilk bulunan korunur -> "once kucuk y, sonra kucuk x".
            Consider(grid, piece, fx, fy, x0, y0, ref found, ref best, ref bx, ref by);
            Consider(grid, piece, fx, fy, x1, y0, ref found, ref best, ref bx, ref by);
            Consider(grid, piece, fx, fy, x0, y1, ref found, ref best, ref bx, ref by);
            Consider(grid, piece, fx, fy, x1, y1, ref found, ref best, ref bx, ref by);

            if (!found || best > maxDistance * maxDistance) return false;
            anchor = new Cell(bx, by);
            return true;
        }

        private static void Consider(Grid g, PieceDef p, float fx, float fy, int ax, int ay,
            ref bool found, ref float best, ref int bx, ref int by)
        {
            if (!g.CanPlace(p, ax, ay)) return;
            float dx = ax - fx, dy = ay - fy;
            float d2 = dx * dx + dy * dy;
            if (!found || d2 < best)
            {
                found = true;
                best = d2;
                bx = ax;
                by = ay;
            }
        }
    }
}
