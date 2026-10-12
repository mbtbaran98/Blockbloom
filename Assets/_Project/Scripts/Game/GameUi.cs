using UnityEngine;
using UnityEngine.UI;

namespace Blockbloom.Game
{
    /// <summary>
    /// Dunya dikdortgenine baglanan uGUI metni (Screen Space Overlay kanvas, olcekleyicisiz: 1 kanvas birimi = 1 piksel).
    /// Konum/boyut yalniz yerlesim degisince (Apply) hesaplanir; metin yalniz deger degisince atanir.
    /// </summary>
    public sealed class UiLabel
    {
        private readonly Text _text;
        private readonly RectTransform _rt;
        private Rect _world;
        private float _worldFont;

        public UiLabel(Transform canvas, Font font, string name, TextAnchor align, FontStyle style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            _rt = (RectTransform)go.transform;
            _rt.anchorMin = Vector2.zero;
            _rt.anchorMax = Vector2.zero;
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _text = go.AddComponent<Text>();
            _text.font = font;
            _text.alignment = align;
            _text.fontStyle = style;
            _text.color = color;
            _text.raycastTarget = false;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.supportRichText = false;
        }

        /// <summary>worldRect: metin kutusu; worldFontHeight: yazi boyu (dunya birimi).</summary>
        public void Place(Rect worldRect, float worldFontHeight)
        {
            _world = worldRect;
            _worldFont = worldFontHeight;
        }

        public void Apply(Camera cam, float pixelsPerUnit)
        {
            Vector3 a = cam.WorldToScreenPoint(new Vector3(_world.xMin, _world.yMin, 0f));
            Vector3 b = cam.WorldToScreenPoint(new Vector3(_world.xMax, _world.yMax, 0f));
            _rt.anchoredPosition = new Vector2((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f);
            _rt.sizeDelta = new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y));
            _text.fontSize = Mathf.Max(1, Mathf.RoundToInt(_worldFont * pixelsPerUnit));
        }

        public string Text
        {
            get => _text.text;
            set { if (_text.text != value) _text.text = value; }
        }

        public bool Visible
        {
            get => _text.enabled;
            set { if (_text.enabled != value) _text.enabled = value; }
        }

        public Color Color
        {
            get => _text.color;
            set { _text.color = value; }
        }
    }

    /// <summary>
    /// Isabet testi tabanli dugme (EventSystem yok): dunya uzayinda 9 dilimli yuvarlak zemin (SpriteRenderer) + uGUI etiket.
    /// Dokunma alani gorsel kutunun merkezinde, en az asgari dokunma kenari (GDD §7.4: &gt;= 44 pt) boyutundadir.
    /// </summary>
    public sealed class ButtonView
    {
        private readonly SpriteRenderer _bg;
        private readonly UiLabel _label;
        private Rect _touch;
        private bool _visible = true;

        public ButtonView(Transform worldParent, Transform canvas, Font font, string name, string text, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldParent, false);
            _bg = go.AddComponent<SpriteRenderer>();
            _bg.sprite = SpriteFactory.Panel;
            _bg.drawMode = SpriteDrawMode.Sliced;
            _bg.sortingOrder = order;
            _label = new UiLabel(canvas, font, name + "Label", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Text);
            _label.Text = text;
            SetPrimary(false);
        }

        /// <summary>Birincil: vurgu turkuaz zemin, koyu yazi. Ikincil: panel tonu zemin, beyaz yazi.</summary>
        public void SetPrimary(bool primary)
        {
            _bg.color = primary ? (Color)Palette.Accent : (Color)Palette.ButtonSecondary;
            _label.Color = primary ? (Color)Palette.Background : (Color)Palette.Text;
        }

        /// <summary>visual: gorsel kutu (dunya); minTouch: asgari dokunma kenari (dunya).</summary>
        public void Place(Rect visual, float minTouch, float fontHeight)
        {
            _bg.transform.localPosition = new Vector3(visual.center.x, visual.center.y, 0f);
            _bg.size = visual.size;
            _touch = MenuLayout.TouchRect(visual, minTouch);
            _label.Place(visual, fontHeight);
        }

        public void Apply(Camera cam, float ppu) => _label.Apply(cam, ppu);

        public Rect TouchRect => _touch;

        public bool Hit(Vector2 world) => _visible && _touch.Contains(world);

        public bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                if (_bg.enabled != value) _bg.enabled = value;
                _label.Visible = value;
            }
        }
    }

    /// <summary>
    /// HUD: menu dugmesi (sol ust, GDD §7.4), skor, rekor (GDD §7.3: max(oyunBasiRekor, skor)), kombo.
    /// Kombo etiketi baslik satirinin ortasinda (skor degeriyle cakismasin diye; bicim M1'de art-ui ile netlesir).
    /// </summary>
    public sealed class HudView
    {
        public const int OrderMenuButton = 30;
        /// <summary>Menu dugmesinin gorsel kenari (dunya birimi; HUD yuksekligiyle sinirli).</summary>
        public const float MenuButtonSize = 1.15f;
        private const float MenuButtonGap = 0.3f;

        private readonly UiLabel _scoreTitle, _scoreValue, _bestTitle, _bestValue, _combo;
        private readonly SpriteRenderer _menuBg;
        private readonly Transform[] _menuBars = new Transform[3];
        private Rect _menuTouch;
        private int _score = -1, _best = -1, _comboValue = -1;

        public HudView(Transform worldParent, Transform canvas, Font font)
        {
            _scoreTitle = new UiLabel(canvas, font, "ScoreTitle", TextAnchor.LowerLeft, FontStyle.Bold, Palette.TextDim);
            _scoreValue = new UiLabel(canvas, font, "ScoreValue", TextAnchor.UpperLeft, FontStyle.Bold, Palette.Text);
            _bestTitle = new UiLabel(canvas, font, "BestTitle", TextAnchor.LowerRight, FontStyle.Bold, Palette.TextDim);
            _bestValue = new UiLabel(canvas, font, "BestValue", TextAnchor.UpperRight, FontStyle.Bold, Palette.Text);
            _combo = new UiLabel(canvas, font, "Combo", TextAnchor.LowerCenter, FontStyle.Bold, Palette.Accent);
            _scoreTitle.Text = "SKOR";
            _bestTitle.Text = "REKOR";

            // Menu dugmesi: yuvarlak zemin + uc yatay cubuk (sprite; ek varlik yok).
            var btn = new GameObject("MenuButton");
            btn.transform.SetParent(worldParent, false);
            _menuBg = btn.AddComponent<SpriteRenderer>();
            _menuBg.sprite = SpriteFactory.Panel;
            _menuBg.drawMode = SpriteDrawMode.Sliced;
            _menuBg.color = Palette.ButtonSecondary;
            _menuBg.sortingOrder = OrderMenuButton;
            for (int i = 0; i < _menuBars.Length; i++)
            {
                var bar = new GameObject("Bar" + i);
                bar.transform.SetParent(btn.transform, false);
                var sr = bar.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteFactory.White;
                sr.color = Palette.Text;
                sr.sortingOrder = OrderMenuButton + 1;
                _menuBars[i] = bar.transform;
            }
        }

        /// <summary>hud: HUD dikdortgeni (dunya); minTouch: asgari dokunma kenari (dunya, GDD §7.4 &gt;= 44 pt).</summary>
        public void Layout(Rect hud, float minTouch)
        {
            float s = Mathf.Min(MenuButtonSize, hud.height);
            var menu = new Rect(hud.xMin, hud.center.y - s * 0.5f, s, s);
            _menuBg.transform.localPosition = new Vector3(menu.center.x, menu.center.y, 0f);
            _menuBg.size = menu.size;
            for (int i = 0; i < _menuBars.Length; i++)
            {
                _menuBars[i].localPosition = new Vector3(0f, (1 - i) * s * 0.2f, 0f);
                _menuBars[i].localScale = new Vector3(s * 0.52f, s * 0.085f, 1f);
            }
            _menuTouch = MenuLayout.TouchRect(menu, minTouch);

            float titleH = hud.height * 0.32f;
            float valueH = hud.height - titleH;
            float halfW = hud.width * 0.5f;
            float left = menu.xMax + MenuButtonGap;
            float leftW = Mathf.Max(0.5f, hud.center.x - left);
            _scoreTitle.Place(new Rect(left, hud.yMax - titleH, leftW, titleH), 0.38f);
            _scoreValue.Place(new Rect(left, hud.yMin, leftW, valueH), 0.9f);
            _bestTitle.Place(new Rect(hud.xMin + halfW, hud.yMax - titleH, halfW, titleH), 0.38f);
            _bestValue.Place(new Rect(hud.xMin + halfW, hud.yMin, halfW, valueH), 0.9f);
            _combo.Place(new Rect(hud.center.x - 1.5f, hud.yMax - titleH, 3f, titleH), 0.42f);
        }

        public void Apply(Camera cam, float ppu)
        {
            _scoreTitle.Apply(cam, ppu);
            _scoreValue.Apply(cam, ppu);
            _bestTitle.Apply(cam, ppu);
            _bestValue.Apply(cam, ppu);
            _combo.Apply(cam, ppu);
        }

        /// <summary>Menu dugmesinin dokunma alani (dunya).</summary>
        public Rect MenuButtonRect => _menuTouch;

        /// <summary>Degisen deger varsa metni gunceller (deger degismedikce bellek ayirmaz).</summary>
        public void Set(int score, int best, int combo)
        {
            if (score != _score) { _score = score; _scoreValue.Text = score.ToString(); }
            if (best != _best) { _best = best; _bestValue.Text = best.ToString(); }
            if (combo != _comboValue)
            {
                _comboValue = combo;
                // Kombo K >= 1 iken gosterilir (gosterim bicimi M1'de art-ui ile netlesir).
                _combo.Visible = combo >= 1;
                if (combo >= 1) _combo.Text = "Kombo x" + combo;
            }
        }
    }

    /// <summary>GDD §7.2 oyun sonu paneli: "Menu" (ikincil, solda) ve "Tekrar oyna" (birincil, sagda). Arka plan sprite'larla (dunya), metin uGUI ile.</summary>
    public sealed class GameOverView
    {
        public const int OrderDim = 40;
        public const int OrderPanel = 41;
        public const int OrderButton = 42;

        private readonly GameObject _root;
        private readonly SpriteRenderer _dim, _panel;
        private readonly UiLabel _title, _score, _newRecord, _best, _moves, _lines, _combo;
        private readonly ButtonView _replay, _menu;

        public GameOverView(Transform worldParent, Transform canvas, Font font)
        {
            _root = new GameObject("GameOver");
            _root.transform.SetParent(worldParent, false);
            _dim = NewSprite("Dim", SpriteFactory.White, OrderDim, new Color(0f, 0f, 0f, 0.55f));
            _panel = NewSprite("Panel", SpriteFactory.Panel, OrderPanel, Palette.Panel);
            _panel.drawMode = SpriteDrawMode.Sliced;

            _title = new UiLabel(canvas, font, "GoTitle", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.TextDim);
            _score = new UiLabel(canvas, font, "GoScore", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Text);
            _newRecord = new UiLabel(canvas, font, "GoNewRecord", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Accent);
            _best = new UiLabel(canvas, font, "GoBest", TextAnchor.MiddleCenter, FontStyle.Normal, Palette.Text);
            _moves = new UiLabel(canvas, font, "GoMoves", TextAnchor.MiddleCenter, FontStyle.Normal, Palette.TextDim);
            _lines = new UiLabel(canvas, font, "GoLines", TextAnchor.MiddleCenter, FontStyle.Normal, Palette.TextDim);
            _combo = new UiLabel(canvas, font, "GoCombo", TextAnchor.MiddleCenter, FontStyle.Normal, Palette.TextDim);
            _replay = new ButtonView(_root.transform, canvas, font, "GoReplay", "Tekrar oyna", OrderButton);
            _replay.SetPrimary(true);
            _menu = new ButtonView(_root.transform, canvas, font, "GoMenu", "Menü", OrderButton);
            _menu.SetPrimary(false);
            _title.Text = "Oyun bitti";
            _newRecord.Text = "Yeni rekor!";
            SetVisible(false);
        }

        private SpriteRenderer NewSprite(string name, Sprite sprite, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }

        public bool Visible { get; private set; }

        /// <summary>Dugme kimlikleri (TapTracker icin).</summary>
        public const int ButtonReplay = 0;
        public const int ButtonMenu = 1;

        /// <summary>Dokunulan dugme (ButtonReplay/ButtonMenu) ya da TapTracker.NoButton.</summary>
        public int HitTest(Vector2 world)
        {
            if (!Visible) return TapTracker.NoButton;
            if (_replay.Hit(world)) return ButtonReplay;
            if (_menu.Hit(world)) return ButtonMenu;
            return TapTracker.NoButton;
        }

        public Rect ReplayRect => _replay.TouchRect;
        public Rect MenuRect => _menu.TouchRect;

        /// <summary>
        /// Panel tahtanin ustunde ortalanir; view: kameranin gordugu dunya dikdortgeni (karartma icin);
        /// minTouch: asgari dokunma kenari (dunya).
        /// </summary>
        public void Layout(Rect view, float minTouch)
        {
            _dim.transform.localPosition = new Vector3(view.center.x, view.center.y, 0f);
            _dim.transform.localScale = new Vector3(view.width + 2f, view.height + 2f, 1f);

            var panel = new Rect(-3.6f, -3.9f, 7.2f, 7.8f);
            _panel.transform.localPosition = new Vector3(panel.center.x, panel.center.y, 0f);
            _panel.size = panel.size;

            float bh = Mathf.Min(Mathf.Max(1.15f, minTouch), 1.9f);
            const float by = -2.875f; // dugme satiri merkezi
            _menu.Place(new Rect(-3.25f, by - bh * 0.5f, 3.05f, bh), minTouch, 0.42f);
            _replay.Place(new Rect(0.2f, by - bh * 0.5f, 3.05f, bh), minTouch, 0.42f);

            float w = panel.width - 0.6f, x = panel.xMin + 0.3f;
            _title.Place(new Rect(x, 2.75f, w, 0.7f), 0.5f);
            _score.Place(new Rect(x, 1.35f, w, 1.4f), 1.25f);
            _newRecord.Place(new Rect(x, 0.7f, w, 0.65f), 0.5f);
            _best.Place(new Rect(x, 0.05f, w, 0.6f), 0.42f);
            _moves.Place(new Rect(x, -0.75f, w, 0.55f), 0.38f);
            _lines.Place(new Rect(x, -1.3f, w, 0.55f), 0.38f);
            _combo.Place(new Rect(x, -1.85f, w, 0.55f), 0.38f);
        }

        public void Apply(Camera cam, float ppu)
        {
            _title.Apply(cam, ppu);
            _score.Apply(cam, ppu);
            _newRecord.Apply(cam, ppu);
            _best.Apply(cam, ppu);
            _moves.Apply(cam, ppu);
            _lines.Apply(cam, ppu);
            _combo.Apply(cam, ppu);
            _replay.Apply(cam, ppu);
            _menu.Apply(cam, ppu);
        }

        /// <summary>bestShown: max(oyunBasiRekor, skor) (GDD §7.2). isNewRecord: skor &gt; oyunBasiRekor.</summary>
        public void Show(int score, int bestShown, bool isNewRecord, int moves, int lines, int maxCombo)
        {
            _score.Text = score.ToString();
            _best.Text = "Rekor: " + bestShown;
            _moves.Text = "Hamle: " + moves;
            _lines.Text = "Patlatılan çizgi: " + lines;
            _combo.Text = "En yüksek kombo: " + maxCombo;
            SetVisible(true);
            _newRecord.Visible = isNewRecord;
        }

        public void Hide() => SetVisible(false);

        private void SetVisible(bool v)
        {
            Visible = v;
            _root.SetActive(v);
            _title.Visible = v;
            _score.Visible = v;
            _newRecord.Visible = v;
            _best.Visible = v;
            _moves.Visible = v;
            _lines.Visible = v;
            _combo.Visible = v;
            _replay.Visible = v;
            _menu.Visible = v;
        }
    }
}
