using System;
using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    /// <summary>docs/reports/m0-balance.md §7'deki B1-B3 hatalarinin regresyon testleri.</summary>
    public class BugfixTests
    {
        // ---------------- B1: gecersiz WeightProfile ----------------

        [Test]
        public void B1_Repro_MinFitting3_IsRejectedAtConstruction()
        {
            // Rapor: new WeightProfile(10, 10, 10, 1, 3, 3) dama tahtasinda NextTray'i cokertiyordu.
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeightProfile(10, 10, 10, 1, 3, 3));
        }

        [Test]
        public void B1_Repro_SmallMulZero_IsRejectedAtConstruction()
        {
            // Rapor: new WeightProfile(0, 10, 10, 1, 1, 3) dama tahtasinda NextTray'i cokertiyordu.
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeightProfile(0, 10, 10, 1, 1, 3));
        }

        [TestCase(1, 0, 0, 1, 1, 0)]
        [TestCase(1, 0, 0, 1, 2, 3)]
        [TestCase(10, 10, 10, 1, 1, 3)]
        public void B1_ValidProfiles_AreAccepted_AndGuaranteeHoldsOnCheckerboard(int s, int m, int l, int fit, int minFit, int maxL)
        {
            var prof = new WeightProfile(s, m, l, fit, minFit, maxL);
            Grid g = Grid.FromBits(Checkerboard()); // yalniz M1 sigar
            for (ulong seed = 1; seed <= 50; seed++)
            {
                TrayPiece[] t = new Generator(PieceCatalog.All, new Pcg32(seed)).NextTray(g, prof);
                int fitting = 0;
                for (int i = 0; i < t.Length; i++) if (g.FitsAnywhere(t[i].Def)) fitting++;
                Assert.That(fitting, Is.GreaterThanOrEqualTo(minFit), "tohum " + seed);
            }
        }

        [TestCase(1, -1, 0, 1, 1, 0, "mediumMul")]
        [TestCase(1, 0, -1, 1, 1, 0, "largeMul")]
        [TestCase(1, 0, 0, 0, 1, 0, "fitMul")]
        [TestCase(1, 0, 0, 1, 0, 0, "minFitting")]
        [TestCase(1, 0, 0, 1, 1, -1, "maxLarge")]
        [TestCase(1, 0, 0, 1, 1, 4, "maxLarge")]
        [TestCase(0, 10, 10, 1, 1, 3, "smallMul")]
        [TestCase(10, 10, 10, 1, 3, 3, "minFitting")]
        public void B1_InvalidProfiles_ThrowWithParamName(int s, int m, int l, int fit, int minFit, int maxL, string param)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new WeightProfile(s, m, l, fit, minFit, maxL));
            Assert.That(ex.ParamName, Is.EqualTo(param));
        }

        [Test]
        public void B1_AllCoreConfigProfiles_AreValid()
        {
            foreach (DifficultyMode mode in Enum.GetValues(typeof(DifficultyMode)))
                Assert.DoesNotThrow(() => Difficulty.Profile(mode));
        }

        [Test]
        public void B1_RepairWithNoCandidate_ThrowsClearMessage()
        {
            // Ozel katalog: tahtaya yalniz "A" (tek hucre) sigar ama agirligi 0. Ilk uc cekim buyuk B/C'den gelir,
            // onarimda sigan ve agirligi pozitif aday kalmaz -> anlasilir mesajla InvalidOperationException.
            var a = new PieceDef("A", 0, new Cell(0, 0));
            var big = new PieceDef("B", 1, new Cell(0, 0), new Cell(1, 0), new Cell(0, 1), new Cell(1, 1), new Cell(2, 0));
            var line = new PieceDef("C", 1, new Cell(0, 0), new Cell(1, 0), new Cell(2, 0), new Cell(3, 0), new Cell(4, 0));
            Grid g = Grid.FromBits(Checkerboard());
            var gen = new Generator(new[] { a, big, line }, new Pcg32(1));
            var ex = Assert.Throws<InvalidOperationException>(() => gen.NextTray(g, new WeightProfile(1, 1, 1, 1, 1, 3)));
            Assert.That(ex.Message, Does.Contain("Garanti onarimi"));
        }

        // ---------------- B3: tam dolu tahta ----------------

        [Test]
        public void B3_FullBoard_ThrowsArgumentExceptionWithMeaningfulMessage()
        {
            Grid g = Grid.FromBits(ulong.MaxValue);
            var rng = new Pcg32(1);
            (ulong st, ulong inc) = rng.GetState();
            var ex = Assert.Throws<ArgumentException>(() => new Generator(PieceCatalog.All, rng).NextTray(g, DifficultyMode.Normal));
            Assert.That(ex.Message, Does.Contain("tamamen dolu"));
            Assert.That(rng.GetState(), Is.EqualTo((st, inc)), "hata durumunda RNG tuketilmemeli");
        }

        // ---------------- B2: tutarsiz anlik goruntu ----------------

        private static GameSnapshot ValidMidGame()
        {
            // Bir hamle yapilmis gecerli oyun (tepside 2 parca).
            GameState gs = GameState.NewGame(4UL);
            var m = gs.LegalMoves().GetEnumerator();
            m.MoveNext();
            gs.TryPlace(m.Current.slot, m.Current.anchor.X, m.Current.anchor.Y);
            return gs.ToSnapshot();
        }

        private static void SetBits(GameSnapshot s, ulong bits)
        {
            s.Bits = bits;
            for (int i = 0; i < 64; i++) s.Colors[i] = (byte)(((bits >> i) & 1UL) != 0 ? 1 : 0);
        }

        [Test]
        public void B2_Repro_FullRow_IsRejected()
        {
            // Rapor: Bits = 0xFF (satir 0 dolu), tepside M1; (4,5)'e M1 konunca "hayalet" 1 cizgi patliyordu.
            GameSnapshot s = ValidMidGame();
            SetBits(s, 0xFFUL);
            s.TrayIds = new[] { "M1", null, null };
            s.TrayColors = new byte[] { 1, 0, 0 };
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
        }

        [Test]
        public void B2_FullColumn_IsRejected()
        {
            GameSnapshot s = ValidMidGame();
            SetBits(s, Col(3));
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
        }

        [Test]
        public void B2_AlmostFullRow_IsAccepted()
        {
            GameSnapshot s = ValidMidGame();
            SetBits(s, Row(0) & ~Bit(7, 0));
            Assert.DoesNotThrow(() => GameState.FromSnapshot(s));
        }

        [Test]
        public void B2_Repro_StatsMaxComboInconsistent_IsRejected()
        {
            // Rapor: Stats.MaxCombo = 0, MaxCombo = 5.
            GameSnapshot s = ValidMidGame();
            s.MaxCombo = 5;
            s.Stats.MaxCombo = 0;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s.Stats.MaxCombo = 6;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s.Stats.MaxCombo = 5;
            Assert.DoesNotThrow(() => GameState.FromSnapshot(s));
        }

        [Test]
        public void B2_Repro_ClearedInCurrentTray_WithFullTray_IsRejected()
        {
            GameSnapshot s = GameState.NewGame(1UL).ToSnapshot(); // tepsi tam dolu
            s.ClearedInCurrentTray = true;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
        }

        [Test]
        public void B2_ClearedInCurrentTray_WithPartialTray_IsAccepted()
        {
            GameSnapshot s = ValidMidGame();
            s.ClearedInCurrentTray = true;
            Assert.DoesNotThrow(() => GameState.FromSnapshot(s));
        }

        [Test]
        public void B2_NegativeOrInconsistentCounters_AreRejected()
        {
            GameSnapshot s = ValidMidGame(); s.Score = -1;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = ValidMidGame(); s.Combo = 3; // MaxCombo 0 < Combo
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = ValidMidGame(); s.TraysWithoutClear = -1;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = ValidMidGame(); s.Stats.Moves = -1;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = ValidMidGame(); s.Stats.LinesCleared = -1;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = ValidMidGame(); s.Stats.PerfectClears = -1;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = ValidMidGame(); s.Stats.TraysDealt = 0;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
        }

        [Test]
        public void B2_RealGames_AlwaysProduceAcceptedSnapshots()
        {
            var buffer = new System.Collections.Generic.List<(int slot, Cell anchor)>();
            for (ulong seed = 1; seed <= 30; seed++)
            {
                GameState gs = GameState.NewGame(seed);
                var bot = new Pcg32(seed + 1000000UL);
                while (!gs.IsGameOver)
                {
                    var m = PickRandomMove(gs, bot, buffer);
                    gs.TryPlace(m.slot, m.anchor.X, m.anchor.Y);
                    GameSnapshot snap = gs.ToSnapshot();
                    Assert.DoesNotThrow(() => GameState.FromSnapshot(snap), "tohum " + seed);
                }
            }
        }
    }
}
