using System.Collections.Generic;
using System.Linq;
using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    /// <summary>QA kenar durumlari (M0 denge simulasyonu ile birlikte eklendi).</summary>
    public class EdgeCaseTests
    {
        [Test]
        public void RowAndColumnAndPerfectClear_InSameMove_ViaGameState()
        {
            // Satir 0 ve sutun 0 dolu, yalniz (0,0) bos; baska hucre yok. M1 (0,0) -> 1 satir + 1 sutun, 15 hucre, tahta bos.
            ulong bits = (Row(0) | Col(0)) & ~Bit(0, 0);
            GameState gs = Build(bits, new[] { "M1", "O4", null });
            MoveResult r = gs.TryPlace(0, 0, 0);

            Assert.That(r.Success, Is.True);
            Assert.That(r.Placement.LineCount, Is.EqualTo(2));
            Assert.That(r.Placement.CellsCleared, Is.EqualTo(15));
            Assert.That(r.Placement.BoardCleared, Is.True);
            Assert.That(r.Score.PlacementPoints, Is.EqualTo(1));
            Assert.That(r.Score.LinePoints, Is.EqualTo(10 * 2 * 2 * 1));
            Assert.That(r.Score.PerfectClearBonus, Is.EqualTo(300));
            Assert.That(r.Score.Total, Is.EqualTo(341));
            Assert.That(gs.Scoring.Combo, Is.EqualTo(1));
            Assert.That(gs.Stats.PerfectClears, Is.EqualTo(1));
            Assert.That(gs.Grid.IsEmpty, Is.True);
            Assert.That(r.TrayRefilled, Is.False, "O4 hala tepside");
        }

        [Test]
        public void SingleEmptyCell_M1ClearsAllSixteenLines_ThenRefillsInOpenMode()
        {
            // Oyunda olusamaz (dolu satirlar zaten patlardi) ama FromSnapshot kabul eder; en uc patlatma durumu.
            GameState gs = Build(~Bit(4, 4), new[] { "M1", null, null });
            MoveResult r = gs.TryPlace(0, 4, 4);

            Assert.That(r.Placement.LineCount, Is.EqualTo(16));
            Assert.That(r.Placement.CellsCleared, Is.EqualTo(64));
            Assert.That(r.Placement.BoardCleared, Is.True);
            Assert.That(r.Score.Total, Is.EqualTo(1 + 10 * 16 * 16 * 1 + 300));
            Assert.That(r.TrayRefilled, Is.True);
            Assert.That(r.NewTrayMode, Is.EqualTo(DifficultyMode.Open), "bos tahta + bu tepside patlatma -> T=0 -> ACIK");
            Assert.That(gs.IsGameOver, Is.False);
        }

        [Test]
        public void Relief_WhenOnlyM1Fits_TrayContainsExactlyTwoM1()
        {
            // Dama tahtasinda yalniz M1 sigar; RAHATLATMA asgari 2 sigan ister, ayni parca en fazla 2 kez.
            Grid g = Grid.FromBits(Checkerboard());
            for (ulong seed = 1; seed <= 200; seed++)
            {
                TrayPiece[] t = new Generator(PieceCatalog.All, new Pcg32(seed)).NextTray(g, DifficultyMode.Relief);
                Assert.That(t.Count(p => p.Def.Id == "M1"), Is.EqualTo(2), "tohum " + seed);
                Assert.That(t.Count(p => g.FitsAnywhere(p.Def)), Is.EqualTo(2));
                Assert.That(t.Count(p => p.Def.Size == SizeClass.Large), Is.LessThanOrEqualTo(1));
            }
        }

        [Test]
        public void TrayRules_HoldOnEveryDealtTray_InRealGames()
        {
            // GDD §5.3-5.4: her yeni tepside >= 1 sigan (RAHATLATMA'da >= 2), ayni kimlik <= 2, RAHATLATMA'da <= 1 B;
            // yenilemenin hemen ardindan oyun bitmez. Rastgele ve "en cok hucre birakan" hamlelerle yogun tahtalar da denenir.
            var buffer = new List<(int slot, Cell anchor)>();
            int reliefTrays = 0;
            for (ulong seed = 1; seed <= 300; seed++)
            {
                GameState gs = GameState.NewGame(seed);
                AssertTray(gs, DifficultyMode.Open, seed);
                var bot = new Pcg32(seed + 1000000UL);
                int moves = 0;
                while (!gs.IsGameOver && moves < 3000)
                {
                    var m = PickRandomMove(gs, bot, buffer);
                    MoveResult r = gs.TryPlace(m.slot, m.anchor.X, m.anchor.Y);
                    Assert.That(r.Success, Is.True);
                    moves++;
                    if (r.TrayRefilled)
                    {
                        Assert.That(r.IsGameOver, Is.False, "yenilemeden hemen sonra oyun bitti, tohum " + seed);
                        AssertTray(gs, r.NewTrayMode.Value, seed);
                        if (r.NewTrayMode == DifficultyMode.Relief) reliefTrays++;
                    }
                }
            }
            Assert.That(reliefTrays, Is.GreaterThan(100), "RAHATLATMA yeterince denenmeli");
        }

        private static void AssertTray(GameState gs, DifficultyMode mode, ulong seed)
        {
            var defs = Enumerable.Range(0, 3).Select(i => gs.Tray[i].Value.Def).ToList();
            int fit = Enumerable.Range(0, 3).Count(gs.SlotFitsAnywhere);
            int min = Difficulty.Profile(mode).MinFitting;
            Assert.That(fit, Is.GreaterThanOrEqualTo(min), "sigan parca, tohum " + seed + " mod " + mode);
            Assert.That(defs.GroupBy(d => d.Id).Max(gr => gr.Count()), Is.LessThanOrEqualTo(2), "ayni kimlik, tohum " + seed);
            Assert.That(defs.Count(d => d.Size == SizeClass.Large), Is.LessThanOrEqualTo(Difficulty.Profile(mode).MaxLarge), "B sayisi, tohum " + seed);
        }
    }
}
