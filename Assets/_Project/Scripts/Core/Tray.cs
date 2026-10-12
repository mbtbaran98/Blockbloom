using System;

namespace Blockbloom.Core
{
    /// <summary>Tepsideki parca ornegi: tanim + renk (1..6, yalniz gorsel).</summary>
    public readonly struct TrayPiece
    {
        public TrayPiece(PieceDef def, byte color)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
            if (color < 1 || color > CoreConfig.ColorCount) throw new ArgumentOutOfRangeException(nameof(color));
            Color = color;
        }

        public PieceDef Def { get; }
        public byte Color { get; }
    }

    /// <summary>3 yuvali tepsi (GDD §3.2). Yerlestirilen yuva bos kalir; diger parcalar kaymaz.</summary>
    public sealed class Tray
    {
        public const int SlotCount = CoreConfig.TraySlotCount;

        private readonly TrayPiece?[] _slots = new TrayPiece?[SlotCount];

        /// <summary>Yuvadaki parca; bossa null. Gecersiz yuva -> ArgumentOutOfRangeException.</summary>
        public TrayPiece? this[int slot]
        {
            get
            {
                if ((uint)slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
                return _slots[slot];
            }
        }

        public int RemainingCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < SlotCount; i++) if (_slots[i].HasValue) n++;
                return n;
            }
        }

        public bool IsEmpty => RemainingCount == 0;

        /// <summary>Yuvayi bosaltir ve parcayi dondurur. Bos yuva -> InvalidOperationException.</summary>
        public TrayPiece Take(int slot)
        {
            TrayPiece? p = this[slot];
            if (!p.HasValue) throw new InvalidOperationException("Yuva " + slot + " bos.");
            _slots[slot] = null;
            return p.Value;
        }

        internal void Fill(TrayPiece[] pieces)
        {
            if (pieces == null || pieces.Length != SlotCount) throw new ArgumentException("Tepsi 3 parca ile doldurulur.", nameof(pieces));
            for (int i = 0; i < SlotCount; i++) _slots[i] = pieces[i];
        }

        internal void Set(int slot, TrayPiece? piece)
        {
            if ((uint)slot >= SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            _slots[slot] = piece;
        }
    }
}
