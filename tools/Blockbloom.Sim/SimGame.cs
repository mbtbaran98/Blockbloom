using System;
using System.Collections.Generic;
using System.Diagnostics;
using Blockbloom.Core;

namespace Blockbloom.Sim
{
    /// <summary>Tek hamlenin simulasyon icin duzlestirilmis sonucu.</summary>
    public struct MoveOutcome
    {
        public int Total;
        public int Placement;
        public int LinePoints;
        public int Bonus;
        public int Lines;
        public int RowsCleared;
        public int ColsCleared;
        public int CellsCleared;
        public int ComboAfter;
        public bool BoardCleared;
        public bool Refilled;
        public DifficultyMode? NewMode;
        public bool GameOver;

        public static MoveOutcome From(PlacementResult pr, ScoreBreakdown sb, bool refilled, DifficultyMode? mode, bool over)
        {
            return new MoveOutcome
            {
                Total = sb.Total,
                Placement = sb.PlacementPoints,
                LinePoints = sb.LinePoints,
                Bonus = sb.PerfectClearBonus,
                Lines = pr.LineCount,
                RowsCleared = pr.ClearedRows.Count,
                ColsCleared = pr.ClearedCols.Count,
                CellsCleared = pr.CellsCleared,
                ComboAfter = sb.ComboAfter,
                BoardCleared = pr.BoardCleared,
                Refilled = refilled,
                NewMode = mode,
                GameOver = over,
            };
        }
    }

    /// <summary>Yeni dagitilan bir tepsinin bilgisi.</summary>
    public sealed class TrayInfo
    {
        public DifficultyMode Mode;
        /// <summary>Dagitim aninda tahtaya sigan parca sayisi (0..3).</summary>
        public int Fitting;
        /// <summary>Uretimdeki onarim cekimi sayisi; bilinmiyorsa -1.</summary>
        public int Repairs = -1;
        /// <summary>NextTray suresi (mikrosaniye); olculmediyse -1.</summary>
        public double GenMicros = -1;
        /// <summary>Uretim anindaki dolu hucre sayisi ve T.</summary>
        public int Filled;
        public int T;
        public PieceDef[] Pieces = new PieceDef[Tray.SlotCount];
    }

    /// <summary>Botlarin ve kosucunun gordugu ortak oyun yuzu.</summary>
    public interface ISimGame
    {
        ulong Bits { get; }
        int Combo { get; }
        int Score { get; }
        bool IsGameOver { get; }
        GameStats Stats { get; }
        TrayPiece? Slot(int i);
        int LegalMoves(List<(int slot, Cell anchor)> results);
        MoveOutcome Place(int slot, int ax, int ay);
        TrayInfo LastTray { get; }
        int RemainingInTray { get; }
    }

    /// <summary>Generator'in NextInt cagrilarini sayan saydam sarmalayici (dizi birebir ayni kalir).</summary>
    public sealed class CountingRandom : IRandom
    {
        private readonly Pcg32 _inner;
        public int NextIntCalls;

        public CountingRandom(Pcg32 inner) { _inner = inner; }
        public uint NextUInt() => _inner.NextUInt();
        public int NextInt(int maxExclusive) { NextIntCalls++; return _inner.NextInt(maxExclusive); }
        public (ulong State, ulong Inc) GetState() => _inner.GetState();
    }

    /// <summary>
    /// GameState'e dokunmadan ayni kurallari Grid + Generator + ScoreKeeper ile uygulayan ince dongu.
    /// difficultyOn=false: her tepsi NORMAL profille uretilir (zorluk ayari kapali karsilastirmasi, GDD §10).
    /// Uretici RNG'si sayilir; boylece onarim cekimleri ayirt edilir (S14) ve uretim suresi olculur (S15).
    /// </summary>
    public sealed class ThinGame : ISimGame
    {
        private readonly Grid _grid = new Grid();
        private readonly ScoreKeeper _sk = new ScoreKeeper();
        private readonly DifficultyState _ds = new DifficultyState();
        private readonly TrayPiece?[] _tray = new TrayPiece?[Tray.SlotCount];
        private readonly CountingRandom _rng;
        private readonly Generator _gen;
        private readonly bool _difficultyOn;
        private readonly SimDifficulty _cfg;
        private bool _clearedInTray;

        /// <summary>Parametreli zorluk (duyarlilik taramasi). cfg Core kurallarinin kopyasidir.</summary>
        public ThinGame(ulong seed, SimDifficulty cfg) : this(seed, true, cfg) { }

        public ThinGame(ulong seed, bool difficultyOn) : this(seed, difficultyOn, null) { }

        private ThinGame(ulong seed, bool difficultyOn, SimDifficulty cfg)
        {
            _cfg = cfg;
            _rng = new CountingRandom(new Pcg32(seed));
            _gen = new Generator(PieceCatalog.All, _rng);
            _difficultyOn = difficultyOn;
            Stats = new GameStats();
            Deal();
            Stats.TraysDealt = 1;
            IsGameOver = !AnyFits();
        }

        public Grid Grid => _grid;
        public DifficultyState Difficulty => _ds;
        public ulong Bits => _grid.Bits;
        public int Combo => _sk.Combo;
        public int MaxCombo => _sk.MaxCombo;
        public int Score => _sk.Score;
        public bool IsGameOver { get; private set; }
        public GameStats Stats { get; }
        public TrayInfo LastTray { get; private set; }
        public (ulong State, ulong Inc) RngState => _rng.GetState();

        public int RemainingInTray
        {
            get { int n = 0; for (int i = 0; i < _tray.Length; i++) if (_tray[i].HasValue) n++; return n; }
        }

        public TrayPiece? Slot(int i) => _tray[i];

        private void Deal()
        {
            int filled = _grid.FilledCount;
            DifficultyMode mode;
            WeightProfile prof;
            if (_cfg != null)
            {
                mode = _cfg.Evaluate(filled, _ds.TraysWithoutClear);
                prof = _cfg.Profile(mode);
            }
            else
            {
                mode = _difficultyOn ? global::Blockbloom.Core.Difficulty.Evaluate(filled, _ds) : DifficultyMode.Normal;
                prof = global::Blockbloom.Core.Difficulty.Profile(mode);
            }
            _rng.NextIntCalls = 0;
            long t0 = Stopwatch.GetTimestamp();
            TrayPiece[] tray = _gen.NextTray(_grid, prof);
            long t1 = Stopwatch.GetTimestamp();
            var info = new TrayInfo
            {
                Mode = mode,
                Repairs = _rng.NextIntCalls - 2 * Tray.SlotCount,
                GenMicros = (t1 - t0) * 1_000_000.0 / Stopwatch.Frequency,
                Filled = filled,
                T = _ds.TraysWithoutClear,
            };
            for (int i = 0; i < Tray.SlotCount; i++)
            {
                _tray[i] = tray[i];
                info.Pieces[i] = tray[i].Def;
                if (_grid.FitsAnywhere(tray[i].Def)) info.Fitting++;
            }
            LastTray = info;
        }

        public MoveOutcome Place(int slot, int ax, int ay)
        {
            if (IsGameOver) throw new InvalidOperationException("Oyun bitti.");
            TrayPiece? p = _tray[slot];
            if (!p.HasValue) throw new InvalidOperationException("Bos yuva " + slot);
            if (!_grid.CanPlace(p.Value.Def, ax, ay)) throw new InvalidOperationException("Gecersiz yerlestirme");

            _tray[slot] = null;
            PlacementResult pr = _grid.Place(p.Value.Def, ax, ay, p.Value.Color);
            ScoreBreakdown sb = _sk.Apply(pr);
            if (pr.LineCount > 0) _clearedInTray = true;
            Stats.Moves++;
            Stats.LinesCleared += pr.LineCount;
            if (pr.BoardCleared) Stats.PerfectClears++;
            Stats.MaxCombo = _sk.MaxCombo;

            bool refilled = false;
            DifficultyMode? mode = null;
            if (RemainingInTray == 0)
            {
                _ds.OnTrayCompleted(_clearedInTray);
                _clearedInTray = false;
                Deal();
                Stats.TraysDealt++;
                refilled = true;
                mode = LastTray.Mode;
            }
            IsGameOver = !AnyFits();
            return MoveOutcome.From(pr, sb, refilled, mode, IsGameOver);
        }

        public int LegalMoves(List<(int slot, Cell anchor)> results)
        {
            results.Clear();
            ulong bits = _grid.Bits;
            for (int s = 0; s < _tray.Length; s++)
            {
                TrayPiece? p = _tray[s];
                if (!p.HasValue) continue;
                ulong[] masks = p.Value.Def.AnchorMasks;
                Cell[] cells = p.Value.Def.AnchorCells;
                for (int i = 0; i < masks.Length; i++)
                {
                    if ((bits & masks[i]) == 0UL) results.Add((s, cells[i]));
                }
            }
            return results.Count;
        }

        private bool AnyFits()
        {
            for (int s = 0; s < _tray.Length; s++)
            {
                if (_tray[s].HasValue && _grid.FitsAnywhere(_tray[s].Value.Def)) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// GameState sarmalayicisi. Istege bagli golge ThinGame (zorluk ACIK) her hamlede ayni hamleyi oynar ve
    /// durum birebir karsilastirilir; golge ayrica onarim sayisini ve uretim suresini saglar.
    /// </summary>
    public sealed class GameStateAdapter : ISimGame
    {
        private ThinGame _shadow;

        public GameStateAdapter(GameState g, ThinGame shadow)
        {
            G = g;
            _shadow = shadow;
            LastTray = BuildTray(DifficultyMode.Open, isInitial: true);
            if (_shadow != null) CompareWithShadow(null, default, "baslangic");
        }

        public GameState G { get; }
        public string ShadowMismatch { get; private set; }
        public bool HasShadow => _shadow != null;

        public ulong Bits => G.Grid.Bits;
        public int Combo => G.Scoring.Combo;
        public int Score => G.Scoring.Score;
        public bool IsGameOver => G.IsGameOver;
        public GameStats Stats => G.Stats;
        public TrayInfo LastTray { get; private set; }
        public int RemainingInTray => G.Tray.RemainingCount;
        public TrayPiece? Slot(int i) => G.Tray[i];
        public int LegalMoves(List<(int slot, Cell anchor)> results) => G.LegalMoves(results);

        public MoveOutcome Place(int slot, int ax, int ay)
        {
            MoveResult r = G.TryPlace(slot, ax, ay);
            if (!r.Success) throw new InvalidOperationException("TryPlace basarisiz: " + r.FailReason);
            MoveOutcome o = MoveOutcome.From(r.Placement, r.Score, r.TrayRefilled, r.NewTrayMode, r.IsGameOver);
            if (r.TrayRefilled) LastTray = BuildTray(r.NewTrayMode.Value, isInitial: false);
            if (_shadow != null)
            {
                MoveOutcome so = _shadow.Place(slot, ax, ay);
                CompareWithShadow(so, o, "hamle " + G.Stats.Moves);
                if (r.TrayRefilled && _shadow != null)
                {
                    LastTray.Repairs = _shadow.LastTray.Repairs;
                    LastTray.GenMicros = _shadow.LastTray.GenMicros;
                }
            }
            return o;
        }

        private TrayInfo BuildTray(DifficultyMode mode, bool isInitial)
        {
            var info = new TrayInfo
            {
                Mode = mode,
                Filled = G.Grid.FilledCount,
                T = G.Difficulty.TraysWithoutClear,
            };
            for (int i = 0; i < Tray.SlotCount; i++)
            {
                TrayPiece? p = G.Tray[i];
                info.Pieces[i] = p.HasValue ? p.Value.Def : null;
                if (G.SlotFitsAnywhere(i)) info.Fitting++;
            }
            if (isInitial && _shadow != null)
            {
                info.Repairs = _shadow.LastTray.Repairs;
                info.GenMicros = _shadow.LastTray.GenMicros;
            }
            return info;
        }

        private void CompareWithShadow(MoveOutcome? so, MoveOutcome o, string where)
        {
            string err = null;
            ThinGame s = _shadow;
            if (so.HasValue)
            {
                MoveOutcome a = so.Value;
                if (a.Total != o.Total || a.Lines != o.Lines || a.ComboAfter != o.ComboAfter || a.Refilled != o.Refilled
                    || a.NewMode != o.NewMode || a.GameOver != o.GameOver || a.CellsCleared != o.CellsCleared)
                    err = "hamle sonucu farkli";
            }
            if (err == null && s.Bits != G.Grid.Bits) err = "tahta bitleri farkli";
            if (err == null && (s.Score != G.Scoring.Score || s.Combo != G.Scoring.Combo || s.MaxCombo != G.Scoring.MaxCombo)) err = "skor/kombo farkli";
            if (err == null && s.Difficulty.TraysWithoutClear != G.Difficulty.TraysWithoutClear) err = "T farkli";
            if (err == null && s.IsGameOver != G.IsGameOver) err = "oyun sonu farkli";
            if (err == null)
            {
                for (int i = 0; i < Tray.SlotCount; i++)
                {
                    TrayPiece? a = s.Slot(i), b = G.Tray[i];
                    if (a.HasValue != b.HasValue || (a.HasValue && (a.Value.Def != b.Value.Def || a.Value.Color != b.Value.Color)))
                    {
                        err = "tepsi yuvasi " + i + " farkli";
                        break;
                    }
                }
            }
            if (err == null)
            {
                GameStats x = s.Stats, y = G.Stats;
                if (x.Moves != y.Moves || x.LinesCleared != y.LinesCleared || x.MaxCombo != y.MaxCombo
                    || x.PerfectClears != y.PerfectClears || x.TraysDealt != y.TraysDealt) err = "istatistik farkli";
            }
            if (err == null && (G.IsGameOver || (so.HasValue && so.Value.Refilled)))
            {
                GameSnapshot snap = G.ToSnapshot();
                (ulong st, ulong inc) = s.RngState;
                if (snap.RngState != st || snap.RngInc != inc) err = "RNG durumu farkli";
            }
            if (err != null)
            {
                ShadowMismatch = where + ": " + err;
                _shadow = null; // ilk farktan sonra karsilastirmayi birak
            }
        }
    }
}
