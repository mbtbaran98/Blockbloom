using System;
using System.Collections.Generic;

namespace Blockbloom.Core
{
    /// <summary>
    /// 8x8 tahta. Doluluk ulong bit tahtasinda (indeks y*8+x), renkler ayri dizide (yalniz gorsel).
    /// CanPlace / FitsAnywhere / PreviewLines bellek ayirmaz.
    /// </summary>
    public sealed class Grid
    {
        public const int Size = CoreConfig.BoardSize;

        private ulong _bits;
        private readonly byte[] _colors = new byte[CoreConfig.CellCount];

        /// <summary>Dolu hucre bit tahtasi, indeks y*8+x.</summary>
        public ulong Bits => _bits;

        public int FilledCount => BitUtil.PopCount(_bits);

        public bool IsEmpty => _bits == 0UL;

        /// <summary>0 = bos, 1..6 = renk. Tahta disi -> ArgumentOutOfRangeException.</summary>
        public byte ColorAt(int x, int y)
        {
            if ((uint)x >= Size || (uint)y >= Size) throw new ArgumentOutOfRangeException(nameof(x));
            return _colors[y * Size + x];
        }

        public bool IsFilled(int x, int y)
        {
            if ((uint)x >= Size || (uint)y >= Size) throw new ArgumentOutOfRangeException(nameof(x));
            return (_bits & (1UL << (y * Size + x))) != 0;
        }

        /// <summary>GDD §3.4: parcanin her hucresi tahta icinde ve bos.</summary>
        public bool CanPlace(PieceDef p, int ax, int ay)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (!p.InBounds(ax, ay)) return false;
            return (_bits & p.MaskAtUnchecked(ax, ay)) == 0UL;
        }

        public bool FitsAnywhere(PieceDef p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            ulong bits = _bits;
            ulong[] masks = p.AnchorMasks;
            for (int i = 0; i < masks.Length; i++)
            {
                if ((bits & masks[i]) == 0UL) return true;
            }
            return false;
        }

        /// <summary>Gecerli capalar, y sonra x artan sirada.</summary>
        public IEnumerable<Cell> ValidAnchors(PieceDef p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            return ValidAnchorsIterator(p);
        }

        private IEnumerable<Cell> ValidAnchorsIterator(PieceDef p)
        {
            ulong[] masks = p.AnchorMasks;
            Cell[] cells = p.AnchorCells;
            for (int i = 0; i < masks.Length; i++)
            {
                if ((_bits & masks[i]) == 0UL) yield return cells[i];
            }
        }

        /// <summary>
        /// Bu capaya birakilirsa patlayacak satir/sutunlar. Tahtayi degistirmez, bellek ayirmaz.
        /// CanPlace yanlissa bos onizleme (LineCount = 0) dondurur.
        /// </summary>
        public LinePreview PreviewLines(PieceDef p, int ax, int ay)
        {
            if (!CanPlace(p, ax, ay)) return default(LinePreview);
            ulong after = _bits | p.MaskAtUnchecked(ax, ay);
            return new LinePreview(BitUtil.FullRows(after), BitUtil.FullCols(after));
        }

        /// <summary>
        /// GDD §3.5 adim 1-3: hucreleri doldurur, dolu satir ve sutunlari ayni anda bosaltir (kesisim bir kez).
        /// CanPlace yanlissa InvalidOperationException. color 1..6 olmali.
        /// </summary>
        public PlacementResult Place(PieceDef p, int ax, int ay, byte color)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            if (color < 1 || color > CoreConfig.ColorCount) throw new ArgumentOutOfRangeException(nameof(color), "Renk 1..6 olmali.");
            if (!CanPlace(p, ax, ay)) throw new InvalidOperationException("Parca " + p.Id + " (" + ax + "," + ay + ") capasina yerlestirilemez.");

            ulong mask = p.MaskAtUnchecked(ax, ay);
            _bits |= mask;
            IReadOnlyList<Cell> cells = p.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell c = cells[i];
                _colors[(ay + c.Y) * Size + ax + c.X] = color;
            }

            byte rows = BitUtil.FullRows(_bits);
            byte cols = BitUtil.FullCols(_bits);
            int lineCount = BitUtil.PopCount(rows) + BitUtil.PopCount(cols);
            Cell[] cleared;
            if (lineCount == 0)
            {
                cleared = Array.Empty<Cell>();
            }
            else
            {
                ulong clearMask = BitUtil.ClearMask(rows, cols);
                cleared = new Cell[BitUtil.PopCount(clearMask)];
                int k = 0;
                for (int i = 0; i < CoreConfig.CellCount; i++)
                {
                    if ((clearMask & (1UL << i)) != 0)
                    {
                        cleared[k++] = new Cell(i % Size, i / Size);
                        _colors[i] = 0;
                    }
                }
                _bits &= ~clearMask;
            }

            bool boardCleared = lineCount > 0 && _bits == 0UL;
            return new PlacementResult(p.CellCount, rows, cols, cleared, boardCleared);
        }

        public Grid Clone()
        {
            var g = new Grid();
            g._bits = _bits;
            Buffer.BlockCopy(_colors, 0, g._colors, 0, _colors.Length);
            return g;
        }

        /// <summary>Renkleri hedef diziye kopyalar (64 bayt, indeks y*8+x).</summary>
        public void CopyColorsTo(byte[] destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (destination.Length < CoreConfig.CellCount) throw new ArgumentException("Hedef en az 64 bayt olmali.", nameof(destination));
            Buffer.BlockCopy(_colors, 0, destination, 0, CoreConfig.CellCount);
        }

        /// <summary>
        /// Ham veriden tahta (anlik goruntu, testler). Dolu bit &lt;=&gt; renk 1..6 tutarliligi denetlenir.
        /// Not: dolu satir/sutun iceren (oyunda olusamayacak) tahtalara izin verir; patlatma yalnizca Place icinde olur.
        /// </summary>
        public static Grid FromRaw(ulong bits, byte[] colors)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            if (colors.Length != CoreConfig.CellCount) throw new ArgumentException("Renk dizisi 64 bayt olmali.", nameof(colors));
            var g = new Grid();
            for (int i = 0; i < CoreConfig.CellCount; i++)
            {
                bool filled = (bits & (1UL << i)) != 0;
                byte c = colors[i];
                if (filled != (c != 0) || c > CoreConfig.ColorCount)
                    throw new ArgumentException("Bit tahtasi ile renkler tutarsiz (hucre " + i + ").", nameof(colors));
                g._colors[i] = c;
            }
            g._bits = bits;
            return g;
        }

        /// <summary>Tum dolu hucreler tek renkte (testler ve araclar icin).</summary>
        public static Grid FromBits(ulong bits, byte color = 1)
        {
            if (color < 1 || color > CoreConfig.ColorCount) throw new ArgumentOutOfRangeException(nameof(color));
            var colors = new byte[CoreConfig.CellCount];
            for (int i = 0; i < CoreConfig.CellCount; i++)
            {
                if ((bits & (1UL << i)) != 0) colors[i] = color;
            }
            return FromRaw(bits, colors);
        }
    }
}
