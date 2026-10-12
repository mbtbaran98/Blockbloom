using System.Collections.Generic;
using System.Linq;
using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    public class GameStateTests
    {
        [Test]
        public void NewGame_InitialState()
        {
            GameState gs = GameState.NewGame(42UL);
            Assert.That(gs.Seed, Is.EqualTo(42UL));
            Assert.That(gs.Grid.IsEmpty, Is.True);
            Assert.That(gs.Tray.RemainingCount, Is.EqualTo(3));
            Assert.That(gs.Scoring.Score, Is.EqualTo(0));
            Assert.That(gs.Scoring.Combo, Is.EqualTo(0));
            Assert.That(gs.Difficulty.TraysWithoutClear, Is.EqualTo(0));
            Assert.That(gs.Stats.TraysDealt, Is.EqualTo(1));
            Assert.That(gs.Stats.Moves, Is.EqualTo(0));
            Assert.That(gs.IsGameOver, Is.False);
            Assert.That(gs.ClearedInCurrentTray, Is.False);
        }

        [Test]
        public void NewGame_FirstTrayUsesOpenMode()
        {
            // Ilk tepsi, ayni tohumlu RNG ile ACIK profilde uretilen tepsiyle ayni olmali
            GameState gs = GameState.NewGame(777UL);
            TrayPiece[] expected = new Generator(PieceCatalog.All, new Pcg32(777UL)).NextTray(new Grid(), DifficultyMode.Open);
            for (int i = 0; i < 3; i++)
            {
                Assert.That(gs.Tray[i].Value.Def.Id, Is.EqualTo(expected[i].Def.Id));
                Assert.That(gs.Tray[i].Value.Color, Is.EqualTo(expected[i].Color));
            }
        }

        [Test]
        public void Tray_RefillsOnlyWhenAllThreeUsed_SlotsDoNotShift()
        {
            GameState gs = Build(0UL, new[] { "M1", "I2H", "O4" });
            MoveResult r = gs.TryPlace(1, 0, 0);
            Assert.That(r.Success, Is.True);
            Assert.That(r.TrayRefilled, Is.False);
            Assert.That(r.NewTrayMode, Is.Null);
            Assert.That(gs.Tray[1].HasValue, Is.False);
            Assert.That(gs.Tray[0].Value.Def.Id, Is.EqualTo("M1"));
            Assert.That(gs.Tray[2].Value.Def.Id, Is.EqualTo("O4"));

            r = gs.TryPlace(0, 7, 7);
            Assert.That(r.TrayRefilled, Is.False);
            Assert.That(gs.Tray.RemainingCount, Is.EqualTo(1));

            r = gs.TryPlace(2, 3, 3);
            Assert.That(r.TrayRefilled, Is.True);
            Assert.That(r.NewTrayMode, Is.Not.Null);
            Assert.That(gs.Tray.RemainingCount, Is.EqualTo(3));
            Assert.That(gs.Stats.TraysDealt, Is.EqualTo(2));
            Assert.That(gs.Stats.Moves, Is.EqualTo(3));
        }

        [Test]
        public void Refill_HappensAfterClear_ModeUsesPostClearCount()
        {
            // satir 0 (0,0) haric + satir 2..5 x 0..5 + (7,7): 32 dolu. M1 sonrasi 33, satir 0 patlar -> 25.
            ulong bits = (Row(0) & ~Bit(0, 0)) | Bit(7, 7);
            for (int y = 2; y <= 5; y++) for (int x = 0; x <= 5; x++) bits |= Bit(x, y);
            GameState gs = Build(bits, new[] { "M1", null, null });
            MoveResult r = gs.TryPlace(0, 0, 0);
            Assert.That(r.Placement.LineCount, Is.EqualTo(1));
            Assert.That(gs.Grid.FilledCount, Is.EqualTo(25));
            Assert.That(r.TrayRefilled, Is.True);
            Assert.That(r.NewTrayMode, Is.EqualTo(DifficultyMode.Normal), "patlatma oncesi sayimla RAHATLATMA olurdu");
            Assert.That(gs.Difficulty.TraysWithoutClear, Is.EqualTo(0));
        }

        [Test]
        public void Combo_PersistsAcrossTrayRefill()
        {
            GameState gs = Build(Row(0) & ~Bit(0, 0), new[] { "M1", null, null }, combo: 1);
            MoveResult r = gs.TryPlace(0, 0, 0);
            Assert.That(r.TrayRefilled, Is.True);
            Assert.That(r.Score.ComboAfter, Is.EqualTo(2));
            Assert.That(r.Score.LinePoints, Is.EqualTo(10 * 1 * 2));
            Assert.That(r.Score.PerfectClearBonus, Is.EqualTo(300));
            Assert.That(r.Score.Total, Is.EqualTo(1 + 20 + 300));
            Assert.That(gs.Scoring.Combo, Is.EqualTo(2));
            Assert.That(gs.Stats.PerfectClears, Is.EqualTo(1));
            Assert.That(gs.Stats.MaxCombo, Is.EqualTo(2));
        }

        [Test]
        public void TraysWithoutClear_IncrementsAndTriggersRelief()
        {
            GameState gs = Build(Bit(7, 7), new[] { "M1", null, null }, traysWithoutClear: 1);
            MoveResult r = gs.TryPlace(0, 0, 0);
            Assert.That(gs.Difficulty.TraysWithoutClear, Is.EqualTo(2));
            Assert.That(r.NewTrayMode, Is.EqualTo(DifficultyMode.Relief));
        }

        [Test]
        public void EarlierClearInSameTray_ResetsT()
        {
            // Bu tepside daha once patlatma olmus (anlik goruntude kayitli): T sifirlanir, mod ACIK
            GameState gs = Build(Bit(7, 7), new[] { "M1", null, null }, traysWithoutClear: 1, clearedInTray: true);
            MoveResult r = gs.TryPlace(0, 0, 0);
            Assert.That(gs.Difficulty.TraysWithoutClear, Is.EqualTo(0));
            Assert.That(r.NewTrayMode, Is.EqualTo(DifficultyMode.Open));
            Assert.That(gs.ClearedInCurrentTray, Is.False);
        }

        [Test]
        public void FailedMoves_DoNotChangeStateOrRng()
        {
            GameState gs = Build(Bit(3, 3), new[] { "O4", null, "M1" }, seed: 11);
            GameSnapshot before = gs.ToSnapshot();

            MoveResult r = gs.TryPlace(1, 0, 0);
            Assert.That(r.Success, Is.False);
            Assert.That(r.FailReason, Is.EqualTo(MoveFailReason.EmptySlot));
            Assert.That(r.Placement, Is.Null);
            Assert.That(r.Score.Total, Is.EqualTo(0));

            Assert.That(gs.TryPlace(-1, 0, 0).FailReason, Is.EqualTo(MoveFailReason.EmptySlot));
            Assert.That(gs.TryPlace(3, 0, 0).FailReason, Is.EqualTo(MoveFailReason.EmptySlot));
            Assert.That(gs.TryPlace(0, 7, 0).FailReason, Is.EqualTo(MoveFailReason.InvalidPlacement));
            Assert.That(gs.TryPlace(0, -1, 0).FailReason, Is.EqualTo(MoveFailReason.InvalidPlacement));
            Assert.That(gs.TryPlace(0, 2, 2).FailReason, Is.EqualTo(MoveFailReason.InvalidPlacement));
            Assert.That(gs.TryPlace(2, 3, 3).FailReason, Is.EqualTo(MoveFailReason.InvalidPlacement));

            AssertSnapshotsEqual(before, gs.ToSnapshot());

            // Basarisiz denemeler sonraki oyunu degistirmez: ayni hamleler ayni sonucu verir
            GameState clean = GameState.FromSnapshot(before);
            MoveResult a = gs.TryPlace(0, 0, 0), b = clean.TryPlace(0, 0, 0);
            MoveResult a2 = gs.TryPlace(2, 7, 7), b2 = clean.TryPlace(2, 7, 7);
            Assert.That(a2.TrayRefilled && b2.TrayRefilled, Is.True);
            AssertSnapshotsEqual(clean.ToSnapshot(), gs.ToSnapshot());
        }

        [Test]
        public void GameOver_WhenNoRemainingPieceFits()
        {
            // dama tahtasi: I2H hicbir yere sigmaz, M1 sigar
            GameState gs = Build(Checkerboard(), new[] { "I2H", "M1", null });
            Assert.That(gs.IsGameOver, Is.False);
            Assert.That(gs.SlotFitsAnywhere(0), Is.False);
            Assert.That(gs.SlotFitsAnywhere(1), Is.True);
            Assert.That(gs.SlotFitsAnywhere(2), Is.False);

            MoveResult r = gs.TryPlace(1, 1, 0);
            Assert.That(r.Success, Is.True);
            Assert.That(r.IsGameOver, Is.True);
            Assert.That(gs.IsGameOver, Is.True);
            Assert.That(gs.LegalMoves().Any(), Is.False);

            GameSnapshot before = gs.ToSnapshot();
            MoveResult after = gs.TryPlace(0, 0, 0);
            Assert.That(after.Success, Is.False);
            Assert.That(after.FailReason, Is.EqualTo(MoveFailReason.GameOver));
            Assert.That(after.IsGameOver, Is.True);
            AssertSnapshotsEqual(before, gs.ToSnapshot());
        }

        [Test]
        public void FromSnapshot_DetectsGameOverImmediately()
        {
            GameState gs = Build(Checkerboard(), new[] { "I2H", "O9", null });
            Assert.That(gs.IsGameOver, Is.True);
        }

        [Test]
        public void GameOver_ChecksPiecesIndividually_NotJointly()
        {
            // Dama tahtasinda (dolu satir/sutun yok) yalniz sol-ust 2x2 bos: O4 yalniz bir yere sigar.
            // Iki O4 ayri ayri sigar; birlikte sigmasalar da oyun bitmez.
            ulong bits = Checkerboard() & ~(Bit(0, 0) | Bit(1, 0) | Bit(0, 1) | Bit(1, 1));
            GameState gs = Build(bits, new[] { "O4", "O4", null });
            Assert.That(gs.Grid.ValidAnchors(PieceCatalog.Get("O4")).Count(), Is.EqualTo(1));
            Assert.That(gs.IsGameOver, Is.False);
        }

        [Test]
        public void LegalMoves_OrderAndCompleteness()
        {
            GameState gs = Build(Bit(2, 2) | Bit(5, 6), new[] { "O4", null, "I3V" });
            var moves = gs.LegalMoves().ToList();
            var buffer = new List<(int slot, Cell anchor)>();
            Assert.That(gs.LegalMoves(buffer), Is.EqualTo(moves.Count));
            Assert.That(buffer, Is.EqualTo(moves));

            int brute = 0;
            for (int s = 0; s < 3; s++)
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                        if (gs.CanPlace(s, x, y)) brute++;
            Assert.That(moves.Count, Is.EqualTo(brute));
            for (int i = 1; i < moves.Count; i++)
            {
                var p = moves[i - 1];
                var c = moves[i];
                int kp = p.slot * 64 + p.anchor.Index, kc = c.slot * 64 + c.anchor.Index;
                Assert.That(kc, Is.GreaterThan(kp));
            }
            Assert.That(moves.All(m => m.slot != 1), Is.True);
        }

        [Test]
        public void Score_EqualsSumOfMoveTotals_AndStatsConsistent_RandomGames()
        {
            var buffer = new List<(int slot, Cell anchor)>();
            for (ulong seed = 1; seed <= 200; seed++)
            {
                GameState gs = GameState.NewGame(seed);
                var bot = new Pcg32(seed + 1000000UL);
                long sum = 0;
                int lines = 0, moves = 0, perfect = 0, trays = 1, maxCombo = 0;
                while (!gs.IsGameOver && moves < 3000)
                {
                    var m = PickRandomMove(gs, bot, buffer);
                    MoveResult r = gs.TryPlace(m.slot, m.anchor.X, m.anchor.Y);
                    Assert.That(r.Success, Is.True);
                    moves++;
                    sum += r.Score.Total;
                    lines += r.Placement.LineCount;
                    if (r.Placement.BoardCleared) perfect++;
                    maxCombo = System.Math.Max(maxCombo, r.Score.ComboAfter);
                    int nr = r.Placement.ClearedRows.Count, nc = r.Placement.ClearedCols.Count;
                    Assert.That(r.Placement.CellsCleared, Is.EqualTo(8 * nr + 8 * nc - nr * nc));
                    if (r.TrayRefilled)
                    {
                        trays++;
                        int fit = 0;
                        for (int s = 0; s < 3; s++) if (gs.SlotFitsAnywhere(s)) fit++;
                        Assert.That(fit, Is.GreaterThanOrEqualTo(1), "yeni tepside en az 1 sigan");
                        Assert.That(r.IsGameOver, Is.False, "yenilemeden hemen sonra oyun bitmez");
                        if (r.NewTrayMode == DifficultyMode.Relief)
                        {
                            Assert.That(fit, Is.GreaterThanOrEqualTo(2), "RAHATLATMA: en az 2 sigan");
                            int large = 0;
                            for (int s = 0; s < 3; s++) if (gs.Tray[s].Value.Def.Size == SizeClass.Large) large++;
                            Assert.That(large, Is.LessThanOrEqualTo(1), "RAHATLATMA: en fazla 1 B");
                        }
                        var ids = new[] { gs.Tray[0].Value.Def.Id, gs.Tray[1].Value.Def.Id, gs.Tray[2].Value.Def.Id };
                        Assert.That(ids.GroupBy(x => x).Max(gr => gr.Count()), Is.LessThanOrEqualTo(2));
                    }
                    Assert.That(gs.IsGameOver, Is.EqualTo(gs.LegalMoves(buffer) == 0));
                }
                Assert.That(gs.IsGameOver, Is.True, "rastgele bot oyunu bitirmeli");
                Assert.That(gs.Scoring.Score, Is.EqualTo(sum));
                Assert.That(gs.Stats.Moves, Is.EqualTo(moves));
                Assert.That(gs.Stats.LinesCleared, Is.EqualTo(lines));
                Assert.That(gs.Stats.PerfectClears, Is.EqualTo(perfect));
                Assert.That(gs.Stats.TraysDealt, Is.EqualTo(trays));
                Assert.That(gs.Stats.MaxCombo, Is.EqualTo(maxCombo));
                Assert.That(gs.Scoring.MaxCombo, Is.EqualTo(maxCombo));
            }
        }
    }
}
