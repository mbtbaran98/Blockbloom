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
    }

    /// <summary>HUD: skor, rekor (GDD §7.3: max(oyunBasiRekor, skor)), kombo.</summary>
    public sealed class HudView
    {
        private readonly UiLabel _scoreTitle, _scoreValue, _bestTitle, _bestValue, _combo;
        private int _score = -1, _best = -1, _comboValue = -1;

        public HudView(Transform canvas, Font font)
        {
            _scoreTitle = new UiLabel(canvas, font, "ScoreTitle", TextAnchor.LowerLeft, FontStyle.Bold, Palette.TextDim);
            _scoreValue = new UiLabel(canvas, font, "ScoreValue", TextAnchor.UpperLeft, FontStyle.Bold, Palette.Text);
            _bestTitle = new UiLabel(canvas, font, "BestTitle", TextAnchor.LowerRight, FontStyle.Bold, Palette.TextDim);
            _bestValue = new UiLabel(canvas, font, "BestValue", TextAnchor.UpperRight, FontStyle.Bold, Palette.Text);
            _combo = new UiLabel(canvas, font, "Combo", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Accent);
            _scoreTitle.Text = "SKOR";
            _bestTitle.Text = "REKOR";
        }

        public void Layout(Rect hud)
        {
            float titleH = hud.height * 0.32f;
            float valueH = hud.height - titleH;
            float halfW = hud.width * 0.5f;
            _scoreTitle.Place(new Rect(hud.xMin, hud.yMax - titleH, halfW, titleH), 0.38f);
            _scoreValue.Place(new Rect(hud.xMin, hud.yMin, halfW, valueH), 0.9f);
            _bestTitle.Place(new Rect(hud.xMin + halfW, hud.yMax - titleH, halfW, titleH), 0.38f);
            _bestValue.Place(new Rect(hud.xMin + halfW, hud.yMin, halfW, valueH), 0.9f);
            _combo.Place(new Rect(hud.center.x - 1.5f, hud.yMin, 3f, valueH), 0.5f);
        }

        public void Apply(Camera cam, float ppu)
        {
            _scoreTitle.Apply(cam, ppu);
            _scoreValue.Apply(cam, ppu);
            _bestTitle.Apply(cam, ppu);
            _bestValue.Apply(cam, ppu);
            _combo.Apply(cam, ppu);
        }

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

    /// <summary>GDD §7.2 oyun sonu paneli. Arka plan sprite'larla (dunya), metin uGUI ile.</summary>
    public sealed class GameOverView
    {
        public const int OrderDim = 40;
        public const int OrderPanel = 41;
        public const int OrderButton = 42;

        private readonly GameObject _root;
        private readonly SpriteRenderer _dim, _panel, _button;
        private readonly UiLabel _title, _score, _newRecord, _best, _moves, _lines, _combo, _buttonLabel;
        private Rect _buttonRect;

        public GameOverView(Transform worldParent, Transform canvas, Font font)
        {
            _root = new GameObject("GameOver");
            _root.transform.SetParent(worldParent, false);
            _dim = NewSprite("Dim", SpriteFactory.White, OrderDim, new Color(0f, 0f, 0f, 0.55f));
            _panel = NewSprite("Panel", SpriteFactory.Panel, OrderPanel, Palette.Panel);
            _panel.drawMode = SpriteDrawMode.Sliced;
            _button = NewSprite("Button", SpriteFactory.Panel, OrderButton, Palette.Accent);
            _button.drawMode = SpriteDrawMode.Sliced;

            _title = new UiLabel(canvas, font, "GoTitle", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.TextDim);
            _score = new UiLabel(canvas, font, "GoScore", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Text);
            _newRecord = new UiLabel(canvas, font, "GoNewRecord", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Accent);
            _best = new UiLabel(canvas, font, "GoBest", TextAnchor.MiddleCenter, FontStyle.Normal, Palette.Text);
            _moves = new UiLabel(canvas, font, "GoMoves", TextAnchor.MiddleCenter, FontStyle.Normal, Palette.TextDim);
            _lines = new UiLabel(canvas, font, "GoLines", TextAnchor.MiddleCenter, FontStyle.Normal, Palette.TextDim);
            _combo = new UiLabel(canvas, font, "GoCombo", TextAnchor.MiddleCenter, FontStyle.Normal, Palette.TextDim);
            _buttonLabel = new UiLabel(canvas, font, "GoButton", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Background);
            _title.Text = "Oyun bitti";
            _newRecord.Text = "Yeni rekor!";
            _buttonLabel.Text = "Tekrar oyna";
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

        /// <summary>"Tekrar oyna" dokunma alani (dunya).</summary>
        public Rect ButtonRect => _buttonRect;

        /// <summary>Panel tahtanin ustunde ortalanir; view: kameranin gordugu dunya dikdortgeni (karartma icin).</summary>
        public void Layout(Rect view)
        {
            _dim.transform.localPosition = new Vector3(view.center.x, view.center.y, 0f);
            _dim.transform.localScale = new Vector3(view.width + 2f, view.height + 2f, 1f);

            var panel = new Rect(-3.6f, -3.9f, 7.2f, 7.8f);
            _panel.transform.localPosition = new Vector3(panel.center.x, panel.center.y, 0f);
            _panel.size = panel.size;

            _buttonRect = new Rect(-2.5f, -3.45f, 5f, 1.15f);
            _button.transform.localPosition = new Vector3(_buttonRect.center.x, _buttonRect.center.y, 0f);
            _button.size = _buttonRect.size;

            float w = panel.width - 0.6f, x = panel.xMin + 0.3f;
            _title.Place(new Rect(x, 2.75f, w, 0.7f), 0.5f);
            _score.Place(new Rect(x, 1.35f, w, 1.4f), 1.25f);
            _newRecord.Place(new Rect(x, 0.7f, w, 0.65f), 0.5f);
            _best.Place(new Rect(x, 0.05f, w, 0.6f), 0.42f);
            _moves.Place(new Rect(x, -0.75f, w, 0.55f), 0.38f);
            _lines.Place(new Rect(x, -1.3f, w, 0.55f), 0.38f);
            _combo.Place(new Rect(x, -1.85f, w, 0.55f), 0.38f);
            _buttonLabel.Place(_buttonRect, 0.5f);
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
            _buttonLabel.Apply(cam, ppu);
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
            _buttonLabel.Visible = v;
        }
    }
}
