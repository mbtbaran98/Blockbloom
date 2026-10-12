using System;
using System.Linq;
using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    public class GridTests
    {
        private static PieceDef P(string id) => PieceCatalog.Get(id);

        [Test]
        public void NewGrid_IsEmpty()
        {
            var g = new Grid();
            Assert.That(g.IsEmpty, Is.True);
            Assert.That(g.FilledCount, Is.EqualTo(0));
            Assert.That(g.Bits, Is.EqualTo(0UL));
            Assert.That(g.ColorAt(7, 7), Is.EqualTo(0));
        }

        [Test]
        public void CanPlace_Bounds()
        {
            var g = new Grid();
            Assert.That(g.CanPlace(P("I5H"), 3, 0), Is.True);
            Assert.That(g.CanPlace(P("I5H"), 4, 0), Is.False);
            Assert.That(g.CanPlace(P("I5H"), 0, 7), Is.True);
            Assert.That(g.CanPlace(P("I5H"), 0, 8), Is.False);
            Assert.That(g.CanPlace(P("I5V"), 7, 3), Is.True);
            Assert.That(g.CanPlace(P("I5V"), 7, 4), Is.False);
            Assert.That(g.CanPlace(P("O9"), 5, 5), Is.True);
            Assert.That(g.CanPlace(P("O9"), 6, 5), Is.False);
            Assert.That(g.CanPlace(P("O9"), 5, 6), Is.False);
            Assert.That(g.CanPlace(P("M1"), -1, 0), Is.False);
            Assert.That(g.CanPlace(P("M1"), 0, -1), Is.False);
            Assert.That(g.CanPlace(P("M1"), 7, 7), Is.True);
            Assert.That(g.CanPlace(P("M1"), int.MinValue, int.MaxValue), Is.False);
            // V3D'nin (0,0) hucresi bos olsa da sinir kutusu tahta icinde olmali
            Assert.That(g.CanPlace(P("V3D"), 6, 6), Is.True);
            Assert.That(g.CanPlace(P("V3D"), 7, 6), Is.False);
        }

        [Test]
        public void CanPlace_Occupancy_OnlyPieceCellsMatter()
        {
            // V3D'nin bos kosesi (0,0) dolu hucreye denk gelebilir
            var g = Grid.FromBits(Bit(2, 2));
            Assert.That(g.CanPlace(P("V3D"), 2, 2), Is.True);
            Assert.That(g.CanPlace(P("V3A"), 2, 2), Is.False);
            Assert.That(g.CanPlace(P("O4"), 1, 1), Is.False);
            Assert.That(g.CanPlace(P("O4"), 3, 3), Is.True);
        }

        [Test]
        public void Place_FillsCellsWithColor_NoClear()
        {
            var g = new Grid();
            var r = g.Place(P("L4A"), 2, 3, 5);
            Assert.That(r.CellsPlaced, Is.EqualTo(4));
            Assert.That(r.LineCount, Is.EqualTo(0));
            Assert.That(r.CellsCleared, Is.EqualTo(0));
            Assert.That(r.BoardCleared, Is.False);
            Assert.That(g.FilledCount, Is.EqualTo(4));
            Assert.That(g.ColorAt(2, 3), Is.EqualTo(5));
            Assert.That(g.ColorAt(3, 5), Is.EqualTo(5));
            Assert.That(g.ColorAt(3, 3), Is.EqualTo(0));
            Assert.That(g.Bits, Is.EqualTo(Bit(2, 3) | Bit(2, 4) | Bit(2, 5) | Bit(3, 5)));
        }

        [Test]
        public void Place_Invalid_Throws_AndDoesNotChange()
        {
            var g = Grid.FromBits(Bit(0, 0));
            Assert.Throws<InvalidOperationException>(() => g.Place(P("I2H"), 0, 0, 1));
            Assert.Throws<InvalidOperationException>(() => g.Place(P("I2H"), 7, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.Place(P("M1"), 3, 3, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => g.Place(P("M1"), 3, 3, 7));
            Assert.That(g.Bits, Is.EqualTo(Bit(0, 0)));
        }

        [Test]
        public void Place_SingleRow_Clears()
        {
            var g = Grid.FromBits(Row(4) & ~(Bit(0, 4) | Bit(1, 4) | Bit(2, 4)) | Bit(0, 3));
            var r = g.Place(P("I3H"), 0, 4, 2);
            Assert.That(r.ClearedRows, Is.EqualTo(new[] { 4 }));
            Assert.That(r.ClearedCols, Is.Empty);
            Assert.That(r.LineCount, Is.EqualTo(1));
            Assert.That(r.CellsCleared, Is.EqualTo(8));
            Assert.That(r.BoardCleared, Is.False);
            Assert.That(g.Bits, Is.EqualTo(Bit(0, 3)));
            Assert.That(g.ColorAt(1, 4), Is.EqualTo(0));
        }

        [Test]
        public void Place_RowAndColumnSimultaneously_IntersectionClearedOnce()
        {
            // satir 0 ve sutun 0 tek eksik hucresi (0,0)
            var g = Grid.FromBits((Row(0) | Col(0)) & ~Bit(0, 0) | Bit(5, 5));
            var r = g.Place(P("M1"), 0, 0, 3);
            Assert.That(r.ClearedRows, Is.EqualTo(new[] { 0 }));
            Assert.That(r.ClearedCols, Is.EqualTo(new[] { 0 }));
            Assert.That(r.LineCount, Is.EqualTo(2));
            Assert.That(r.CellsCleared, Is.EqualTo(15));
            Assert.That(r.ClearedCells.Distinct().Count(), Is.EqualTo(15));
            Assert.That(r.ClearedCells.Count(c => c.X == 0 && c.Y == 0), Is.EqualTo(1));
            Assert.That(g.Bits, Is.EqualTo(Bit(5, 5)));
        }

        [Test]
        public void Place_TwoRowsTwoCols_CellsClearedFormula()
        {
            // satir 2,3 ve sutun 4,5; O4 (4,2) eksik dortluyu tamamlar
            ulong full = Row(2) | Row(3) | Col(4) | Col(5);
            ulong hole = Bit(4, 2) | Bit(5, 2) | Bit(4, 3) | Bit(5, 3);
            var g = Grid.FromBits(full & ~hole);
            var r = g.Place(P("O4"), 4, 2, 1);
            Assert.That(r.ClearedRows, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(r.ClearedCols, Is.EqualTo(new[] { 4, 5 }));
            Assert.That(r.LineCount, Is.EqualTo(4));
            Assert.That(r.CellsCleared, Is.EqualTo(8 * 2 + 8 * 2 - 2 * 2));
            Assert.That(r.BoardCleared, Is.True);
            Assert.That(g.IsEmpty, Is.True);
        }

        [Test]
        public void ClearedCells_AscendingIndexOrder()
        {
            var g = Grid.FromBits((Row(6) | Col(1)) & ~Bit(1, 6));
            var r = g.Place(P("M1"), 1, 6, 1);
            var idx = r.ClearedCells.Select(c => c.Index).ToList();
            Assert.That(idx, Is.Ordered.Ascending);
            Assert.That(idx.Count, Is.EqualTo(15));
        }

        [Test]
        public void PreviewLines_MatchesPlace_AndDoesNotModify()
        {
            var rng = new Pcg32(5UL);
            for (int trial = 0; trial < 2000; trial++)
            {
                ulong bits = ((ulong)rng.NextUInt() << 32 | rng.NextUInt()) | ((ulong)rng.NextUInt() << 32 | rng.NextUInt());
                var g = Grid.FromBits(bits);
                PieceDef p = PieceCatalog.All[rng.NextInt(37)];
                int ax = rng.NextInt(8), ay = rng.NextInt(8);
                LinePreview pv = g.PreviewLines(p, ax, ay);
                Assert.That(g.Bits, Is.EqualTo(bits));
                if (!g.CanPlace(p, ax, ay))
                {
                    Assert.That(pv.LineCount, Is.EqualTo(0));
                    continue;
                }
                var r = g.Place(p, ax, ay, 1);
                Assert.That(pv.Rows, Is.EqualTo(r.ClearedRows));
                Assert.That(pv.Cols, Is.EqualTo(r.ClearedCols));
                Assert.That(pv.LineCount, Is.EqualTo(r.LineCount));
                int nr = r.ClearedRows.Count, nc = r.ClearedCols.Count;
                Assert.That(r.CellsCleared, Is.EqualTo(8 * nr + 8 * nc - nr * nc));
            }
        }

        [Test]
        public void PreviewLines_Contains()
        {
            var g = Grid.FromBits(Row(3) & ~Bit(7, 3));
            LinePreview pv = g.PreviewLines(P("M1"), 7, 3);
            Assert.That(pv.LineCount, Is.EqualTo(1));
            Assert.That(pv.Contains(0, 3), Is.True);
            Assert.That(pv.Contains(0, 2), Is.False);
            Assert.That(pv.RowMask, Is.EqualTo(1 << 3));
        }

        [Test]
        public void ValidAnchors_OrderAndCount()
        {
            var g = Grid.FromBits(Bit(3, 3));
            var anchors = g.ValidAnchors(P("O4")).ToList();
            Assert.That(anchors.Count, Is.EqualTo(49 - 4));
            for (int i = 1; i < anchors.Count; i++)
            {
                Assert.That(anchors[i].Index, Is.GreaterThan(anchors[i - 1].Index));
            }
            Assert.That(anchors.All(a => g.CanPlace(P("O4"), a.X, a.Y)), Is.True);
        }

        [Test]
        public void FitsAnywhere_MatchesBruteForce()
        {
            var rng = new Pcg32(77UL);
            for (int trial = 0; trial < 500; trial++)
            {
                ulong bits = ((ulong)rng.NextUInt() << 32 | rng.NextUInt()) | ((ulong)rng.NextUInt() << 32 | rng.NextUInt());
                var g = Grid.FromBits(bits);
                foreach (PieceDef p in PieceCatalog.All)
                {
                    bool brute = false;
                    for (int y = -2; y < 10 && !brute; y++)
                        for (int x = -2; x < 10 && !brute; x++)
                            if (g.CanPlace(p, x, y)) brute = true;
                    Assert.That(g.FitsAnywhere(p), Is.EqualTo(brute));
                }
            }
        }

        [Test]
        public void Checkerboard_OnlyM1Fits()
        {
            var g = Grid.FromBits(Checkerboard());
            foreach (PieceDef p in PieceCatalog.All)
                Assert.That(g.FitsAnywhere(p), Is.EqualTo(p.Id == "M1"), p.Id);
        }

        [Test]
        public void Clone_IsIndependent()
        {
            var g = new Grid();
            g.Place(P("O4"), 0, 0, 2);
            Grid c = g.Clone();
            c.Place(P("M1"), 5, 5, 3);
            Assert.That(g.FilledCount, Is.EqualTo(4));
            Assert.That(c.FilledCount, Is.EqualTo(5));
            Assert.That(c.ColorAt(0, 0), Is.EqualTo(2));
        }

        [Test]
        public void FromRaw_RejectsInconsistentColors()
        {
            var colors = new byte[64];
            Assert.Throws<ArgumentException>(() => Grid.FromRaw(1UL, colors));
            colors[1] = 2;
            Assert.Throws<ArgumentException>(() => Grid.FromRaw(0UL, colors));
            colors[1] = 0;
            colors[0] = 9;
            Assert.Throws<ArgumentException>(() => Grid.FromRaw(1UL, colors));
        }
    }
}
