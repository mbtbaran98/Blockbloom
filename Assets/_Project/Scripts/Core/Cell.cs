using System;

namespace Blockbloom.Core
{
    /// <summary>Izgara hucresi. X saga (0-7), Y asagi (0-7). Indeks = Y*8 + X.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public int X { get; }
        public int Y { get; }

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>Tahta indeksi (Y*8 + X). Yalniz tahta icindeki hucreler icin anlamli.</summary>
        public int Index => Y * CoreConfig.BoardSize + X;

        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => (X * 397) ^ Y;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);
        public override string ToString() => "(" + X + "," + Y + ")";
    }
}
