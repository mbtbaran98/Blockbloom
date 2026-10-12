using System;
using Blockbloom.Core;
using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// M0 oyun denetleyicisi: sahneyi kodla kurar, girdiyi okur, Core'u (GameState) surer, gorunumleri gunceller.
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
        private readonly PointerInput _input = new PointerInput();

        // Yerlesim
        private Layout _layout;
        private int _screenW = -1, _screenH = -1;
        private Rect _safe;
        private Rect _view;
        private float _ppu;

        // Oyun
        private GameState _game;
        private IRecordStore _records;
        private SnapshotStore _saves;
        private int _bestAtStart;
        private Phase _phase;
        private float _gameOverAt;
        private bool _buttonArmed;
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
            StartOrResume();
            UpdateLayoutIfNeeded(); // ilk kare cizilmeden yerlesim hazir
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
            _board = new BoardView(_world);
            var trayRoot = new GameObject("Tray").transform;
            trayRoot.SetParent(_world, false);
            for (int i = 0; i < _pieces.Length; i++) _pieces[i] = new PieceView(trayRoot, i);

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _hud = new HudView(canvasGo.transform, font);
            _gameOver = new GameOverView(_world, canvasGo.transform, font);
        }

        private void StartOrResume()
        {
            if (_saves.TryLoad(out GameSnapshot snap, out int bestAtStart))
            {
                try
                {
                    GameState restored = GameState.FromSnapshot(snap);
                    if (!restored.IsGameOver)
                    {
                        BeginGame(restored, bestAtStart);
                        return;
                    }
                }
                catch (ArgumentException e)
                {
                    Debug.LogWarning("Blockbloom: kayitli oyun gecersiz, yeni oyun baslatiliyor. " + e.Message);
                }
                _saves.Clear();
            }
            StartNewGame();
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
            _buttonArmed = false;
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
            if (_game == null) return;
            float now = Time.unscaledTime;
            UpdateLayoutIfNeeded();

            PointerSample p = _input.Read();
            switch (_phase)
            {
                case Phase.Playing:
                    HandlePlayingInput(p, now);
                    break;
                case Phase.GameOverWait:
                    TickGameOver(now);
                    break;
                case Phase.GameOverPanel:
                    HandlePanelInput(p);
                    break;
            }

            TickLift(now);
            TickReturns(now);
            _board.TickFx(now);
        }

        private void HandlePlayingInput(PointerSample p, float now)
        {
            if (_dragSlot < 0 && p.Down)
            {
                Vector2 w = ScreenToWorld(p.Position);
                for (int i = 0; i < Tray.SlotCount; i++)
                {
                    if (_game.Tray[i].HasValue && _layout.SlotTouchRect(i, _view).Contains(w))
                    {
                        BeginDrag(i, now);
                        break;
                    }
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
            _buttonArmed = false;
        }

        private void HandlePanelInput(PointerSample p)
        {
            Rect r = _gameOver.ButtonRect;
            if (p.Down) _buttonArmed = r.Contains(ScreenToWorld(p.Position));
            if (p.Up)
            {
                bool hit = _buttonArmed && r.Contains(ScreenToWorld(p.Position));
                _buttonArmed = false;
                if (hit) StartNewGame();
            }
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

            for (int i = 0; i < Tray.SlotCount; i++)
            {
                if (i == _dragSlot || _returning[i] || !_pieces[i].Visible) continue;
                _pieces[i].SetPose(_pieces[i].RestPivot(_layout.SlotCenter(i)), InteractionConfig.TrayScale);
            }
            _hud.Layout(_layout.Hud);
            _hud.Apply(_cam, _ppu);
            _gameOver.Layout(_view);
            _gameOver.Apply(_cam, _ppu);
        }

        // ------------------------------------------------------------------ kalicilik (GDD §7.3)

        private void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            CancelDrag();
            PersistProgress();
        }

        private void OnApplicationQuit()
        {
            PersistProgress();
        }

        private void CancelDrag()
        {
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
