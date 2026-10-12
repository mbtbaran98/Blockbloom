using Blockbloom.Game;
using NUnit.Framework;
using UnityEngine;

namespace Blockbloom.Tests.EditMode
{
    public class PaletteAndLayoutTests
    {
        [TestCase(1, 0x19, 0x9F, 0xEF)] // mavi
        [TestCase(2, 0xF2, 0x43, 0x3B)] // kirmizi
        [TestCase(3, 0xFD, 0xDA, 0x23)] // sari
        [TestCase(4, 0x76, 0xCF, 0x36)] // yesil
        [TestCase(5, 0xEA, 0x56, 0xA7)] // pembe
        [TestCase(6, 0xAD, 0x48, 0xE3)] // mor
        public void TileColor_MatchesClaudeMdPalette(int id, int r, int g, int b)
        {
            Color32 c = Palette.Tile(id);
            Assert.That(c.r, Is.EqualTo(r));
            Assert.That(c.g, Is.EqualTo(g));
            Assert.That(c.b, Is.EqualTo(b));
            Assert.That(c.a, Is.EqualTo(255));
        }

        [Test]
        public void Background_IsClaudeMdZemin()
        {
            Assert.That(Palette.Background, Is.EqualTo(new Color32(0x18, 0x11, 0x4C, 0xFF)));
        }

        [Test]
        public void TileColor_OutOfRange_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Palette.Tile(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Palette.Tile(7));
        }

        [Test]
        public void SpriteFactory_ProducesSpritesForAllColors()
        {
            for (int i = 1; i <= 6; i++) Assert.That(SpriteFactory.TileSprite(i), Is.Not.Null);
            Assert.That(SpriteFactory.TileSprite(1).texture, Is.SameAs(SpriteFactory.Empty.texture), "tek atlas");
            Assert.That(SpriteFactory.TileSprite(1).bounds.size.x, Is.EqualTo(1f).Within(1e-4f), "1 karo = 1 hucre");
        }

        [TestCase(1080, 1920, false)]
        [TestCase(1170, 2532, false)]
        [TestCase(1920, 1080, true)]
        [TestCase(2400, 1080, true)]
        [TestCase(1536, 2048, false)]
        public void Layout_FitsInsideSafeArea(int w, int h, bool expectLandscape)
        {
            var safe = new Rect(0, 0, w, h);
            Layout l = Layout.Choose(safe);
            Assert.That(l.Landscape, Is.EqualTo(expectLandscape));
            l.FitCamera(w, h, safe, out float size, out Vector2 cam);
            float viewH = size * 2f, viewW = viewH * w / h;
            var view = new Rect(cam.x - viewW / 2f, cam.y - viewH / 2f, viewW, viewH);
            Assert.That(view.xMin, Is.LessThanOrEqualTo(l.Bounds.xMin + 1e-3f));
            Assert.That(view.xMax, Is.GreaterThanOrEqualTo(l.Bounds.xMax - 1e-3f));
            Assert.That(view.yMin, Is.LessThanOrEqualTo(l.Bounds.yMin + 1e-3f));
            Assert.That(view.yMax, Is.GreaterThanOrEqualTo(l.Bounds.yMax - 1e-3f));
        }

        [Test]
        public void Layout_RespectsNotchSafeArea()
        {
            // Ustte 132 px centik, altta 102 px ev cubugu
            int w = 1170, h = 2532;
            var safe = new Rect(0, 102, w, h - 234);
            Layout l = Layout.Choose(safe);
            l.FitCamera(w, h, safe, out float size, out Vector2 cam);
            float ppu = h / (2f * size);
            // Yerlesimin ust kenari guvenli alanin icinde kalmali
            float topPx = (l.Bounds.yMax - cam.y) * ppu + h / 2f;
            float bottomPx = (l.Bounds.yMin - cam.y) * ppu + h / 2f;
            Assert.That(topPx, Is.LessThanOrEqualTo(safe.yMax + 0.5f));
            Assert.That(bottomPx, Is.GreaterThanOrEqualTo(safe.yMin - 0.5f));
        }

        [Test]
        public void Portrait_SlotTouchRects_SplitScreenWidthInThirds()
        {
            Layout l = Layout.Portrait();
            var view = new Rect(-6f, -9f, 12f, 18f);
            for (int i = 0; i < 3; i++)
            {
                Rect r = l.SlotTouchRect(i, view);
                Assert.That(r.width, Is.EqualTo(4f).Within(1e-4f));
                Assert.That(r.Contains(l.SlotCenter(i)), Is.True);
            }
        }
    }
}
