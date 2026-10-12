using System;
using Blockbloom.Core;
using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// M0 uygulama denetleyicisi: sahneyi kodla kurar, ana menuyu (GDD §7.4) ve oyunu yonetir, girdiyi okur,
    /// Core'u (GameState) surer, gorunumleri gunceller. Acilista ana menu gosterilir; kayitli yarim oyun yalniz
    /// "Devam et" ile surdurulur. Ekranlar arasi gecis &lt;= 200 ms soluma (FadeTimeline).
    /// Oyun mantigi yalnizca GameState.TryPlace uzerinden ilerler; bu sinif kural icermez.
    /// Performans (I8): tum nesneler Awake'te kurulur; Update'te Instantiate/Find yok, kare basina bellek ayirma yok
    /// (metinler yalniz deger degisince, MoveResult yalniz hamlede olusur).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }

        private enum Phase { Playing, GameOverWait, GameOverPanel }

        // Kurulum
        private Camera _cam;
        private Transform _world;
        private BoardView _board;
        private readonly PieceView[] _pieces = new PieceView[Tray.SlotCount];
        private HudView _hud;
        private GameOverView _gameOver;
        private MainMenuView _menu;
        private Transform _gameRoot;
        private Canvas _gameCanvas, _menuCanvas;
        private CanvasGroup _gameGroup, _menuGroup;
        private SpriteRenderer _cover;
        private readonly PointerInput _input = new PointerInput();
        private readonly TapTracker _taps = new TapTracker();

        // Ekran ve gecis (GDD §7.4)
        private AppScreen _screen = AppScreen.Menu;
        private FadeTimeline _fade;
        private bool _fadeSwitched;
        private AppScreen _fadeTarget;
        private MenuButton _fadeAction;
        private float _coverShown = -1f;

        // Yerlesim
        private Layout _layout;
        private int _screenW = -1, _screenH = -1;
        private Rect _safe;
        private Rect _view;
        private Rect _safeWorld;
        private float _ppu;
        private float _minTouch;

        // Oyun
        private GameState _game;
        private IRecordStore _records;
        private SnapshotStore _saves;
        private int _bestAtStart;
        private Phase _phase;
        private float _gameOverAt;
        private ulong _seedSalt;
        private readonly byte[] _colorsBefore = new byte[CoreConfig.CellCount];

        // Surukleme
        private int _dragSlot = -1;
        private float _liftStart;
        private bool _hasAnchor;
        private Cell _anchor;

        // Yuvaya donus animasyonu (yuva basina)
        private readonly bool[] _returning = new bool[Tray.SlotCount];
        private readonly Vector2[] _retFrom = new Vector2[Tray.SlotCount];
        private readonly float[] _retFromScale = new float[Tray.SlotCount];
        private readonly float[] _retStart = new float[Tray.SlotCount];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null; // "Enter Play Mode Options" ile alan yeniden yuklenmezse
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Application.targetFrameRate = 60;

            _records = new PlayerPrefsRecordStore();
            _saves = new SnapshotStore();
            BuildScene();
            UpdateLayoutIfNeeded(); // ilk kare cizilmeden yerlesim hazir
            ShowMenu();             // GDD §7.4: acilista ana menu; kayit varsa "Devam et"
            _fade = FadeTimeline.RevealOnly(Time.unscaledTime);
            _fadeSwitched = true;
            ApplyCover(1f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ kurulum

        private void BuildScene()
        {
            _cam = Camera.main;
            if (_cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.transform.SetParent(transform, false);
                _cam = camGo.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Palette.Background;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 50f;
            _cam.transform.rotation = Quaternion.identity;

            _world = new GameObject("World").transform;
            _world.SetParent(transform, false);
            // Tum gorunumler localPosition ile dunya biriminde yerlesir; kok dunya orijininde ve olceksiz olmali
            // (GameApp sahnede elle baska bir konuma eklenmis olsa bile).
            _world.position = Vector3.zero;
            _world.rotation = Quaternion.identity;
            Vector3 ls = transform.lossyScale;
            _world.localScale = new Vector3(1f / Mathf.Max(1e-4f, ls.x), 1f / Mathf.Max(1e-4f, ls.y), 1f);
            _gameRoot = new GameObject("Game").transform;
            _gameRoot.SetParent(_world, false);
            _board = new BoardView(_gameRoot);
            var trayRoot = new GameObject("Tray").transform;
            trayRoot.SetParent(_gameRoot, false);
            for (int i = 0; i < _pieces.Length; i++) _pieces[i] = new PieceView(trayRoot, i);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _gameCanvas = NewCanvas("GameCanvas", out _gameGroup);
            _menuCanvas = NewCanvas("MenuCanvas", out _menuGroup);
            _hud = new HudView(_gameRoot, _gameCanvas.transform, font);
            _gameOver = new GameOverView(_gameRoot, _gameCanvas.transform, font);
            _menu = new MainMenuView(_world, _menuCanvas.transform, font);

            // Gecis ortusu: zemin renginde tam ekran sprite (dunya) + kanvas alfasi (metinler ortunun ustunde cizildigi icin).
            var coverGo = new GameObject("FadeCover");
            coverGo.transform.SetParent(_world, false);
            _cover = coverGo.AddComponent<SpriteRenderer>();
            _cover.sprite = SpriteFactory.White;
            _cover.sortingOrder = 1000;
            _cover.color = Palette.Background;
            _cover.enabled = false;
        }

        private Canvas NewCanvas(string name, out CanvasGroup group)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            group = go.AddComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            return canvas;
        }

        /// <summary>
        /// Surdurulebilir kayit var mi; varsa geri yukler. Okunamayan, tutarsiz ya da bitmis kayit silinir
        /// (menude hic calismayan bir "Devam et" gosterilmesin diye menuye girerken de cagrilir).
        /// </summary>
        private bool TryLoadResumable(out GameState game, out int bestAtStart)
        {
            game = null;
            if (!_saves.TryLoad(out GameSnapshot snap, out bestAtStart)) return false;
            try
            {
                GameState restored = GameState.FromSnapshot(snap);
                if (!restored.IsGameOver)
                {
                    game = restored;
                    return true;
                }
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning("Blockbloom: kayitli oyun gecersiz, siliniyor. " + e.Message);
            }
            _saves.Clear();
            bestAtStart = 0;
            return false;
        }

        /// <summary>GDD §7.4 "Devam et": kaydi surdurur (oyunBasiRekor kayittan). Kayit kaybolduysa yeni oyun.</summary>
        private void ResumeSavedGame()
        {
            if (TryLoadResumable(out GameState game, out int bestAtStart)) BeginGame(game, bestAtStart);
            else StartNewGame();
        }

        private void StartNewGame()
        {
            // Tohum Unity katmaninda: zaman + oturum ici sayac (ayni tikte iki oyun ayni olmasin).
            _seedSalt += 0x9E3779B97F4A7C15UL;
            ulong seed = (ulong)DateTime.UtcNow.Ticks ^ _seedSalt;
            BeginGame(GameState.NewGame(seed), _records.BestScore);
        }

        /// <summary>bestAtStart: GDD §7.2 oyunBasiRekor; oyun boyunca sabit.</summary>
        private void BeginGame(GameState game, int bestAtStart)
        {
            _game = game;
            _bestAtStart = bestAtStart;
            _phase = Phase.Playing;
            _dragSlot = -1;
            _hasAnchor = false;
            _taps.Reset();
            for (int i = 0; i < _returning.Length; i++) _returning[i] = false;
            _gameOver.Hide();
            _board.ClearAllFx();
            RenderBoard();
            ShowTray();
            UpdateHud();
            _screenW = -1; // yerlesimi yeniden uygula
        }

        // ------------------------------------------------------------------ kare dongusu

        private void Update()
        {
            if (_menu == null) return; // kurulmamis (yinelenen ornek yok ediliyor)
            float now = Time.unscaledTime;
            UpdateLayoutIfNeeded();
            TickTransition(now);

            // Girdi her karede okunur (izlenen temas tutarli kalsin); gecis surerken yok sayilir.
            PointerSample p = _input.Read();
            bool back = BackKey.WasPressedThisFrame();
            bool transitioning = _fade.Active;

            if (_screen == AppScreen.Menu)
            {
                if (!transitioning) HandleMenuInput(p, now);
            }
            else if (_game != null)
            {
                switch (_phase)
                {
                    case Phase.Playing:
                        if (AppNav.DragAllowed(_screen, transitioning)) HandlePlayingInput(p, now);
                        break;
                    case Phase.GameOverWait:
                        TickGameOver(now);
                        break;
                    case Phase.GameOverPanel:
                        if (!transitioning) HandlePanelInput(p, now);
                        break;
                }
            }

            if (back) HandleBack(now);

            if (_game != null && _screen == AppScreen.Game)
            {
                TickLift(now);
                TickReturns(now);
                _board.TickFx(now);
            }
        }

        // ------------------------------------------------------------------ menu ve gecisler (GDD §7.4)

        private void HandleMenuInput(PointerSample p, float now)
        {
            if (p.Down) _taps.Down(_menu.HitTest(ScreenToWorld(p.Position)));
            if (!p.Up) return;
            int fired = _taps.Up(_menu.HitTest(ScreenToWorld(p.Position)));
            if (fired == MainMenuView.ButtonContinue) BeginTransition(AppScreen.Game, MenuButton.Continue, now);
            else if (fired == MainMenuView.ButtonNewGame)
            {
                // Onay yok: eski kayit silinir, rekor korunur.
                _saves.Clear();
                BeginTransition(AppScreen.Game, MenuButton.NewGame, now);
            }
        }

        private void HandleBack(float now)
        {
            bool locked = _screen == AppScreen.Game && _phase == Phase.GameOverWait;
            switch (AppNav.OnBack(_screen, _fade.Active, locked))
            {
                case BackAction.GoToMenu:
                    GoToMenu(now);
                    break;
                case BackAction.Quit:
                    QuitApp();
                    break;
            }
        }

        /// <summary>Oyun icinden menuye: surukleme iptal, oyun kaydedilir (bitmisse kayit zaten silinmis), soluma.</summary>
        private void GoToMenu(float now)
        {
            CancelDrag();
            PersistProgress();
            BeginTransition(AppScreen.Menu, MenuButton.None, now);
        }

        private static void QuitApp()
        {
            // iOS'ta uygulamalar kendini kapatmaz (geri tusu da yok); diger platformlarda cik. Editorde etkisiz.
            if (Application.platform == RuntimePlatform.IPhonePlayer) return;
            Application.Quit();
        }

        private void BeginTransition(AppScreen target, MenuButton action, float now)
        {
            _taps.Reset();
            _fade = FadeTimeline.Transition(now);
            _fadeSwitched = false;
            _fadeTarget = target;
            _fadeAction = action;
        }

        /// <summary>Ortu hesabi; solmanin sonunda ekran degisir. Kare basina bellek ayirmaz.</summary>
        private void TickTransition(float now)
        {
            if (!_fade.Active) return;
            if (!_fadeSwitched && _fade.PastMidpoint(now))
            {
                _fadeSwitched = true;
                if (_fadeTarget == AppScreen.Menu)
                {
                    ShowMenu();
                }
                else
                {
                    if (_fadeAction == MenuButton.Continue) ResumeSavedGame();
                    else StartNewGame();
                    ShowGameScreen();
                }
            }
            float c = _fade.Cover(now);
            if (_fade.Finished(now))
            {
                _fade.Stop();
                c = 0f;
            }
            ApplyCover(c);
        }

        private void ApplyCover(float c)
        {
            if (c == _coverShown) return;
            _coverShown = c;
            bool on = c > 0f;
            if (_cover.enabled != on) _cover.enabled = on;
            Color col = Palette.Background;
            col.a = c;
            _cover.color = col;
            _gameGroup.alpha = 1f - c;
            _menuGroup.alpha = 1f - c;
        }

        private void ShowMenu()
        {
            _screen = AppScreen.Menu;
            _taps.Reset();
            _gameOver.Hide();
            _gameRoot.gameObject.SetActive(false);
            _gameCanvas.enabled = false;

            bool hasSave = TryLoadResumable(out _, out _);
            _menu.SetModel(MainMenuModel.Create(hasSave, _records.BestScore));
            _menu.Layout(_safeWorld, _view.width, _minTouch);
            _menu.Apply(_cam, _ppu);
            _menu.SetVisible(true);
            _menuCanvas.enabled = true;
        }

        private void ShowGameScreen()
        {
            _screen = AppScreen.Game;
            _taps.Reset();
            _menu.SetVisible(false);
            _menuCanvas.enabled = false;
            _gameRoot.gameObject.SetActive(true);
            _gameCanvas.enabled = true;
        }

        // ------------------------------------------------------------------ oyun girdisi

        private void HandlePlayingInput(PointerSample p, float now)
        {
            if (_dragSlot < 0)
            {
                // HUD menu dugmesi (isabet testi; temas dugmede basladiysa surukleme baslamaz).
                if (p.Down)
                {
                    Vector2 w = ScreenToWorld(p.Position);
                    if (_taps.Down(_hud.MenuButtonRect.Contains(w) ? 0 : TapTracker.NoButton)) return;
                    for (int i = 0; i < Tray.SlotCount; i++)
                    {
                        if (_game.Tray[i].HasValue && _layout.SlotTouchRect(i, _view).Contains(w))
                        {
                            BeginDrag(i, now);
                            break;
                        }
                    }
                }
                else if (_taps.Armed != TapTracker.NoButton)
                {
                    if (p.Up && _taps.Up(_hud.MenuButtonRect.Contains(ScreenToWorld(p.Position)) ? 0 : TapTracker.NoButton) == 0)
                        GoToMenu(now);
                    return;
                }
            }
            if (_dragSlot < 0) return;

            if (p.Held) UpdateDrag(ScreenToWorld(p.Position));
            if (p.Up) EndDrag(now);
        }

        private void BeginDrag(int slot, float now)
        {
            _dragSlot = slot;
            _returning[slot] = false;
            _liftStart = now;
            _hasAnchor = false;
            _pieces[slot].SetSortingOrder(PieceView.OrderDragged);
        }

        /// <summary>GDD §8: parca parmaga yatayda ortali, alt kenari parmagin 1,2 hucre yukarisinda; capa en yakin gecerli.</summary>
        private void UpdateDrag(Vector2 finger)
        {
            PieceView view = _pieces[_dragSlot];
            Vector2 pivot = new Vector2(finger.x, finger.y + InteractionConfig.DragOffsetCells);
            view.Root.localPosition = new Vector3(pivot.x, pivot.y, 0f); // olcek TickLift'te

            TrayPiece piece = _game.Tray[_dragSlot].Value;
            // Sol-ust hucre merkezinin tahtaya gore konumu (hucre birimi; y asagi). Tam olcek geometrisiyle.
            Vector2 tl = Layout.BoardTopLeft;
            float cornerX = pivot.x - view.Width * 0.5f;
            float cornerTop = pivot.y + view.Height;
            float fx = cornerX - tl.x;
            float fy = tl.y - cornerTop;

            bool found = DropAnchor.TryFind(_game.Grid, piece.Def, fx, fy, InteractionConfig.SnapMaxDistance, out Cell a);
            if (found != _hasAnchor || (found && a != _anchor))
            {
                _hasAnchor = found;
                _anchor = a;
                RenderBoard();
            }
        }

        private void EndDrag(float now)
        {
            int slot = _dragSlot;
            _dragSlot = -1;
            if (_hasAnchor)
            {
                TrayPiece piece = _game.Tray[slot].Value;
                _game.Grid.CopyColorsTo(_colorsBefore);
                MoveResult r = _game.TryPlace(slot, _anchor.X, _anchor.Y);
                _hasAnchor = false;
                if (r.Success)
                {
                    OnPlaced(r, piece, now);
                    return;
                }
            }
            _hasAnchor = false;
            RenderBoard();
            StartReturn(slot, now);
        }

        private void OnPlaced(MoveResult r, TrayPiece piece, float now)
        {
            RenderBoard();
            if (r.Placement.LineCount > 0) _board.StartClearFx(r.Placement, _colorsBefore, piece.Color, now);
            ShowTray();
            UpdateHud();
            if (r.IsGameOver) EnterGameOver(now);
        }

        private void StartReturn(int slot, float now)
        {
            PieceView v = _pieces[slot];
            _returning[slot] = true;
            _retFrom[slot] = v.Root.localPosition;
            _retFromScale[slot] = v.Root.localScale.x;
            _retStart[slot] = now;
        }

        private void TickLift(float now)
        {
            if (_dragSlot < 0) return;
            float t = Mathf.Clamp01((now - _liftStart) / InteractionConfig.LiftDuration);
            float e = 1f - (1f - t) * (1f - t);
            float s = Mathf.LerpUnclamped(InteractionConfig.TrayScale, 1f, e);
            _pieces[_dragSlot].Root.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>GDD §8: gecersiz birakmada 150 ms ease-out ile yuvaya ve tepsi olcegine donus.</summary>
        private void TickReturns(float now)
        {
            for (int i = 0; i < Tray.SlotCount; i++)
            {
                if (!_returning[i]) continue;
                PieceView v = _pieces[i];
                Vector2 rest = v.RestPivot(_layout.SlotCenter(i));
                float t = Mathf.Clamp01((now - _retStart[i]) / InteractionConfig.ReturnDuration);
                float inv = 1f - t;
                float e = 1f - inv * inv * inv; // ease-out (kubik)
                v.SetPose(Vector2.LerpUnclamped(_retFrom[i], rest, e), Mathf.LerpUnclamped(_retFromScale[i], InteractionConfig.TrayScale, e));
                if (t >= 1f)
                {
                    _returning[i] = false;
                    v.SetSortingOrder(PieceView.OrderTray);
                }
            }
        }

        // ------------------------------------------------------------------ oyun sonu (GDD §7)

        private void EnterGameOver(float now)
        {
            _phase = Phase.GameOverWait; // girdi kilitlenir
            _gameOverAt = now;

            int score = _game.Scoring.Score;
            int maxCombo = _game.Stats.MaxCombo;
            _records.Save(Math.Max(_records.BestScore, score), _records.GamesPlayed + 1, Math.Max(_records.BestCombo, maxCombo));
            _saves.Clear();
        }

        private void TickGameOver(float now)
        {
            float el = now - _gameOverAt - InteractionConfig.GameOverDelay;
            if (el < 0f) return;
            float t = Mathf.Clamp01(el / InteractionConfig.GameOverGrayDuration);
            _board.SetGameOverGray(_game.Grid, t);
            if (t < 1f) return;

            int score = _game.Scoring.Score;
            _gameOver.Show(score, Math.Max(_bestAtStart, score), score > _bestAtStart,
                _game.Stats.Moves, _game.Stats.LinesCleared, _game.Stats.MaxCombo);
            _phase = Phase.GameOverPanel;
            _taps.Reset();
        }

        private void HandlePanelInput(PointerSample p, float now)
        {
            if (p.Down) _taps.Down(_gameOver.HitTest(ScreenToWorld(p.Position)));
            if (!p.Up) return;
            int fired = _taps.Up(_gameOver.HitTest(ScreenToWorld(p.Position)));
            if (fired == GameOverView.ButtonReplay) StartNewGame();
            else if (fired == GameOverView.ButtonMenu) GoToMenu(now); // kayit oyun sonunda silindi; menude yalniz "Yeni oyun"
        }

        // ------------------------------------------------------------------ cizim yardimcilari

        private void RenderBoard()
        {
            if (_hasAnchor && _dragSlot >= 0)
            {
                TrayPiece piece = _game.Tray[_dragSlot].Value;
                _board.Render(_game.Grid, true, piece.Def, piece.Color, _anchor.X, _anchor.Y);
            }
            else
            {
                _board.Render(_game.Grid, false, null, 0, 0, 0);
            }
        }

        /// <summary>Tepsiyi Core durumundan cizer; sigmayan parca %40 opak (GDD §8). Yalniz hamleden sonra cagrilir.</summary>
        private void ShowTray()
        {
            for (int i = 0; i < Tray.SlotCount; i++)
            {
                PieceView v = _pieces[i];
                TrayPiece? p = _game.Tray[i];
                if (!p.HasValue)
                {
                    v.Hide();
                    _returning[i] = false;
                    continue;
                }
                if (_dragSlot == i) continue;
                if (!v.Visible || !_returning[i])
                {
                    v.Show(p.Value.Def, p.Value.Color);
                    v.SetSortingOrder(PieceView.OrderTray);
                    v.SetPose(v.RestPivot(_layout.SlotCenter(i)), InteractionConfig.TrayScale);
                    _returning[i] = false;
                }
                v.SetAlpha(_game.SlotFitsAnywhere(i) ? 1f : InteractionConfig.UnfitAlpha);
            }
        }

        private void UpdateHud()
        {
            int score = _game.Scoring.Score;
            _hud.Set(score, Math.Max(_bestAtStart, score), _game.Scoring.Combo);
        }

        private Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector3 w = _cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_cam.transform.position.z));
            return new Vector2(w.x, w.y);
        }

        private void UpdateLayoutIfNeeded()
        {
            int w = Screen.width, h = Screen.height;
            Rect safe = Screen.safeArea;
            if (w == _screenW && h == _screenH && safe == _safe) return;
            _screenW = w;
            _screenH = h;
            _safe = safe;

            _layout = Layout.Choose(safe);
            _layout.FitCamera(w, h, safe, out float size, out Vector2 camPos);
            _cam.orthographicSize = size;
            _cam.transform.position = new Vector3(camPos.x, camPos.y, -10f);
            _ppu = h / (2f * size);
            float viewH = size * 2f, viewW = viewH * ((float)w / Mathf.Max(1, h));
            _view = new Rect(camPos.x - viewW * 0.5f, camPos.y - viewH * 0.5f, viewW, viewH);
            _safeWorld = MenuLayout.ScreenToWorldRect(safe, w, h, camPos, _ppu);
            _minTouch = TouchTarget.MinPixels(Screen.dpi, Mathf.Min(w, h)) / Mathf.Max(1e-4f, _ppu);
            _cover.transform.localPosition = new Vector3(_view.center.x, _view.center.y, 0f);
            _cover.transform.localScale = new Vector3(_view.width + 2f, _view.height + 2f, 1f);

            for (int i = 0; i < Tray.SlotCount; i++)
            {
                if (i == _dragSlot || _returning[i] || !_pieces[i].Visible) continue;
                _pieces[i].SetPose(_pieces[i].RestPivot(_layout.SlotCenter(i)), InteractionConfig.TrayScale);
            }
            _hud.Layout(_layout.Hud, _minTouch);
            _hud.Apply(_cam, _ppu);
            _gameOver.Layout(_view, _minTouch);
            _gameOver.Apply(_cam, _ppu);
            _menu.Layout(_safeWorld, _view.width, _minTouch);
            _menu.Apply(_cam, _ppu);
        }

        // ------------------------------------------------------------------ kalicilik (GDD §7.3)

        private void OnApplicationPause(bool paused)
        {
            if (!paused || _screen != AppScreen.Game) return;
            CancelDrag();
            PersistProgress();
        }

        private void OnApplicationQuit()
        {
            if (_screen != AppScreen.Game) return; // menude kayit zaten guncel
            PersistProgress();
        }

        private void CancelDrag()
        {
            _taps.Reset();
            if (_dragSlot < 0) return;
            int slot = _dragSlot;
            _dragSlot = -1;
            _hasAnchor = false;
            _returning[slot] = false;
            PieceView v = _pieces[slot];
            v.SetSortingOrder(PieceView.OrderTray);
            v.SetPose(v.RestPivot(_layout.SlotCenter(slot)), InteractionConfig.TrayScale);
            RenderBoard();
        }

        /// <summary>Arka plana alinirken: anlik skor rekoru astiysa rekor hemen yazilir; yarim oyun kaydedilir.</summary>
        private void PersistProgress()
        {
            if (_game == null || _records == null) return;
            if (_phase != Phase.Playing || _game.IsGameOver)
            {
                _saves.Clear();
                return;
            }
            int score = _game.Scoring.Score;
            if (score > _records.BestScore)
                _records.Save(score, _records.GamesPlayed, Math.Max(_records.BestCombo, _game.Stats.MaxCombo));
            _saves.Save(_game.ToSnapshot(), _bestAtStart);
        }
    }
}
