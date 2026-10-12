using System.Collections.Generic;

namespace Blockbloom.Core
{
    /// <summary>
    /// Birakma onizlemesinde patlayacak cizgiler (GDD §8). Rows/Cols onbellekli salt okunur listelerdir;
    /// olusturmak bellek ayirmaz. RowMask/ColMask: bit i = satir/sutun i.
    /// </summary>
    public readonly struct LinePreview
    {
        private readonly byte _rows;
        private readonly byte _cols;

        public LinePreview(byte rowMask, byte colMask)
        {
            _rows = rowMask;
            _cols = colMask;
        }

        public byte RowMask => _rows;
        public byte ColMask => _cols;
        public IReadOnlyList<int> Rows => BitUtil.IndexLists[_rows];
        public IReadOnlyList<int> Cols => BitUtil.IndexLists[_cols];
        public int LineCount => BitUtil.PopCount(_rows) + BitUtil.PopCount(_cols);

        /// <summary>Hucre (x, y) patlayacak bir cizgide mi (vurgu icin).</summary>
        public bool Contains(int x, int y)
        {
            return ((_rows >> y) & 1) != 0 || ((_cols >> x) & 1) != 0;
        }
    }
}
