using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    public class ScoringTests
    {
        private static PieceDef P(string id) => PieceCatalog.Get(id);

        /// <summary>
        /// GDD §6.4 sayisal ornekleri, tek bir ScoreKeeper ile (K adimlar arasinda tasinir).
        /// Not: ornek zinciri ayni tahtada art arda hamleler olarak geometrik olarak kurulamaz
        /// (3. adimda sutun patlarsa 4. adimdaki "iki satiri tek I2V tamamlar ve tahta bosalir" durumu olusamaz),
        /// bu yuzden her adimin tahtasi ayri kurulur; Grid.Place gercek PlacementResult uretir.
        /// </summary>
        [Test]
        public void Gdd_6_4_NumericExamples()
        {
            var sk = new ScoreKeeper();

            // 1) L4A, patlatma yok -> 4, K=0
            var g1 = new Grid();
            ScoreBreakdown b1 = sk.Apply(g1.Place(P("L4A"), 0, 0, 1));
            Assert.That(b1.Total, Is.EqualTo(4));
            Assert.That(b1.ComboAfter, Is.EqualTo(0));

            // 2) I3H bir satiri tamamlar -> 3 + 10 = 13, K=1
            var g2 = Grid.FromBits(Row(0) & ~(Bit(0, 0) | Bit(1, 0) | Bit(2, 0)) | Bit(4, 5));
            PlacementResult r2 = g2.Place(P("I3H"), 0, 0, 1);
            Assert.That(r2.BoardCleared, Is.False);
            ScoreBreakdown b2 = sk.Apply(r2);
            Assert.That(b2.PlacementPoints, Is.EqualTo(3));
            Assert.That(b2.LinePoints, Is.EqualTo(10));
            Assert.That(b2.Total, Is.EqualTo(13));
            Assert.That(b2.ComboAfter, Is.EqualTo(1));

            // 3) O4 1 satir + 1 sutun (15 hucre) -> 4 + 10*4*2 = 84, K=2
            ulong pre3 = (Row(0) | Col(0)) & ~(Bit(0, 0) | Bit(1, 0) | Bit(0, 1));
            var g3 = Grid.FromBits(pre3);
            PlacementResult r3 = g3.Place(P("O4"), 0, 0, 1);
            Assert.That(r3.BoardCleared, Is.False);
            Assert.That(r3.LineCount, Is.EqualTo(2));
            Assert.That(r3.CellsCleared, Is.EqualTo(15));
            ScoreBreakdown b3 = sk.Apply(r3);
            Assert.That(b3.Total, Is.EqualTo(84));
            Assert.That(b3.ComboAfter, Is.EqualTo(2));

            // 4) I2V alt alta 2 satir, tahta bosalir -> 2 + 10*4*3 + 300 = 422, K=3
            var g4 = Grid.FromBits((Row(6) | Row(7)) & ~(Bit(7, 6) | Bit(7, 7)));
            PlacementResult r4 = g4.Place(P("I2V"), 7, 6, 1);
            Assert.That(r4.BoardCleared, Is.True);
            ScoreBreakdown b4 = sk.Apply(r4);
            Assert.That(b4.PlacementPoints, Is.EqualTo(2));
            Assert.That(b4.LinePoints, Is.EqualTo(120));
            Assert.That(b4.PerfectClearBonus, Is.EqualTo(300));
            Assert.That(b4.Total, Is.EqualTo(422));
            Assert.That(b4.ComboAfter, Is.EqualTo(3));

            // 5) M1, patlatma yok -> 1, K=0
            ScoreBreakdown b5 = sk.Apply(new Grid().Place(P("M1"), 3, 3, 1));
            Assert.That(b5.Total, Is.EqualTo(1));
            Assert.That(b5.ComboAfter, Is.EqualTo(0));

            Assert.That(sk.Score, Is.EqualTo(524));
            Assert.That(sk.Combo, Is.EqualTo(0));
            Assert.That(sk.MaxCombo, Is.EqualTo(3));
        }

        [TestCase(1, "M1", 10)]
        [TestCase(2, "I2V", 40)]
        [TestCase(3, "I3V", 90)]
        [TestCase(4, "I4V", 160)]
        [TestCase(5, "I5V", 250)]
        [TestCase(6, "I5V", 360)]
        public void LinePoints_QuickTable_K1(int lines, string pieceId, int expected)
        {
            // Satir 0..h-1 sutun 0 haric dolu; dikey parca sutun 0'a konunca h satir patlar.
            // 6 cizgi: 5 satir + sutun 0 (sutunun 5..7 hucreleri onceden dolu).
            PieceDef p = P(pieceId);
            ulong full = 0;
            for (int y = 0; y < p.Height; y++) full |= Row(y);
            if (lines == 6) full |= Col(0);
            ulong holes = p.MaskAt(0, 0);
            var g = Grid.FromBits(full & ~holes);
            PlacementResult r = g.Place(p, 0, 0, 1);
            Assert.That(r.LineCount, Is.EqualTo(lines));
            ScoreBreakdown b = new ScoreKeeper().Apply(r);
            Assert.That(b.LinePoints, Is.EqualTo(expected));
            Assert.That(b.ComboAfter, Is.EqualTo(1));
        }

        [Test]
        public void Combo_MultiLineIncrementsOnce_ResetsOnNonClearingMove()
        {
            var sk = new ScoreKeeper();
            var g = Grid.FromBits((Row(0) | Row(1) | Row(2)) & ~(Bit(0, 0) | Bit(0, 1) | Bit(0, 2)) | Bit(5, 5));
            ScoreBreakdown b = sk.Apply(g.Place(P("I3V"), 0, 0, 1));
            Assert.That(b.ComboAfter, Is.EqualTo(1));
            Assert.That(b.LinePoints, Is.EqualTo(90));

            b = sk.Apply(g.Place(P("M1"), 0, 0, 1));
            Assert.That(b.ComboAfter, Is.EqualTo(0));
            Assert.That(b.LinePoints, Is.EqualTo(0));
            Assert.That(sk.MaxCombo, Is.EqualTo(1));
        }

        [Test]
        public void PerfectClear_NotMultipliedByCombo()
        {
            var sk = new ScoreKeeper();
            // 4 kez ust uste tek satir patlat; sonuncusu tahtayi bosaltir
            for (int k = 1; k <= 4; k++)
            {
                ulong extra = k < 4 ? Bit(0, 7) : 0UL;
                var g = Grid.FromBits(Row(0) & ~Bit(3, 0) | extra);
                ScoreBreakdown b = sk.Apply(g.Place(P("M1"), 3, 0, 1));
                Assert.That(b.ComboAfter, Is.EqualTo(k));
                Assert.That(b.LinePoints, Is.EqualTo(10 * k));
                Assert.That(b.PerfectClearBonus, Is.EqualTo(k == 4 ? 300 : 0));
            }
        }

        [Test]
        public void EmptyBoardWithoutClear_GivesNoBonus()
        {
            // Yerlestirme sonrasi tahta hic bos olamaz; BoardCleared yalniz patlatmayla gelir
            var g = new Grid();
            var r = g.Place(P("M1"), 0, 0, 1);
            Assert.That(r.BoardCleared, Is.False);
            Assert.That(new ScoreKeeper().Apply(r).PerfectClearBonus, Is.EqualTo(0));
        }
    }
}
