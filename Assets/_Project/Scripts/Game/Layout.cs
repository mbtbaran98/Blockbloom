using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// Dunya birimi yerlesimi (1 birim = 1 hucre). Tahta her zaman (0,0) merkezli: sol-ust kose (-4, 4).
    /// Portre: HUD ustte, tahta ortada, 3 yuvali tepsi altta yan yana.
    /// Yatay: HUD ustte, tahta solda, tepsi sagda ust uste.
    /// Kamera, yerlesimin tamamini ekranin guvenli alanina (Screen.safeArea) sigdirir; hangisi tahtayi buyuk
    /// gosteriyorsa o yerlesim secilir. Saf hesap; bellek ayirmaz.
    /// </summary>
    public struct Layout
    {
        public const float BoardHalf = 4f;
        public const float SlotSize = 3f;
        private const float HudHeight = 1.7f;
        private const float HudGap = 0.3f;
        private const float TrayGap = 0.45f;
        private const float Margin = 0.25f;

        public bool Landscape;
        /// <summary>Yerlesimin tamami (kameranin sigdirdigi dikdortgen).</summary>
        public Rect Bounds;
        public Rect Hud;
        /// <summary>Tepsinin gorsel alani (3 yuva).</summary>
        public Rect Tray;

        public static Vector2 BoardTopLeft => new Vector2(-BoardHalf, BoardHalf);

        /// <summary>Tahta hucresi (x saga, y asagi; Core koordinati) merkezinin dunya konumu.</summary>
        public static Vector3 CellCenter(int x, int y) => new Vector3(-BoardHalf + x + 0.5f, BoardHalf - y - 0.5f, 0f);

        /// <summary>Yuva merkezinin dunya konumu.</summary>
        public Vector2 SlotCenter(int slot)
        {
            if (Landscape) return new Vector2(Tray.center.x, Tray.yMax - SlotSize * (slot + 0.5f));
            return new Vector2(Tray.xMin + SlotSize * (slot + 0.5f), Tray.center.y);
        }

        /// <summary>Yuvanin gorsel dikdortgeni.</summary>
        public Rect SlotRect(int slot)
        {
            Vector2 c = SlotCenter(slot);
            return new Rect(c.x - SlotSize * 0.5f, c.y - SlotSize * 0.5f, SlotSize, SlotSize);
        }

        /// <summary>
        /// GDD §8 dokunma alani: yuvanin tamami. Portrede ekran genisliginin 1/3'u x tepsi yuksekligi (ekranin altina
        /// kadar uzatilir); yatayda tepsi sutunu genisligi (ekranin sagina kadar) x yuva yuksekligi.
        /// view: kameranin gordugu dunya dikdortgeni.
        /// </summary>
        public Rect SlotTouchRect(int slot, Rect view)
        {
            if (Landscape)
            {
                Rect r = SlotRect(slot);
                float xMin = Tray.xMin - TrayGap * 0.5f;
                return Rect.MinMaxRect(xMin, r.yMin, Mathf.Max(view.xMax, r.xMax), r.yMax);
            }
            float third = view.width / 3f;
            float top = Tray.yMax + TrayGap * 0.5f;
            return Rect.MinMaxRect(view.xMin + third * slot, Mathf.Min(view.yMin, Tray.yMin), view.xMin + third * (slot + 1), top);
        }

        public static Layout Portrait()
        {
            var l = new Layout();
            l.Landscape = false;
            float hudBottom = BoardHalf + HudGap;
            l.Hud = new Rect(-BoardHalf, hudBottom, BoardHalf * 2f, HudHeight);
            float trayTop = -BoardHalf - TrayGap;
            l.Tray = new Rect(-SlotSize * 1.5f, trayTop - SlotSize, SlotSize * 3f, SlotSize);
            l.Bounds = Rect.MinMaxRect(l.Tray.xMin - Margin, l.Tray.yMin - Margin, l.Tray.xMax + Margin, l.Hud.yMax + Margin);
            return l;
        }

        public static Layout LandscapeLayout()
        {
            var l = new Layout();
            l.Landscape = true;
            l.Hud = new Rect(-BoardHalf, BoardHalf + HudGap, BoardHalf * 2f, HudHeight);
            float trayLeft = BoardHalf + TrayGap;
            l.Tray = new Rect(trayLeft, -SlotSize * 1.5f, SlotSize, SlotSize * 3f);
            float bottom = Mathf.Min(-BoardHalf, l.Tray.yMin) - Margin;
            float top = Mathf.Max(l.Hud.yMax, l.Tray.yMax) + Margin;
            l.Bounds = Rect.MinMaxRect(-BoardHalf - Margin, bottom, l.Tray.xMax + Margin, top);
            return l;
        }

        /// <summary>Bu yerlesim guvenli alana sigdirilinca dunya birimi basina piksel.</summary>
        public float PixelsPerUnit(Rect safeArea)
        {
            if (Bounds.width <= 0f || Bounds.height <= 0f || safeArea.width <= 0f || safeArea.height <= 0f) return 0f;
            return Mathf.Min(safeArea.width / Bounds.width, safeArea.height / Bounds.height);
        }

        /// <summary>Tahtayi daha buyuk gosteren yerlesim (esitlikte portre).</summary>
        public static Layout Choose(Rect safeArea)
        {
            Layout p = Portrait();
            Layout l = LandscapeLayout();
            return l.PixelsPerUnit(safeArea) > p.PixelsPerUnit(safeArea) ? l : p;
        }

        /// <summary>
        /// Ortografik kamera ayari: yerlesim guvenli alanin ortasina gelir.
        /// orthoSize = ekranYuksekligi / (2 ppu); kamera konumu guvenli alan ofsetiyle kaydirilir.
        /// </summary>
        public void FitCamera(float screenWidth, float screenHeight, Rect safeArea, out float orthoSize, out Vector2 cameraPos)
        {
            float ppu = PixelsPerUnit(safeArea);
            if (ppu <= 0f) ppu = 1f;
            orthoSize = screenHeight / (2f * ppu);
            Vector2 safeCenterPx = safeArea.center;
            Vector2 screenCenterPx = new Vector2(screenWidth * 0.5f, screenHeight * 0.5f);
            cameraPos = Bounds.center - (safeCenterPx - screenCenterPx) / ppu;
        }
    }
}
