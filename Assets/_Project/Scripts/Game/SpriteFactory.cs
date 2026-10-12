using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// Harici varlik olmadan, calisma aninda bir kez uretilen sprite atlasi (tek doku -> sprite'lar birlikte toplanabilir).
    /// Karolar: yuvarlatilmis kose, koyu kontur, ustten isik, alt golge, kucuk parlama ("seker" karo, CLAUDE.md gorsel kimlik).
    /// Her karo 64 px = 1 dunya birimi (1 hucre). Uretim yalnizca ilk erisimde; sonra onbellekten.
    /// </summary>
    public static class SpriteFactory
    {
        private const int Tile = 64;
        private const int Columns = 4;
        private const int Rows = 3;

        // Atlas yuvalari
        private const int SlotEmpty = 0;
        private const int SlotColor1 = 1; // 1..6
        private const int SlotGray = 7;
        private const int SlotGlow = 8;
        private const int SlotPanel = 9;
        private const int SlotWhite = 10;

        private static Texture2D _atlas;
        private static Sprite _empty, _gray, _glow, _panel, _white;
        private static readonly Sprite[] _tiles = new Sprite[Palette.TileColorCount];

        /// <summary>Renk kimligi 1..6 icin seker karo.</summary>
        public static Sprite TileSprite(int colorId)
        {
            Ensure();
            return _tiles[colorId - 1];
        }

        public static Sprite Empty { get { Ensure(); return _empty; } }
        public static Sprite Gray { get { Ensure(); return _gray; } }
        /// <summary>Beyaz yuvarlatilmis kare (vurgu katmani; renk ile tonlanir).</summary>
        public static Sprite Glow { get { Ensure(); return _glow; } }
        /// <summary>9 dilimli yuvarlatilmis panel (SpriteRenderer.drawMode = Sliced).</summary>
        public static Sprite Panel { get { Ensure(); return _panel; } }
        /// <summary>Duz beyaz kare (karartma katmani).</summary>
        public static Sprite White { get { Ensure(); return _white; } }

        private static void Ensure()
        {
            if (_atlas != null) return;

            int w = Tile * Columns, h = Tile * Rows;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.name = "BlockbloomAtlas";
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];

            DrawEmpty(px, w, SlotEmpty);
            for (int i = 0; i < Palette.TileColorCount; i++) DrawCandy(px, w, SlotColor1 + i, Palette.Tile(i + 1));
            DrawCandy(px, w, SlotGray, Palette.GameOverGray);
            DrawFlatRounded(px, w, SlotGlow, 3f, 14f);
            DrawFlatRounded(px, w, SlotPanel, 0f, 18f);
            DrawSolid(px, w, SlotWhite);

            tex.SetPixels32(px);
            tex.Apply(false, true); // CPU kopyasi birakilir (bellek)
            _atlas = tex;

            _empty = Make(SlotEmpty, Vector4.zero);
            for (int i = 0; i < Palette.TileColorCount; i++) _tiles[i] = Make(SlotColor1 + i, Vector4.zero);
            _gray = Make(SlotGray, Vector4.zero);
            _glow = Make(SlotGlow, Vector4.zero);
            _panel = Make(SlotPanel, new Vector4(22, 22, 22, 22));
            // Duz kare: kenar pikselleri komsu karoya tasmasin diye ic 48x48 kullanilir.
            _white = Sprite.Create(_atlas, new Rect(OriginX(SlotWhite) + 8, OriginY(SlotWhite) + 8, 48, 48),
                new Vector2(0.5f, 0.5f), 48f, 0, SpriteMeshType.FullRect);
            _white.name = "White";
        }

        private static Sprite Make(int slot, Vector4 border)
        {
            Sprite s = Sprite.Create(_atlas, new Rect(OriginX(slot), OriginY(slot), Tile, Tile),
                new Vector2(0.5f, 0.5f), Tile, 0, SpriteMeshType.FullRect, border);
            s.name = "Tile" + slot;
            return s;
        }

        private static int OriginX(int slot) => (slot % Columns) * Tile;
        private static int OriginY(int slot) => (slot / Columns) * Tile;

        // ---------- cizim ----------

        /// <summary>Yuvarlatilmis kare isaretli uzaklik alani; d &lt; 0 icerde (piksel).</summary>
        private static float RoundedRectSdf(float x, float y, float half, float radius)
        {
            float c = Tile * 0.5f;
            float qx = Mathf.Abs(x - c) - (half - radius);
            float qy = Mathf.Abs(y - c) - (half - radius);
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        private static void DrawCandy(Color32[] px, int stride, int slot, Color32 baseColor)
        {
            Color body = baseColor;
            Color bg = Palette.Background;
            Color outline = Color.Lerp(body * 0.42f, bg, 0.45f);
            outline.a = 1f;
            const float inset = 2.5f, radius = 13f, outlineW = 3.2f;
            float half = Tile * 0.5f - inset;
            int ox = OriginX(slot), oy = OriginY(slot);

            for (int y = 0; y < Tile; y++)
            {
                for (int x = 0; x < Tile; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = RoundedRectSdf(fx, fy, half, radius);
                    float alpha = Mathf.Clamp01(0.5f - d);
                    // dikey konum 0 alt .. 1 ust (karo ici)
                    float v = Mathf.InverseLerp(Tile * 0.5f - half, Tile * 0.5f + half, fy);
                    float u = Mathf.InverseLerp(Tile * 0.5f - half, Tile * 0.5f + half, fx);

                    Color c = body;
                    // ustten isik
                    float top = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.86f, v));
                    c = Color.Lerp(c, Color.white, 0.30f * top);
                    // alt golge
                    float bottom = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0.08f, v));
                    c = Color.Lerp(c, c * 0.68f, bottom);
                    // kucuk parlama (sol ust elips)
                    float ex = (u - 0.30f) / 0.17f, ey = (v - 0.80f) / 0.075f;
                    float gloss = Mathf.Clamp01(1f - (ex * ex + ey * ey));
                    c = Color.Lerp(c, Color.white, 0.65f * Mathf.SmoothStep(0f, 1f, gloss * 2f));
                    // kontur
                    float o = Mathf.Clamp01(d + outlineW + 0.5f);
                    c = Color.Lerp(c, outline, o);
                    c.a = alpha;
                    px[(oy + y) * stride + ox + x] = c;
                }
            }
        }

        private static void DrawEmpty(Color32[] px, int stride, int slot)
        {
            Color body = Palette.EmptyCell;
            Color edge = Color.Lerp(body, Palette.Background, 0.6f);
            const float inset = 2.5f, radius = 12f;
            float half = Tile * 0.5f - inset;
            int ox = OriginX(slot), oy = OriginY(slot);
            for (int y = 0; y < Tile; y++)
            {
                for (int x = 0; x < Tile; x++)
                {
                    float fx = x + 0.5f, fy = y + 0.5f;
                    float d = RoundedRectSdf(fx, fy, half, radius);
                    float v = Mathf.InverseLerp(Tile * 0.5f - half, Tile * 0.5f + half, fy);
                    // icbukey his: ust kenar biraz koyu
                    Color c = Color.Lerp(body, edge, 0.5f * Mathf.InverseLerp(0.75f, 1f, v));
                    c = Color.Lerp(c, edge, Mathf.Clamp01(d + 2.5f));
                    c.a = Mathf.Clamp01(0.5f - d);
                    px[(oy + y) * stride + ox + x] = c;
                }
            }
        }

        private static void DrawFlatRounded(Color32[] px, int stride, int slot, float inset, float radius)
        {
            float half = Tile * 0.5f - inset;
            int ox = OriginX(slot), oy = OriginY(slot);
            for (int y = 0; y < Tile; y++)
            {
                for (int x = 0; x < Tile; x++)
                {
                    float d = RoundedRectSdf(x + 0.5f, y + 0.5f, half, radius);
                    byte a = (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(0.5f - d));
                    px[(oy + y) * stride + ox + x] = new Color32(255, 255, 255, a);
                }
            }
        }

        private static void DrawSolid(Color32[] px, int stride, int slot)
        {
            int ox = OriginX(slot), oy = OriginY(slot);
            for (int y = 0; y < Tile; y++)
                for (int x = 0; x < Tile; x++)
                    px[(oy + y) * stride + ox + x] = new Color32(255, 255, 255, 255);
        }
    }
}
