using System.Collections.Generic;

namespace Blockbloom.Core
{
    /// <summary>Grid.Place sonucu (GDD §3.5 adim 1-3). Degismez.</summary>
    public sealed class PlacementResult
    {
        private readonly byte _rows;
        private readonly byte _cols;

        internal PlacementResult(int cellsPlaced, byte rowMask, byte colMask, Cell[] clearedCells, bool boardCleared)
        {
            CellsPlaced = cellsPlaced;
            _rows = rowMask;
            _cols = colMask;
            ClearedCells = clearedCells.Length == 0 ? (IReadOnlyList<Cell>)clearedCells : System.Array.AsReadOnly(clearedCells);
            BoardCleared = boardCleared;
        }

        public int CellsPlaced { get; }
        public IReadOnlyList<int> ClearedRows => BitUtil.IndexLists[_rows];
        public IReadOnlyList<int> ClearedCols => BitUtil.IndexLists[_cols];
        public byte ClearedRowMask => _rows;
        public byte ClearedColMask => _cols;

        /// <summary>|R| + |C| (kesisim cizgi sayisini etkilemez).</summary>
        public int LineCount => BitUtil.PopCount(_rows) + BitUtil.PopCount(_cols);

        /// <summary>8|R| + 8|C| - |R||C| (kesisim bir kez).</summary>
        public int CellsCleared => ClearedCells.Count;

        /// <summary>Bosaltilan hucreler, indeks (y sonra x) artan sirada.</summary>
        public IReadOnlyList<Cell> ClearedCells { get; }

        /// <summary>Patlatma sonucu tahta tamamen bosaldi (tam temizlik).</summary>
        public bool BoardCleared { get; }
    }
}
