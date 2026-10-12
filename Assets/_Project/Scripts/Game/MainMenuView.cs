using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// GDD §7.4 ana menu gorunumu: logo, "Rekor: N", "Devam et" / "Yeni oyun". Kurulumda bir kez olusturulur;
    /// icerik (MainMenuModel) yalniz menuye girerken degisir. Gorunurluk/yerlesim kurali MainMenuModel ve MenuLayout'ta.
    /// Logo Resources'tan Texture2D olarak yuklenir (doku Unity'de "Default" ya da "Sprite (2D and UI)" turunde
    /// iceri alinmis olabilir; ikisinde de ana varlik Texture2D'dir) ve Sprite kodla olusturulur.
    /// Yuklenemezse yazili "Blockbloom" basligi gosterilir.
    /// </summary>
    public sealed class MainMenuView
    {
        /// <summary>Resources.Load yolu: Assets/_Project/Resources/Branding/logo.png</summary>
        public const string LogoResourcePath = "Branding/logo";
        public const string FallbackTitle = "Blockbloom";
        public const int OrderLogo = 5;
        public const int OrderButton = 6;

        public const int ButtonContinue = 0;
        public const int ButtonNewGame = 1;

        private readonly GameObject _root;
        private readonly SpriteRenderer _logo;
        private readonly float _logoAspect;
        private readonly UiLabel _title, _record;
        private readonly ButtonView _continue, _newGame;
        private MainMenuModel _model;
        private bool _visible;

        public MainMenuView(Transform worldParent, Transform canvas, Font font)
        {
            _root = new GameObject("Menu");
            _root.transform.SetParent(worldParent, false);

            Texture2D tex = Resources.Load<Texture2D>(LogoResourcePath);
            if (tex != null && tex.width > 0 && tex.height > 0)
            {
                // ppu = doku yuksekligi -> sprite 1 birim yuksek, aspect birim genis; olcekle boyutlanir.
                Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                    tex.height, 0, SpriteMeshType.FullRect);
                sprite.name = "Logo";
                var go = new GameObject("Logo");
                go.transform.SetParent(_root.transform, false);
                _logo = go.AddComponent<SpriteRenderer>();
                _logo.sprite = sprite;
                _logo.sortingOrder = OrderLogo;
                _logoAspect = (float)tex.width / tex.height;
            }
            else
            {
                Debug.LogWarning("Blockbloom: logo yuklenemedi (Resources/" + LogoResourcePath + "), yazili baslik kullaniliyor.");
                _logoAspect = 0f;
            }

            _title = new UiLabel(canvas, font, "MenuTitle", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Accent);
            _title.Text = FallbackTitle;
            _record = new UiLabel(canvas, font, "MenuRecord", TextAnchor.MiddleCenter, FontStyle.Bold, Palette.Text);
            _continue = new ButtonView(_root.transform, canvas, font, "MenuContinue", "Devam et", OrderButton);
            _newGame = new ButtonView(_root.transform, canvas, font, "MenuNewGame", "Yeni oyun", OrderButton);
            SetModel(MainMenuModel.Create(false, 0));
            SetVisible(false);
        }

        public bool HasLogo => _logo != null;
        public MainMenuModel Model => _model;

        /// <summary>Menuye girerken cagrilir (metin burada uretilir; kare basina degil).</summary>
        public void SetModel(MainMenuModel model)
        {
            _model = model;
            _record.Text = model.ShowRecord ? MainMenuModel.RecordText(model.Record) : "";
            _continue.SetPrimary(model.IsPrimary(MenuButton.Continue));
            _newGame.SetPrimary(model.IsPrimary(MenuButton.NewGame));
            ApplyVisibility();
        }

        /// <summary>safe: guvenli alan (dunya); screenWidth: kameranin gordugu dunya genisligi; minTouch: dunya birimi.</summary>
        public void Layout(Rect safe, float screenWidth, float minTouch)
        {
            MenuLayout l = MenuLayout.Compute(safe, screenWidth, _logoAspect, _model.ShowRecord, _model.ShowContinue, minTouch);
            if (_logo != null)
            {
                _logo.transform.localPosition = new Vector3(l.Logo.center.x, l.Logo.center.y, 0f);
                float h = Mathf.Max(1e-4f, l.Logo.height);
                _logo.transform.localScale = new Vector3(h, h, 1f);
            }
            _title.Place(l.Logo, Mathf.Min(l.Logo.height * 0.6f, l.Logo.width / 6f));
            _record.Place(l.Record, MenuLayout.RecordHeight * 0.6f);
            float font = Mathf.Min(0.5f, l.NewGame.height * 0.42f);
            _continue.Place(l.Continue, minTouch, font);
            _newGame.Place(l.NewGame, minTouch, font);
        }

        public void Apply(Camera cam, float ppu)
        {
            _title.Apply(cam, ppu);
            _record.Apply(cam, ppu);
            _continue.Apply(cam, ppu);
            _newGame.Apply(cam, ppu);
        }

        public void SetVisible(bool v)
        {
            _visible = v;
            _root.SetActive(v);
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            _title.Visible = _visible && _logo == null;
            _record.Visible = _visible && _model.ShowRecord;
            _continue.Visible = _visible && _model.ShowContinue;
            _newGame.Visible = _visible && _model.ShowNewGame;
        }

        /// <summary>Dokunulan dugme (ButtonContinue/ButtonNewGame) ya da TapTracker.NoButton.</summary>
        public int HitTest(Vector2 world)
        {
            if (!_visible) return TapTracker.NoButton;
            if (_continue.Hit(world)) return ButtonContinue;
            if (_newGame.Hit(world)) return ButtonNewGame;
            return TapTracker.NoButton;
        }
    }
}
