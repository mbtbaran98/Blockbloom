using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// GDD §7.4 ana menu yerlesimi (dunya birimi). Portre/kareye yakin: logo, "Rekor: N", dugmeler yukaridan asagi
    /// ortalanmis tek sutun. Yatay (guvenli alan en/boy &gt; LandscapeAspect): logo solda, rekor + dugmeler sagda.
    /// Logo en-boy orani korunur, genisligi kamera gorusunun (ekran genisligi) en fazla %85'i ve guvenli alanin icinde.
    /// Dugme yuksekligi en az asgari dokunma alani (44 pt). Saf hesap; bellek ayirmaz.
    /// </summary>
    public struct MenuLayout
    {
        public const float LogoMaxWidthOfScreen = 0.85f;
        public const float LandscapeAspect = 1.2f;
        /// <summary>Logo yoksa yazili baslik ("Blockbloom") icin varsayilan en/boy.</summary>
        public const float TitleAspect = 4f;
        public const float Margin = 0.35f;
        public const float ButtonHeight = 1.25f;
        public const float ButtonMaxWidth = 6.5f;
        public const float ButtonGap = 0.4f;
        public const float RecordHeight = 0.8f;
        public const float SectionGap = 0.55f;
        /// <summary>Portrede logo guvenli alan yuksekliginin en fazla bu kadari.</summary>
        public const float LogoMaxHeightFrac = 0.45f;

        public bool Landscape;
        /// <summary>Logo (ya da yazili baslik) kutusu.</summary>
        public Rect Logo;
        public Rect Record;
        public Rect Continue;
        public Rect NewGame;

        /// <param name="safe">Guvenli alanin dunya dikdortgeni.</param>
        /// <param name="screenWidth">Kameranin gordugu dunya genisligi (ekran genisligi).</param>
        /// <param name="logoAspect">Logo genislik/yukseklik; &lt;= 0 ise yazili baslik orani.</param>
        /// <param name="minTouch">Asgari dokunma kenari (dunya birimi).</param>
        public static MenuLayout Compute(Rect safe, float screenWidth, float logoAspect, bool showRecord, bool showContinue, float minTouch)
        {
            var m = new MenuLayout();
            float aspect = logoAspect > 0f ? logoAspect : TitleAspect;
            m.Landscape = safe.height > 0f && safe.width / safe.height > LandscapeAspect;
            float btnH = Mathf.Max(ButtonHeight, minTouch);
            int buttons = showContinue ? 2 : 1;
            float buttonsH = btnH * buttons + ButtonGap * (buttons - 1);
            float recordBlock = showRecord ? RecordHeight + SectionGap * 0.5f : 0f;
            float logoMaxW = Mathf.Max(0f, LogoMaxWidthOfScreen * screenWidth);

            if (!m.Landscape)
            {
                float colW = Mathf.Max(0f, safe.width - 2f * Margin);
                float rest = SectionGap + recordBlock + buttonsH;
                float maxLogoH = Mathf.Min(Mathf.Max(0f, safe.height - 2f * Margin - rest), LogoMaxHeightFrac * safe.height);
                Vector2 logo = FitLogo(Mathf.Min(logoMaxW, colW), maxLogoH, aspect);
                float total = logo.y + rest;
                float top = safe.center.y + total * 0.5f;
                m.Logo = new Rect(safe.center.x - logo.x * 0.5f, top - logo.y, logo.x, logo.y);
                float y = m.Logo.yMin - SectionGap;
                float btnW = ButtonWidth(colW, minTouch);
                PlaceColumn(ref m, safe.center.x, y, btnW, btnH, showRecord, showContinue, colW);
            }
            else
            {
                float half = safe.width * 0.5f;
                float leftW = Mathf.Max(0f, half - 2f * Margin);
                Vector2 logo = FitLogo(Mathf.Min(logoMaxW, leftW), Mathf.Max(0f, safe.height - 2f * Margin), aspect);
                float leftCx = safe.xMin + half * 0.5f;
                m.Logo = new Rect(leftCx - logo.x * 0.5f, safe.center.y - logo.y * 0.5f, logo.x, logo.y);

                float rightW = Mathf.Max(0f, half - 2f * Margin);
                float rightCx = safe.xMin + half * 1.5f;
                float colH = recordBlock + buttonsH;
                float y = safe.center.y + colH * 0.5f;
                PlaceColumn(ref m, rightCx, y, ButtonWidth(rightW, minTouch), btnH, showRecord, showContinue, rightW);
            }
            return m;
        }

        private static float ButtonWidth(float colW, float minTouch)
        {
            return Mathf.Max(minTouch, Mathf.Min(ButtonMaxWidth, colW * 0.8f));
        }

        private static Vector2 FitLogo(float maxW, float maxH, float aspect)
        {
            float w = Mathf.Max(0f, maxW);
            float h = w / aspect;
            if (h > maxH)
            {
                h = Mathf.Max(0f, maxH);
                w = h * aspect;
            }
            return new Vector2(w, h);
        }

        /// <summary>top'tan asagi: rekor satiri (varsa), Devam et (varsa), Yeni oyun.</summary>
        private static void PlaceColumn(ref MenuLayout m, float cx, float top, float btnW, float btnH, bool showRecord, bool showContinue, float colW)
        {
            float y = top;
            if (showRecord)
            {
                m.Record = new Rect(cx - colW * 0.5f, y - RecordHeight, colW, RecordHeight);
                y -= RecordHeight + SectionGap * 0.5f;
            }
            else
            {
                m.Record = new Rect(cx, y, 0f, 0f);
            }
            if (showContinue)
            {
                m.Continue = new Rect(cx - btnW * 0.5f, y - btnH, btnW, btnH);
                y -= btnH + ButtonGap;
            }
            else
            {
                m.Continue = new Rect(cx, y, 0f, 0f);
            }
            m.NewGame = new Rect(cx - btnW * 0.5f, y - btnH, btnW, btnH);
        }

        /// <summary>Ekran piksel dikdortgenini (ör. Screen.safeArea) ortografik kameranin dunya dikdortgenine cevirir.</summary>
        public static Rect ScreenToWorldRect(Rect px, float screenWidth, float screenHeight, Vector2 cameraPos, float pixelsPerUnit)
        {
            if (pixelsPerUnit <= 0f) return new Rect(cameraPos, Vector2.zero);
            float x0 = cameraPos.x + (px.xMin - screenWidth * 0.5f) / pixelsPerUnit;
            float y0 = cameraPos.y + (px.yMin - screenHeight * 0.5f) / pixelsPerUnit;
            return new Rect(x0, y0, px.width / pixelsPerUnit, px.height / pixelsPerUnit);
        }

        /// <summary>Gorsel dugme kutusunu, merkezi ayni kalarak en az minTouch x minTouch dokunma alanina buyutur.</summary>
        public static Rect TouchRect(Rect visual, float minTouch)
        {
            float w = Mathf.Max(visual.width, minTouch);
            float h = Mathf.Max(visual.height, minTouch);
            return new Rect(visual.center.x - w * 0.5f, visual.center.y - h * 0.5f, w, h);
        }
    }
}
