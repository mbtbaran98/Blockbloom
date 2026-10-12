using Blockbloom.Game;
using NUnit.Framework;
using UnityEngine;

namespace Blockbloom.Tests.EditMode
{
    /// <summary>GDD §7.4 ana menu yerlesimi: portre/yatay, guvenli alan, logo %85 siniri, dugme dokunma alani.</summary>
    public class MenuLayoutTests
    {
        private const float LogoAspect = 1024f / 572f;
        private const float Eps = 1e-3f;

        /// <summary>Oyunun kamerasiyla ayni hesap: ekran -> (gorus, guvenli alan) dunya dikdortgenleri.</summary>
        private static void World(int w, int h, Rect safePx, out Rect view, out Rect safe, out float ppu)
        {
            Layout l = Layout.Choose(safePx);
            l.FitCamera(w, h, safePx, out float size, out Vector2 cam);
            ppu = h / (2f * size);
            float vh = size * 2f, vw = vh * w / h;
            view = new Rect(cam.x - vw / 2f, cam.y - vh / 2f, vw, vh);
            safe = MenuLayout.ScreenToWorldRect(safePx, w, h, cam, ppu);
        }

        private static void AssertInside(Rect inner, Rect outer, string what)
        {
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - Eps), what + " sol");
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + Eps), what + " sag");
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - Eps), what + " alt");
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + Eps), what + " ust");
        }

        [TestCase(1080, 1920, 0, 0, false)]
        [TestCase(1170, 2532, 102, 132, false)] // centik + ev cubugu
        [TestCase(1536, 2048, 0, 0, false)]
        [TestCase(1920, 1080, 0, 0, true)]
        [TestCase(2400, 1080, 0, 0, true)]
        [TestCase(720, 1280, 0, 0, false)]
        public void Menu_FitsSafeArea_LogoWithin85Percent_ButtonsTouchable(int w, int h, int bottomInset, int topInset, bool landscape)
        {
            var safePx = new Rect(0, bottomInset, w, h - bottomInset - topInset);
            World(w, h, safePx, out Rect view, out Rect safe, out float ppu);
            float minTouch = TouchTarget.MinPixels(420f, Mathf.Min(w, h)) / ppu;

            foreach (bool hasSave in new[] { false, true })
            {
                foreach (bool showRecord in new[] { false, true })
                {
                    MenuLayout m = MenuLayout.Compute(safe, view.width, LogoAspect, showRecord, hasSave, minTouch);
                    Assert.That(m.Landscape, Is.EqualTo(landscape));
                    Assert.That(m.Logo.width, Is.GreaterThan(0f));
                    Assert.That(m.Logo.width, Is.LessThanOrEqualTo(0.85f * view.width + Eps), "logo <= %85 ekran genisligi");
                    Assert.That(m.Logo.width / m.Logo.height, Is.EqualTo(LogoAspect).Within(1e-3f), "en-boy korunur");
                    AssertInside(m.Logo, safe, "logo");
                    AssertInside(m.NewGame, safe, "yeni oyun");
                    Assert.That(m.NewGame.height, Is.GreaterThanOrEqualTo(minTouch - Eps));
                    Assert.That(m.NewGame.width, Is.GreaterThanOrEqualTo(minTouch - Eps));
                    if (hasSave)
                    {
                        AssertInside(m.Continue, safe, "devam et");
                        Assert.That(m.Continue.yMin, Is.GreaterThan(m.NewGame.yMax), "Devam et, Yeni oyun'un ustunde");
                    }
                    if (showRecord)
                    {
                        AssertInside(m.Record, safe, "rekor");
                        Assert.That(m.Record.yMin, Is.GreaterThanOrEqualTo((hasSave ? m.Continue.yMax : m.NewGame.yMax) - Eps));
                        if (!landscape) Assert.That(m.Record.yMax, Is.LessThanOrEqualTo(m.Logo.yMin + Eps), "rekor logonun altinda");
                    }
                    if (!landscape)
                        Assert.That(m.NewGame.yMax, Is.LessThan(m.Logo.yMin), "portre: logo ustte");
                    else
                        Assert.That(m.Logo.xMax, Is.LessThanOrEqualTo(m.NewGame.xMin + Eps), "yatay: logo solda");
                }
            }
        }

        [Test]
        public void NoLogo_FallsBackToTitleAspect()
        {
            var safe = new Rect(-6f, -9f, 12f, 18f);
            MenuLayout m = MenuLayout.Compute(safe, 12f, 0f, false, false, 1f);
            Assert.That(m.Logo.width / m.Logo.height, Is.EqualTo(MenuLayout.TitleAspect).Within(1e-3f));
        }

        [Test]
        public void HiddenRows_HaveZeroSize()
        {
            var safe = new Rect(-6f, -9f, 12f, 18f);
            MenuLayout m = MenuLayout.Compute(safe, 12f, LogoAspect, false, false, 1f);
            Assert.That(m.Continue.width, Is.EqualTo(0f));
            Assert.That(m.Record.height, Is.EqualTo(0f));
        }

        [Test]
        public void ScreenToWorldRect_FullScreen_EqualsView()
        {
            int w = 1080, h = 1920;
            var full = new Rect(0, 0, w, h);
            World(w, h, full, out Rect view, out Rect safe, out _);
            Assert.That(safe.xMin, Is.EqualTo(view.xMin).Within(Eps));
            Assert.That(safe.yMin, Is.EqualTo(view.yMin).Within(Eps));
            Assert.That(safe.width, Is.EqualTo(view.width).Within(Eps));
            Assert.That(safe.height, Is.EqualTo(view.height).Within(Eps));
        }

        [Test]
        public void TouchRect_GrowsSmallVisualAroundCenter()
        {
            var visual = new Rect(0f, 0f, 0.5f, 2f);
            Rect t = MenuLayout.TouchRect(visual, 1f);
            Assert.That(t.width, Is.EqualTo(1f).Within(Eps));
            Assert.That(t.height, Is.EqualTo(2f).Within(Eps));
            Assert.That(t.center.x, Is.EqualTo(visual.center.x).Within(Eps));
            Assert.That(t.center.y, Is.EqualTo(visual.center.y).Within(Eps));
        }

        [TestCase(1080, 1920)]
        [TestCase(1920, 1080)]
        public void HudMenuButton_TouchAreaAtLeast44pt_AndDoesNotOverlapTraySlots(int w, int h)
        {
            var safePx = new Rect(0, 0, w, h);
            World(w, h, safePx, out Rect view, out _, out float ppu);
            Layout l = Layout.Choose(safePx);
            float minTouch = TouchTarget.MinPixels(0f, Mathf.Min(w, h)) / ppu; // DPI bilinmiyor
            Rect visual = new Rect(l.Hud.xMin, l.Hud.center.y - HudView.MenuButtonSize / 2f, HudView.MenuButtonSize, HudView.MenuButtonSize);
            Rect touch = MenuLayout.TouchRect(visual, minTouch);
            Assert.That(touch.width * ppu, Is.GreaterThanOrEqualTo(TouchTarget.MinPixels(0f, Mathf.Min(w, h)) - 0.5f));
            for (int i = 0; i < 3; i++) Assert.That(touch.Overlaps(l.SlotTouchRect(i, view)), Is.False);
            Assert.That(touch.yMin, Is.GreaterThanOrEqualTo(Layout.BoardHalf - Eps), "tahtanin ustunde");
        }

        /// <summary>Logo Resources altinda: calisma aninda Texture2D olarak yuklenir (Default ya da Sprite iceri aktarimi).</summary>
        [Test]
        public void Logo_LoadsFromResources_AsTexture2D()
        {
            Texture2D tex = Resources.Load<Texture2D>(MainMenuView.LogoResourcePath);
            Assert.That(tex, Is.Not.Null, "Assets/_Project/Resources/Branding/logo.png");
            Assert.That((float)tex.width / tex.height, Is.EqualTo(LogoAspect).Within(0.01f));
        }
    }
}
