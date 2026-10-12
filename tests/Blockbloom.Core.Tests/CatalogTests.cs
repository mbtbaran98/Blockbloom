using System;
using System.Collections.Generic;
using System.Linq;
using Blockbloom.Core;
using NUnit.Framework;

namespace Blockbloom.Core.Tests
{
    public class CatalogTests
    {
        // GDD §4 tablosunun birebir kopyasi: kimlik | agirlik | sinif | hucreler (x,y; ...)
        private static readonly string[] Expected =
        {
            "M1|4|K|0,0",
            "I2H|6|K|0,0;1,0",
            "I2V|6|K|0,0;0,1",
            "I3H|6|K|0,0;1,0;2,0",
            "I3V|6|K|0,0;0,1;0,2",
            "V3A|4|K|0,0;1,0;0,1",
            "V3B|4|K|0,0;1,0;1,1",
            "V3C|4|K|0,0;0,1;1,1",
            "V3D|4|K|1,0;0,1;1,1",
            "I4H|5|O|0,0;1,0;2,0;3,0",
            "I4V|5|O|0,0;0,1;0,2;0,3",
            "O4|8|O|0,0;1,0;0,1;1,1",
            "T4U|3|O|1,0;0,1;1,1;2,1",
            "T4D|3|O|0,0;1,0;2,0;1,1",
            "T4L|3|O|1,0;0,1;1,1;1,2",
            "T4R|3|O|0,0;0,1;1,1;0,2",
            "S4H|2|O|1,0;2,0;0,1;1,1",
            "S4V|2|O|0,0;0,1;1,1;1,2",
            "Z4H|2|O|0,0;1,0;1,1;2,1",
            "Z4V|2|O|1,0;0,1;1,1;0,2",
            "L4A|2|O|0,0;0,1;0,2;1,2",
            "L4B|2|O|0,0;1,0;2,0;0,1",
            "L4C|2|O|0,0;1,0;1,1;1,2",
            "L4D|2|O|2,0;0,1;1,1;2,1",
            "J4A|2|O|1,0;1,1;0,2;1,2",
            "J4B|2|O|0,0;0,1;1,1;2,1",
            "J4C|2|O|0,0;1,0;0,1;0,2",
            "J4D|2|O|0,0;1,0;2,0;2,1",
            "I5H|4|B|0,0;1,0;2,0;3,0;4,0",
            "I5V|4|B|0,0;0,1;0,2;0,3;0,4",
            "V5A|2|B|0,0;1,0;2,0;0,1;0,2",
            "V5B|2|B|0,0;1,0;2,0;2,1;2,2",
            "V5C|2|B|0,0;0,1;0,2;1,2;2,2",
            "V5D|2|B|2,0;2,1;0,2;1,2;2,2",
            "R6H|3|B|0,0;1,0;2,0;0,1;1,1;2,1",
            "R6V|3|B|0,0;1,0;0,1;1,1;0,2;1,2",
            "O9|4|B|0,0;1,0;2,0;0,1;1,1;2,1;0,2;1,2;2,2",
        };

        [Test]
        public void Catalog_Has37Pieces_TotalWeight124()
        {
            Assert.That(PieceCatalog.All.Count, Is.EqualTo(37));
            Assert.That(PieceCatalog.TotalBaseWeight, Is.EqualTo(124));
            Assert.That(PieceCatalog.All.Sum(p => p.BaseWeight), Is.EqualTo(124));
        }

        [Test]
        public void Catalog_MatchesGddTableExactly_InOrder()
        {
            Assert.That(PieceCatalog.All.Count, Is.EqualTo(Expected.Length));
            for (int i = 0; i < Expected.Length; i++)
            {
                string[] parts = Expected[i].Split('|');
                PieceDef p = PieceCatalog.All[i];
                Assert.That(p.Id, Is.EqualTo(parts[0]), "sira " + i);
                Assert.That(p.BaseWeight, Is.EqualTo(int.Parse(parts[1])), p.Id);
                SizeClass expectedSize = parts[2] == "K" ? SizeClass.Small : parts[2] == "O" ? SizeClass.Medium : SizeClass.Large;
                Assert.That(p.Size, Is.EqualTo(expectedSize), p.Id);
                Cell[] cells = parts[3].Split(';').Select(s => { var xy = s.Split(','); return new Cell(int.Parse(xy[0]), int.Parse(xy[1])); }).ToArray();
                Assert.That(p.Cells, Is.EqualTo(cells), p.Id);
            }
        }

        [Test]
        public void Catalog_IdsUnique_AndGetWorks()
        {
            var ids = PieceCatalog.All.Select(p => p.Id).ToList();
            Assert.That(ids, Is.Unique);
            foreach (PieceDef p in PieceCatalog.All) Assert.That(PieceCatalog.Get(p.Id), Is.SameAs(p));
            Assert.Throws<ArgumentException>(() => PieceCatalog.Get("X99"));
            Assert.Throws<ArgumentException>(() => PieceCatalog.Get(null));
        }

        [Test]
        public void Catalog_CellsNormalized_UniqueAndConsistent()
        {
            foreach (PieceDef p in PieceCatalog.All)
            {
                Assert.That(p.Cells.Min(c => c.X), Is.EqualTo(0), p.Id);
                Assert.That(p.Cells.Min(c => c.Y), Is.EqualTo(0), p.Id);
                Assert.That(p.Width, Is.EqualTo(p.Cells.Max(c => c.X) + 1), p.Id);
                Assert.That(p.Height, Is.EqualTo(p.Cells.Max(c => c.Y) + 1), p.Id);
                Assert.That(p.Cells, Is.Unique, p.Id);
                Assert.That(p.CellCount, Is.EqualTo(p.Cells.Count), p.Id);
                SizeClass expected = p.CellCount <= 3 ? SizeClass.Small : p.CellCount == 4 ? SizeClass.Medium : SizeClass.Large;
                Assert.That(p.Size, Is.EqualTo(expected), p.Id);
            }
        }

        [Test]
        public void Catalog_PiecesAre4Connected()
        {
            foreach (PieceDef p in PieceCatalog.All)
            {
                var set = new HashSet<Cell>(p.Cells);
                var seen = new HashSet<Cell> { p.Cells[0] };
                var stack = new Stack<Cell>();
                stack.Push(p.Cells[0]);
                while (stack.Count > 0)
                {
                    Cell c = stack.Pop();
                    foreach (var n in new[] { new Cell(c.X + 1, c.Y), new Cell(c.X - 1, c.Y), new Cell(c.X, c.Y + 1), new Cell(c.X, c.Y - 1) })
                        if (set.Contains(n) && seen.Add(n)) stack.Push(n);
                }
                Assert.That(seen.Count, Is.EqualTo(p.CellCount), p.Id + " bagli degil");
            }
        }

        [Test]
        public void Catalog_ClassSharesAndExpectedCells_MatchGdd()
        {
            Assert.That(PieceCatalog.All.Where(p => p.Size == SizeClass.Small).Sum(p => p.BaseWeight), Is.EqualTo(44));
            Assert.That(PieceCatalog.All.Where(p => p.Size == SizeClass.Medium).Sum(p => p.BaseWeight), Is.EqualTo(54));
            Assert.That(PieceCatalog.All.Where(p => p.Size == SizeClass.Large).Sum(p => p.BaseWeight), Is.EqualTo(26));
            Assert.That(PieceCatalog.All.Sum(p => p.BaseWeight * p.CellCount), Is.EqualTo(480));
        }

        [Test]
        public void Catalog_NoTwoPiecesHaveSameShape()
        {
            var shapes = PieceCatalog.All.Select(p => p.MaskAt(0, 0)).ToList();
            Assert.That(shapes, Is.Unique);
        }

        [Test]
        public void MaskAt_MatchesCells_AndRejectsOutOfBoard()
        {
            PieceDef t = PieceCatalog.Get("T4U");
            ulong expected = TestUtil.Bit(3, 4) | TestUtil.Bit(2, 5) | TestUtil.Bit(3, 5) | TestUtil.Bit(4, 5);
            Assert.That(t.MaskAt(2, 4), Is.EqualTo(expected));
            Assert.That(t.MaskAt(5, 6), Is.Not.EqualTo(0UL));
            Assert.Throws<ArgumentOutOfRangeException>(() => t.MaskAt(6, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => t.MaskAt(0, 7));
            Assert.Throws<ArgumentOutOfRangeException>(() => t.MaskAt(-1, 0));
        }

        [Test]
        public void PieceDef_RejectsInvalidShapes()
        {
            Assert.Throws<ArgumentException>(() => new PieceDef("X", 1, new Cell(1, 0)));
            Assert.Throws<ArgumentException>(() => new PieceDef("X", 1, new Cell(0, 0), new Cell(0, 0)));
            Assert.Throws<ArgumentException>(() => new PieceDef("X", 1));
            Assert.Throws<ArgumentException>(() => new PieceDef("X", 1, new Cell(0, 0), new Cell(8, 0)));
        }
    }
}
