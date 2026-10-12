using System;
using System.Collections.Generic;
using System.Diagnostics;
using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    public class RecordStoreTests
    {
        [Test]
        public void MemoryRecordStore_SavesValues()
        {
            IRecordStore store = new MemoryRecordStore();
            Assert.That(store.BestScore, Is.EqualTo(0));
            store.Save(524, 3, 4);
            Assert.That(store.BestScore, Is.EqualTo(524));
            Assert.That(store.GamesPlayed, Is.EqualTo(3));
            Assert.That(store.BestCombo, Is.EqualTo(4));
            Assert.That(((MemoryRecordStore)store).SaveCount, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => store.Save(-1, 0, 0));

            var seeded = new MemoryRecordStore(10, 2, 1);
            Assert.That(seeded.BestScore, Is.EqualTo(10));
            Assert.That(seeded.SaveCount, Is.EqualTo(0));
        }
    }

    public class TrayTests
    {
        [Test]
        public void Take_EmptiesSlot_EmptySlotThrows()
        {
            GameState gs = Build(0UL, new[] { "M1", null, "O4" });
            Tray t = gs.Tray;
            Assert.That(t.RemainingCount, Is.EqualTo(2));
            Assert.That(t[1], Is.Null);
            Assert.Throws<InvalidOperationException>(() => t.Take(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = t[3]; });
            TrayPiece p = t.Take(2);
            Assert.That(p.Def.Id, Is.EqualTo("O4"));
            Assert.That(p.Color, Is.EqualTo(3));
            Assert.That(t[2], Is.Null);
            Assert.That(t.RemainingCount, Is.EqualTo(1));
        }
    }

    public class PerformanceTests
    {
        private static ulong RandomBits(Pcg32 r) => ((ulong)r.NextUInt() << 32 | r.NextUInt()) & ((ulong)r.NextUInt() << 32 | r.NextUInt());

        [Test]
        public void HotPaths_DoNotAllocate()
        {
            var g = Grid.FromBits(0x0F0F00F000FF0001UL);
            PieceDef o9 = PieceCatalog.Get("O9");
            PieceDef l4 = PieceCatalog.Get("L4A");
            GameState gs = GameState.NewGame(5UL);
            var buffer = new List<(int slot, Cell anchor)>(512);
            gs.LegalMoves(buffer); // ilk buyume
            bool sink = false;
            int isink = 0;

            // isinma (JIT)
            for (int i = 0; i < 100; i++)
            {
                sink ^= g.CanPlace(o9, i & 7, (i >> 3) & 7) | g.FitsAnywhere(l4);
                isink += g.PreviewLines(l4, i & 7, 0).LineCount + gs.LegalMoves(buffer);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                sink ^= g.CanPlace(o9, i & 7, (i >> 3) & 7);
                sink ^= g.FitsAnywhere(l4);
                LinePreview pv = g.PreviewLines(l4, i & 7, 0);
                isink += pv.LineCount + pv.Rows.Count;
                isink += gs.LegalMoves(buffer);
                sink ^= gs.SlotFitsAnywhere(i % 3);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0), "sicak yol bellek ayirdi (" + sink + isink + ")");
        }

        [Test]
        public void Generator_AllocatesOnlyResultArray()
        {
            var gen = new Generator(PieceCatalog.All, new Pcg32(1UL));
            var g = Grid.FromBits(0x00000000FF00FF00UL);
            for (int i = 0; i < 100; i++) gen.NextTray(g, DifficultyMode.Relief);
            const int N = 10000;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < N; i++) gen.NextTray(g, (DifficultyMode)(i % 3));
            long perTray = (GC.GetAllocatedBytesForCurrentThread() - before) / N;
            Assert.That(perTray, Is.LessThanOrEqualTo(80), "tepsi basina yalniz 3'luk sonuc dizisi beklenir");
        }

        [Test]
        public void TrayGeneration_AverageUnder100Microseconds()
        {
            var boardRng = new Pcg32(8UL);
            var grids = new Grid[256];
            for (int i = 0; i < grids.Length; i++)
            {
                ulong b = RandomBits(boardRng);
                grids[i] = Grid.FromBits(b == ulong.MaxValue ? b & ~1UL : b);
            }
            var gen = new Generator(PieceCatalog.All, new Pcg32(8UL));
            for (int i = 0; i < 1000; i++) gen.NextTray(grids[i & 255], DifficultyMode.Normal);

            const int N = 20000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < N; i++) gen.NextTray(grids[i & 255], (DifficultyMode)(i % 3));
            sw.Stop();
            double avgMs = sw.Elapsed.TotalMilliseconds / N;
            TestContext.Out.WriteLine("Ortalama tepsi uretimi: " + (avgMs * 1000).ToString("F2") + " us");
            Assert.That(avgMs, Is.LessThan(0.1));
        }
    }
}
