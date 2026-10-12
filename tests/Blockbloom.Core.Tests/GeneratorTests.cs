using System;
using System.Collections.Generic;
using System.Linq;
using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    public class GeneratorTests
    {
        private static string[] Ids(TrayPiece[] t) => t.Select(p => p.Def.Id).ToArray();

        [Test]
        public void Difficulty_Evaluate_RulesInOrder()
        {
            var s0 = new DifficultyState();
            Assert.That(Difficulty.Evaluate(0, s0), Is.EqualTo(DifficultyMode.Open));
            Assert.That(Difficulty.Evaluate(15, s0), Is.EqualTo(DifficultyMode.Open));
            Assert.That(Difficulty.Evaluate(16, s0), Is.EqualTo(DifficultyMode.Normal));
            Assert.That(Difficulty.Evaluate(31, s0), Is.EqualTo(DifficultyMode.Normal));
            Assert.That(Difficulty.Evaluate(32, s0), Is.EqualTo(DifficultyMode.Relief));

            var s1 = new DifficultyState();
            s1.OnTrayCompleted(false);
            Assert.That(s1.TraysWithoutClear, Is.EqualTo(1));
            Assert.That(Difficulty.Evaluate(0, s1), Is.EqualTo(DifficultyMode.Normal));
            s1.OnTrayCompleted(false);
            Assert.That(Difficulty.Evaluate(0, s1), Is.EqualTo(DifficultyMode.Relief));
            s1.OnTrayCompleted(true);
            Assert.That(s1.TraysWithoutClear, Is.EqualTo(0));
            Assert.That(Difficulty.Evaluate(10, s1), Is.EqualTo(DifficultyMode.Open));
        }

        [Test]
        public void Profiles_MatchGdd()
        {
            WeightProfile r = Difficulty.Profile(DifficultyMode.Relief);
            Assert.That(new[] { r.SmallMul, r.MediumMul, r.LargeMul, r.FitMul, r.MinFitting, r.MaxLarge }, Is.EqualTo(new[] { 15, 10, 5, 3, 2, 1 }));
            WeightProfile o = Difficulty.Profile(DifficultyMode.Open);
            Assert.That(new[] { o.SmallMul, o.MediumMul, o.LargeMul, o.FitMul, o.MinFitting, o.MaxLarge }, Is.EqualTo(new[] { 8, 10, 13, 1, 1, 3 }));
            WeightProfile n = Difficulty.Profile(DifficultyMode.Normal);
            Assert.That(new[] { n.SmallMul, n.MediumMul, n.LargeMul, n.FitMul, n.MinFitting, n.MaxLarge }, Is.EqualTo(new[] { 10, 10, 10, 1, 1, 3 }));
        }

        [Test]
        public void Open_EmptyBoard_ExactWeightsAndConsumptionOrder()
        {
            var rng = new ScriptedRandom(0, 0, 0, 0, 1, 5);
            TrayPiece[] t = new Generator(PieceCatalog.All, rng).NextTray(new Grid(), DifficultyMode.Open);
            // K 44*8 + O 54*10 + B 26*13 = 1230; ucuncu yuvada M1 (agirlik 32) dislanir
            Assert.That(rng.NextIntArgs, Is.EqualTo(new[] { 1230, 1230, 1198, 6, 6, 6 }));
            Assert.That(Ids(t), Is.EqualTo(new[] { "M1", "M1", "I2H" }));
            Assert.That(t.Select(p => (int)p.Color), Is.EqualTo(new[] { 1, 2, 6 }));
        }

        [Test]
        public void Normal_SamePieceAtMostTwice_LargeAllowedUpToThree()
        {
            var rng = new ScriptedRandom(1239, 1239, 1199, 0, 0, 0);
            TrayPiece[] t = new Generator(PieceCatalog.All, rng).NextTray(new Grid(), DifficultyMode.Normal);
            Assert.That(rng.NextIntArgs, Is.EqualTo(new[] { 1240, 1240, 1200, 6, 6, 6 }));
            Assert.That(Ids(t), Is.EqualTo(new[] { "O9", "O9", "R6V" }));
        }

        [Test]
        public void Relief_AtMostOneLarge()
        {
            // bos tahta: K 44*15*3 + O 54*10*3 + B 26*5*3 = 3990; O9 sonrasi tum B dislanir: 3990 - 390 = 3600
            var rng = new ScriptedRandom(3989, 3599, 3599, 0, 0, 0);
            TrayPiece[] t = new Generator(PieceCatalog.All, rng).NextTray(new Grid(), DifficultyMode.Relief);
            Assert.That(rng.NextIntArgs, Is.EqualTo(new[] { 3990, 3600, 3600, 6, 6, 6 }));
            Assert.That(Ids(t), Is.EqualTo(new[] { "O9", "J4D", "J4D" }));
        }

        [Test]
        public void Normal_Repair_ReplacesHighestNonFittingSlot()
        {
            // dama tahtasi: yalniz M1 sigar. Normal: toplam 1240, M1 = 40.
            var g = Grid.FromBits(Checkerboard());
            var rng = new ScriptedRandom(40, 40, 40, 0, 0, 0, 0);
            TrayPiece[] t = new Generator(PieceCatalog.All, rng).NextTray(g, DifficultyMode.Normal);
            Assert.That(rng.NextIntArgs, Is.EqualTo(new[] { 1240, 1240, 1180, 40, 6, 6, 6 }));
            Assert.That(Ids(t), Is.EqualTo(new[] { "I2H", "I2H", "M1" }));
        }

        [Test]
        public void Relief_Repair_TwoFittingPieces_FitMultiplierApplied()
        {
            // dama tahtasi (32 dolu): M1 = 4*15*3 = 180; toplam 1450
            var g = Grid.FromBits(Checkerboard());
            var rng = new ScriptedRandom(180, 180, 180, 0, 0, 0, 0, 0);
            TrayPiece[] t = new Generator(PieceCatalog.All, rng).NextTray(g, DifficultyMode.Relief);
            Assert.That(rng.NextIntArgs, Is.EqualTo(new[] { 1450, 1450, 1360, 180, 180, 6, 6, 6 }));
            Assert.That(Ids(t), Is.EqualTo(new[] { "I2H", "M1", "M1" }));
        }

        [Test]
        public void Generator_UsesExactlySixDrawsOnEmptyBoard()
        {
            var rng = new ScriptedRandom();
            new Generator(PieceCatalog.All, rng).NextTray(new Grid(), DifficultyMode.Open);
            Assert.That(rng.NextIntArgs.Count, Is.EqualTo(6));
        }

        [TestCase(DifficultyMode.Open)]
        [TestCase(DifficultyMode.Normal)]
        [TestCase(DifficultyMode.Relief)]
        public void Guarantee_HoldsOnManyRandomBoards(DifficultyMode mode)
        {
            WeightProfile prof = Difficulty.Profile(mode);
            var boardRng = new Pcg32(1234UL + (ulong)mode);
            var gen = new Generator(PieceCatalog.All, new Pcg32(99UL + (ulong)mode));
            for (int trial = 0; trial < 20000; trial++)
            {
                // yogunluk %0..%97 arasi rastgele
                int density = boardRng.NextInt(98);
                ulong bits = 0;
                for (int i = 0; i < 64; i++) if (boardRng.NextInt(100) < density) bits |= 1UL << i;
                if (bits == ulong.MaxValue) bits &= ~(1UL << boardRng.NextInt(64));
                var g = Grid.FromBits(bits);

                TrayPiece[] t = gen.NextTray(g, mode);
                int fitting = t.Count(p => g.FitsAnywhere(p.Def));
                int fittablePieces = PieceCatalog.All.Count(p => g.FitsAnywhere(p));
                Assert.That(fitting, Is.GreaterThanOrEqualTo(Math.Min(prof.MinFitting, 3)), "sigma garantisi");
                Assert.That(t.Count(p => p.Def.Size == SizeClass.Large), Is.LessThanOrEqualTo(prof.MaxLarge));
                Assert.That(t.GroupBy(p => p.Def.Id).Max(gr => gr.Count()), Is.LessThanOrEqualTo(2));
                Assert.That(t.All(p => p.Color >= 1 && p.Color <= 6), Is.True);
                Assert.That(fittablePieces, Is.GreaterThan(0));
            }
        }

        [Test]
        public void Normal_Slot0Distribution_MatchesBaseWeights()
        {
            var gen = new Generator(PieceCatalog.All, new Pcg32(31337UL));
            var g = new Grid();
            var counts = new Dictionary<string, int>();
            const int N = 124000;
            for (int i = 0; i < N; i++)
            {
                string id = gen.NextTray(g, DifficultyMode.Normal)[0].Def.Id;
                counts.TryGetValue(id, out int c);
                counts[id] = c + 1;
            }
            foreach (PieceDef p in PieceCatalog.All)
            {
                counts.TryGetValue(p.Id, out int c);
                double freq = (double)c / N, expected = p.BaseWeight / 124.0;
                Assert.That(Math.Abs(freq - expected), Is.LessThan(0.005), p.Id);
            }
        }

        [Test]
        public void Generator_IsDeterministic()
        {
            var a = new Generator(PieceCatalog.All, new Pcg32(5UL));
            var b = new Generator(PieceCatalog.All, new Pcg32(5UL));
            var g = Grid.FromBits(0x00FF00F0F0000F0FUL);
            for (int i = 0; i < 200; i++)
            {
                var mode = (DifficultyMode)(i % 3);
                TrayPiece[] ta = a.NextTray(g, mode), tb = b.NextTray(g, mode);
                Assert.That(Ids(tb), Is.EqualTo(Ids(ta)));
                Assert.That(tb.Select(p => p.Color), Is.EqualTo(ta.Select(p => p.Color)));
            }
        }
    }
}
