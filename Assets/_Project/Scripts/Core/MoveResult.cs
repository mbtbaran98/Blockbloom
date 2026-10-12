namespace Blockbloom.Core
{
    /// <summary>TryPlace basarisizlik nedenleri (GDD §9).</summary>
    public static class MoveFailReason
    {
        /// <summary>Yuva bos veya gecersiz yuva indeksi.</summary>
        public const string EmptySlot = "EmptySlot";
        /// <summary>CanPlace yanlis (tahta disi veya dolu hucre).</summary>
        public const string InvalidPlacement = "InvalidPlacement";
        /// <summary>Oyun bitmis.</summary>
        public const string GameOver = "GameOver";
    }

    /// <summary>GameState.TryPlace sonucu. Degismez.</summary>
    public sealed class MoveResult
    {
        internal static readonly MoveResult FailEmptySlot = new MoveResult(MoveFailReason.EmptySlot, false);
        internal static readonly MoveResult FailInvalidPlacement = new MoveResult(MoveFailReason.InvalidPlacement, false);
        internal static readonly MoveResult FailGameOver = new MoveResult(MoveFailReason.GameOver, true);

        private MoveResult(string failReason, bool isGameOver)
        {
            Success = false;
            FailReason = failReason;
            IsGameOver = isGameOver;
        }

        internal MoveResult(PlacementResult placement, ScoreBreakdown score, bool trayRefilled, DifficultyMode? newTrayMode, bool isGameOver)
        {
            Success = true;
            Placement = placement;
            Score = score;
            TrayRefilled = trayRefilled;
            NewTrayMode = newTrayMode;
            IsGameOver = isGameOver;
        }

        public bool Success { get; }
        /// <summary>Basariliysa null; aksi MoveFailReason sabitlerinden biri.</summary>
        public string FailReason { get; }
        /// <summary>Basarisizsa null.</summary>
        public PlacementResult Placement { get; }
        /// <summary>Basarisizsa default (tum alanlar 0).</summary>
        public ScoreBreakdown Score { get; }
        public bool TrayRefilled { get; }
        /// <summary>Yalniz hata ayiklama/simulasyon; oyuncuya gosterilmez (GDD §5.3).</summary>
        public DifficultyMode? NewTrayMode { get; }
        /// <summary>Hamle sonrasi oyun bitti mi (basarisiz GameOver sonucunda da true).</summary>
        public bool IsGameOver { get; }
    }
}
