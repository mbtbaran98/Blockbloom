using System;
using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    /// <summary>GDD §8 "Capa hesabi".</summary>
    public class DropAnchorTests
    {
        private static readonly PieceDef M1 = PieceCatalog.Get("M1");
        private static readonly PieceDef O4 = PieceCatalog.Get("O4");

        [Test]
        public void ExactCellPosition_SelectsThatCell()
        {
            Assert.That(DropAnchor.TryFind(new Grid(), M1, 3f, 5f, 0.75f, out Cell a), Is.True);
            Assert.That(a, Is.EqualTo(new Cell(3, 5)));
        }

        [Test]
        public void Fractional_SelectsNearestCandidate()
        {
            Assert.That(DropAnchor.TryFind(new Grid(), M1, 3.3f, 5.6f, 0.75f, out Cell a), Is.True);
            Assert.That(a, Is.EqualTo(new Cell(3, 6)));
        }

        [Test]
        public void NearestBlocked_FallsBackToNextValidCandidate()
        {
            Grid g = Grid.FromBits(Bit(3, 6));
            Assert.That(DropAnchor.TryFind(g, M1, 3.3f, 5.6f, 0.75f, out Cell a), Is.True);
            // Kalan adaylar: (3,5) d2=0.09+0.36=0.45, (4,6) d2=0.49+0.16=0.65, (4,5) d2=0.49+0.36=0.85
            Assert.That(a, Is.EqualTo(new Cell(3, 5)));
        }

        [Test]
        public void Tie_PrefersSmallerY_ThenSmallerX()
        {
            // Tam orta: dort aday esit uzaklikta -> (2,2)
            Assert.That(DropAnchor.TryFind(new Grid(), M1, 2.5f, 2.5f, 0.75f, out Cell a), Is.True);
            Assert.That(a, Is.EqualTo(new Cell(2, 2)));
            // (2,2) dolu: (3,2) ile (2,3) esit -> kucuk y -> (3,2)
            Assert.That(DropAnchor.TryFind(Grid.FromBits(Bit(2, 2)), M1, 2.5f, 2.5f, 0.75f, out a), Is.True);
            Assert.That(a, Is.EqualTo(new Cell(3, 2)));
            // (2,2),(3,2) dolu: (2,3) ile (3,3) esit -> kucuk x -> (2,3)
            Assert.That(DropAnchor.TryFind(Grid.FromBits(Bit(2, 2) | Bit(3, 2)), M1, 2.5f, 2.5f, 0.75f, out a), Is.True);
            Assert.That(a, Is.EqualTo(new Cell(2, 3)));
        }

        [Test]
        public void BeyondThreshold_NoAnchor()
        {
            // Yalniz (4,4) gecerli aday; uzaklik sqrt(0.6^2+0.6^2)=0.85 > 0.75
            ulong bits = Bit(3, 3) | Bit(4, 3) | Bit(3, 4);
            Assert.That(DropAnchor.TryFind(Grid.FromBits(bits), M1, 3.4f, 3.4f, 0.75f, out _), Is.False);
            // Uzaklik 0.7 (yatay) <= 0.75 -> gecerli
            Assert.That(DropAnchor.TryFind(Grid.FromBits(Bit(3, 3)), M1, 3.3f, 3f, 0.75f, out Cell a), Is.True);
            Assert.That(a, Is.EqualTo(new Cell(4, 3)));
        }

        [Test]
        public void OffBoard_NoAnchor_ButEdgeSnapsInside()
        {
            Assert.That(DropAnchor.TryFind(new Grid(), M1, -2f, 3f, 0.75f, out _), Is.False);
            Assert.That(DropAnchor.TryFind(new Grid(), M1, 3f, 1000f, 0.75f, out _), Is.False);
            Assert.That(DropAnchor.TryFind(new Grid(), M1, float.NaN, 0f, 0.75f, out _), Is.False);
            // O4 genislik 2: x=6.4 -> adaylar 6 (gecerli) ve 7 (tahta disi)
            Assert.That(DropAnchor.TryFind(new Grid(), O4, 6.4f, 0f, 0.75f, out Cell a), Is.True);
            Assert.That(a, Is.EqualTo(new Cell(6, 0)));
            // Hafif tasma: -0.3 -> 0
            Assert.That(DropAnchor.TryFind(new Grid(), O4, -0.3f, -0.2f, 0.75f, out a), Is.True);
            Assert.That(a, Is.EqualTo(new Cell(0, 0)));
        }

        [Test]
        public void ResultAlwaysSatisfiesCanPlace_Randomized()
        {
            var rng = new Pcg32(5);
            for (int i = 0; i < 2000; i++)
            {
                Grid g = Grid.FromBits(((ulong)rng.NextUInt() << 32 | rng.NextUInt()) & ((ulong)rng.NextUInt() << 32 | rng.NextUInt()));
                PieceDef p = PieceCatalog.All[rng.NextInt(PieceCatalog.All.Count)];
                float fx = rng.NextInt(1000) / 100f - 1f, fy = rng.NextInt(1000) / 100f - 1f;
                if (DropAnchor.TryFind(g, p, fx, fy, 0.75f, out Cell a))
                {
                    Assert.That(g.CanPlace(p, a.X, a.Y), Is.True);
                    double d = Math.Sqrt((a.X - fx) * (a.X - fx) + (a.Y - fy) * (a.Y - fy));
                    Assert.That(d, Is.LessThanOrEqualTo(0.75 + 1e-6));
                }
            }
        }

        [Test]
        public void DoesNotAllocate()
        {
            var g = new Grid();
            DropAnchor.TryFind(g, O4, 1.2f, 2.7f, 0.75f, out _);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) DropAnchor.TryFind(g, O4, i % 9 - 0.4f, 2.7f, 0.75f, out _);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.EqualTo(0));
        }

        [Test]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => DropAnchor.TryFind(null, M1, 0, 0, 0.75f, out _));
            Assert.Throws<ArgumentNullException>(() => DropAnchor.TryFind(new Grid(), null, 0, 0, 0.75f, out _));
        }
    }
}
