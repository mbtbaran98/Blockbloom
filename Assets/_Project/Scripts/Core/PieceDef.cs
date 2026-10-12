using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Blockbloom.Core
{
    /// <summary>
    /// Sabit parca sekli (GDD §4 katalog satiri). Degismez. Tum gecerli capalardaki bit maskeleri
    /// yapim aninda bir kez hesaplanir; sicak yollarda (CanPlace, FitsAnywhere, uretim) ayirma yapilmaz.
    /// </summary>
    public sealed class PieceDef
    {
        private const int N = CoreConfig.BoardSize;

        // Indeks ay*8+ax; tahtaya sigmayan capalar icin 0 (kullanilmaz; sinir denetimi ayrica yapilir).
        private readonly ulong[] _masks = new ulong[CoreConfig.CellCount];
        // Yalniz tahta icindeki capalar, y sonra x artan sirada.
        private readonly ulong[] _anchorMasks;
        private readonly Cell[] _anchorCells;

        public string Id { get; }
        public IReadOnlyList<Cell> Cells { get; }
        public int Width { get; }
        public int Height { get; }
        public int CellCount { get; }
        public int BaseWeight { get; }
        public SizeClass Size { get; }

        /// <param name="id">Kalici kimlik (anlik goruntu buna baglidir).</param>
        /// <param name="baseWeight">Tamsayi taban agirlik (&gt;= 0).</param>
        /// <param name="cells">Normalize hucreler: min x = 0, min y = 0; tekrarsiz; 8x8 icine sigar.</param>
        public PieceDef(string id, int baseWeight, params Cell[] cells)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Parca kimligi bos olamaz.", nameof(id));
            if (baseWeight < 0) throw new ArgumentOutOfRangeException(nameof(baseWeight));
            if (cells == null || cells.Length == 0) throw new ArgumentException("Parca en az bir hucre icermeli.", nameof(cells));

            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            ulong shape = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                Cell c = cells[i];
                if (c.X < 0 || c.Y < 0 || c.X >= N || c.Y >= N)
                    throw new ArgumentException("Hucre 0..7 araliginda olmali: " + c, nameof(cells));
                ulong bit = 1UL << (c.Y * N + c.X);
                if ((shape & bit) != 0) throw new ArgumentException("Tekrarlanan hucre: " + c, nameof(cells));
                shape |= bit;
                if (c.X < minX) minX = c.X;
                if (c.Y < minY) minY = c.Y;
                if (c.X > maxX) maxX = c.X;
                if (c.Y > maxY) maxY = c.Y;
            }
            if (minX != 0 || minY != 0) throw new ArgumentException("Hucreler normalize olmali (min x = 0, min y = 0).", nameof(cells));

            Id = id;
            BaseWeight = baseWeight;
            Cells = new ReadOnlyCollection<Cell>((Cell[])cells.Clone());
            Width = maxX + 1;
            Height = maxY + 1;
            CellCount = cells.Length;
            Size = CellCount <= CoreConfig.SmallMaxCells ? SizeClass.Small
                 : CellCount == CoreConfig.MediumCells ? SizeClass.Medium
                 : SizeClass.Large;

            int anchorCount = (N - Width + 1) * (N - Height + 1);
            _anchorMasks = new ulong[anchorCount];
            _anchorCells = new Cell[anchorCount];
            int k = 0;
            for (int ay = 0; ay <= N - Height; ay++)
            {
                for (int ax = 0; ax <= N - Width; ax++)
                {
                    // shape (0,0)'a gore; kaydirma tasmaz cunku parca tahta icinde kalir.
                    ulong m = shape << (ay * N + ax);
                    _masks[ay * N + ax] = m;
                    _anchorMasks[k] = m;
                    _anchorCells[k] = new Cell(ax, ay);
                    k++;
                }
            }
        }

        /// <summary>Capa (ax, ay) icin bit maskesi. Parca tahtaya sigmayacaksa ArgumentOutOfRangeException.</summary>
        public ulong MaskAt(int ax, int ay)
        {
            if (!InBounds(ax, ay)) throw new ArgumentOutOfRangeException(nameof(ax), "Capa parcayi tahta disina tasiyor.");
            return _masks[ay * N + ax];
        }

        /// <summary>Parca bu capada tamamen tahta icinde mi (doluluk denetlenmez).</summary>
        public bool InBounds(int ax, int ay)
        {
            return ax >= 0 && ay >= 0 && ax <= N - Width && ay <= N - Height;
        }

        internal ulong MaskAtUnchecked(int ax, int ay) => _masks[ay * N + ax];
        internal ulong[] AnchorMasks => _anchorMasks;
        internal Cell[] AnchorCells => _anchorCells;

        public override string ToString() => Id;
    }
}
