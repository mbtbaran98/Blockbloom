using System;
using System.Collections.Generic;

namespace Blockbloom.Core
{
    /// <summary>
    /// Tek oyunun tum durumu ve hamle akisi (GDD §3.5). Core hic zaman okumaz; tum rastgelelik tohumdan gelir.
    /// Basarisiz TryPlace durumu degistirmez ve RNG tuketmez.
    /// </summary>
    public sealed class GameState
    {
        private readonly Pcg32 _rng;
        private readonly Generator _generator;
        private bool _clearedInTray;

        public Grid Grid { get; }
        public Tray Tray { get; }
        public ScoreKeeper Scoring { get; }
        public DifficultyState Difficulty { get; }
        public GameStats Stats { get; }
        public bool IsGameOver { get; private set; }
        public ulong Seed { get; }

        /// <summary>Mevcut tepside su ana kadar en az bir patlatma oldu mu (T hesabi icin).</summary>
        public bool ClearedInCurrentTray => _clearedInTray;

        private GameState(ulong seed, Pcg32 rng, Grid grid, Tray tray, ScoreKeeper scoring,
            DifficultyState difficulty, GameStats stats, bool clearedInTray)
        {
            Seed = seed;
            _rng = rng;
            _generator = new Generator(PieceCatalog.All, rng);
            Grid = grid;
            Tray = tray;
            Scoring = scoring;
            Difficulty = difficulty;
            Stats = stats;
            _clearedInTray = clearedInTray;
        }

        /// <summary>Bos tahta, K = 0, T = 0, ilk tepsi uretilir (mod ACIK).</summary>
        public static GameState NewGame(ulong seed)
        {
            var gs = new GameState(seed, new Pcg32(seed), new Grid(), new Tray(), new ScoreKeeper(),
                new DifficultyState(), new GameStats(), false);
            DifficultyMode mode = global::Blockbloom.Core.Difficulty.Evaluate(gs.Grid.FilledCount, gs.Difficulty);
            gs.Tray.Fill(gs._generator.NextTray(gs.Grid, mode));
            gs.Stats.TraysDealt = 1;
            gs.IsGameOver = !gs.AnySlotFits();
            return gs;
        }

        public bool CanPlace(int slot, int ax, int ay)
        {
            if ((uint)slot >= Tray.SlotCount) return false;
            TrayPiece? p = Tray[slot];
            return p.HasValue && Grid.CanPlace(p.Value.Def, ax, ay);
        }

        /// <summary>Yuvadaki parca tahtada herhangi bir yere sigiyor mu (soluk gosterim icin). Bos yuva -> false.</summary>
        public bool SlotFitsAnywhere(int slot)
        {
            if ((uint)slot >= Tray.SlotCount) return false;
            TrayPiece? p = Tray[slot];
            return p.HasValue && Grid.FitsAnywhere(p.Value.Def);
        }

        /// <summary>GDD §3.5'in tamami: yerlestir, patlat, puanla, gerekirse tepsiyi yenile, oyun sonunu denetle.</summary>
        public MoveResult TryPlace(int slot, int ax, int ay)
        {
            if (IsGameOver) return MoveResult.FailGameOver;
            if ((uint)slot >= Tray.SlotCount || !Tray[slot].HasValue) return MoveResult.FailEmptySlot;
            TrayPiece piece = Tray[slot].Value;
            if (!Grid.CanPlace(piece.Def, ax, ay)) return MoveResult.FailInvalidPlacement;

            // 1-3: yerlestir ve patlat
            Tray.Take(slot);
            PlacementResult placement = Grid.Place(piece.Def, ax, ay, piece.Color);
            // 4-5: kombo, patlatma puani, tam temizlik
            ScoreBreakdown score = Scoring.Apply(placement);
            if (placement.LineCount > 0) _clearedInTray = true;

            Stats.Moves++;
            Stats.LinesCleared += placement.LineCount;
            if (placement.BoardCleared) Stats.PerfectClears++;
            Stats.MaxCombo = Scoring.MaxCombo;

            // 6: tepsi yenileme (patlatmadan sonra)
            bool refilled = false;
            DifficultyMode? newMode = null;
            if (Tray.IsEmpty)
            {
                Difficulty.OnTrayCompleted(_clearedInTray);
                _clearedInTray = false;
                DifficultyMode mode = global::Blockbloom.Core.Difficulty.Evaluate(Grid.FilledCount, Difficulty);
                Tray.Fill(_generator.NextTray(Grid, mode));
                Stats.TraysDealt++;
                refilled = true;
                newMode = mode;
            }

            // 7: oyun sonu
            IsGameOver = !AnySlotFits();
            return new MoveResult(placement, score, refilled, newMode, IsGameOver);
        }

        /// <summary>Tum yasal hamleler; yuva, sonra y, sonra x artan sirada. Botlar icin.</summary>
        public IEnumerable<(int slot, Cell anchor)> LegalMoves()
        {
            for (int s = 0; s < Tray.SlotCount; s++)
            {
                TrayPiece? p = Tray[s];
                if (!p.HasValue) continue;
                ulong[] masks = p.Value.Def.AnchorMasks;
                Cell[] cells = p.Value.Def.AnchorCells;
                for (int i = 0; i < masks.Length; i++)
                {
                    if ((Grid.Bits & masks[i]) == 0UL) yield return (s, cells[i]);
                }
            }
        }

        /// <summary>LegalMoves() ile ayni sira; cagiranin listesine yazar (ayirmasiz; liste temizlenir). Sayiyi dondurur.</summary>
        public int LegalMoves(List<(int slot, Cell anchor)> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            ulong bits = Grid.Bits;
            for (int s = 0; s < Tray.SlotCount; s++)
            {
                TrayPiece? p = Tray[s];
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

        public GameSnapshot ToSnapshot()
        {
            var s = new GameSnapshot();
            s.Version = CoreConfig.SnapshotVersion;
            s.Seed = Seed;
            s.Bits = Grid.Bits;
            Grid.CopyColorsTo(s.Colors);
            for (int i = 0; i < Tray.SlotCount; i++)
            {
                TrayPiece? p = Tray[i];
                s.TrayIds[i] = p.HasValue ? p.Value.Def.Id : null;
                s.TrayColors[i] = p.HasValue ? p.Value.Color : (byte)0;
            }
            s.Score = Scoring.Score;
            s.Combo = Scoring.Combo;
            s.MaxCombo = Scoring.MaxCombo;
            s.TraysWithoutClear = Difficulty.TraysWithoutClear;
            s.ClearedInCurrentTray = _clearedInTray;
            (ulong state, ulong inc) = _rng.GetState();
            s.RngState = state;
            s.RngInc = inc;
            s.Stats = Stats.Clone();
            return s;
        }

        /// <summary>Anlik goruntuden oyun. Gecersiz veri -> ArgumentException. IsGameOver durumdan yeniden hesaplanir.</summary>
        public static GameState FromSnapshot(GameSnapshot s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            if (s.Version != CoreConfig.SnapshotVersion) throw new ArgumentException("Desteklenmeyen anlik goruntu surumu: " + s.Version, nameof(s));
            if (s.TrayIds == null || s.TrayIds.Length != Tray.SlotCount || s.TrayColors == null || s.TrayColors.Length != Tray.SlotCount)
                throw new ArgumentException("Tepsi verisi 3 oge olmali.", nameof(s));
            if (s.Stats == null) throw new ArgumentException("Stats bos olamaz.", nameof(s));

            Grid grid = Grid.FromRaw(s.Bits, s.Colors);
            var tray = new Tray();
            for (int i = 0; i < Tray.SlotCount; i++)
            {
                if (string.IsNullOrEmpty(s.TrayIds[i])) // JsonUtility null dizgiyi "" olarak geri yukleyebilir
                {
                    if (s.TrayColors[i] != 0) throw new ArgumentException("Bos yuvanin rengi 0 olmali.", nameof(s));
                    continue;
                }
                tray.Set(i, new TrayPiece(PieceCatalog.Get(s.TrayIds[i]), s.TrayColors[i]));
            }
            if (tray.IsEmpty) throw new ArgumentException("Tepside en az bir parca olmali (bos tepsi hamle icinde yenilenir).", nameof(s));

            var gs = new GameState(s.Seed, Pcg32.FromState(s.RngState, s.RngInc), grid, tray,
                new ScoreKeeper(s.Score, s.Combo, s.MaxCombo), new DifficultyState(s.TraysWithoutClear),
                s.Stats.Clone(), s.ClearedInCurrentTray);
            gs.IsGameOver = !gs.AnySlotFits();
            return gs;
        }

        private bool AnySlotFits()
        {
            for (int s = 0; s < Tray.SlotCount; s++)
            {
                if (SlotFitsAnywhere(s)) return true;
            }
            return false;
        }
    }
}
