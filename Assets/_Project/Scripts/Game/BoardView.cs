using Blockbloom.Core;
using UnityEngine;
using Grid = Blockbloom.Core.Grid;

namespace Blockbloom.Game
{
    /// <summary>
    /// 8x8 tahta gorunumu. Tum SpriteRenderer'lar kurulumda bir kez olusturulur (havuz: hucre basina 4 katman);
    /// sonradan Instantiate/Destroy yok. Yeniden cizim yalniz durum ya da onizleme degisince yapilir.
    /// Katmanlar: zemin (bos hucre) &lt; karo (dolu/onizleme) &lt; ust (cizgi vurgusu / oyun sonu grisi) &lt; patlama solmasi.
    /// </summary>
    public sealed class BoardView
    {
        public const int OrderBase = 0;
        public const int OrderBlock = 1;
        public const int OrderOverlay = 2;
        public const int OrderFx = 3;

        private const int N = CoreConfig.BoardSize;
        private const int Count = CoreConfig.CellCount;

        private readonly SpriteRenderer[] _block = new SpriteRenderer[Count];
        private readonly SpriteRenderer[] _overlay = new SpriteRenderer[Count];
        private readonly SpriteRenderer[] _fx = new SpriteRenderer[Count];
        private readonly Transform[] _fxTf = new Transform[Count];
        private readonly float[] _fxStart = new float[Count];
        private readonly byte[] _colors = new byte[Count];
        private int _fxActive;

        public BoardView(Transform parent)
        {
            var root = new GameObject("Board").transform;
            root.SetParent(parent, false);
            for (int i = 0; i < Count; i++)
            {
                int x = i % N, y = i / N;
                Vector3 pos = Layout.CellCenter(x, y);
                SpriteRenderer bg = NewRenderer(root, "Cell" + i, pos, OrderBase);
                bg.sprite = SpriteFactory.Empty;
                _block[i] = NewRenderer(root, "Block" + i, pos, OrderBlock);
                _block[i].enabled = false;
                _overlay[i] = NewRenderer(root, "Overlay" + i, pos, OrderOverlay);
                _overlay[i].enabled = false;
                _fx[i] = NewRenderer(root, "Fx" + i, pos, OrderFx);
                _fx[i].enabled = false;
                _fxTf[i] = _fx[i].transform;
                _fxStart[i] = -1f;
            }
        }

        private static SpriteRenderer NewRenderer(Transform parent, string name, Vector3 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>
        /// Tahtayi ciz. hasPreview ise parca (piece, ax, ay) %45 opak gosterilir ve patlayacak cizgilerin tum hucreleri
        /// parlaklik katmaniyla vurgulanir (GDD §8). Bellek ayirmaz.
        /// </summary>
        public void Render(Grid grid, bool hasPreview, PieceDef piece, byte pieceColor, int ax, int ay)
        {
            grid.CopyColorsTo(_colors);
            ulong previewMask = 0UL;
            LinePreview lines = default(LinePreview);
            if (hasPreview)
            {
                previewMask = piece.MaskAt(ax, ay);
                lines = grid.PreviewLines(piece, ax, ay);
            }
            bool anyLines = lines.LineCount > 0;
            Color previewTint = new Color(1f, 1f, 1f, InteractionConfig.PreviewAlpha);
            Color glowTint = new Color(1f, 1f, 1f, InteractionConfig.LineHighlightAlpha);

            for (int i = 0; i < Count; i++)
            {
                byte c = _colors[i];
                bool inPreview = (previewMask & (1UL << i)) != 0;
                SpriteRenderer b = _block[i];
                if (c != 0)
                {
                    b.enabled = true;
                    b.sprite = SpriteFactory.TileSprite(c);
                    b.color = Color.white;
                }
                else if (inPreview)
                {
                    b.enabled = true;
                    b.sprite = SpriteFactory.TileSprite(pieceColor);
                    b.color = previewTint;
                }
                else
                {
                    b.enabled = false;
                }

                SpriteRenderer o = _overlay[i];
                if (anyLines && lines.Contains(i % N, i / N))
                {
                    o.enabled = true;
                    o.sprite = SpriteFactory.Glow;
                    o.color = glowTint;
                }
                else
                {
                    o.enabled = false;
                }
            }
        }

        /// <summary>
        /// Patlayan hucreler icin kisa solma (M0, &lt;= 150 ms). colorsBefore: hamleden once tahta renkleri;
        /// o an bos olan (yeni yerlestirilen) hucreler parca rengini alir.
        /// </summary>
        public void StartClearFx(PlacementResult placement, byte[] colorsBefore, byte pieceColor, float now)
        {
            var cells = placement.ClearedCells;
            for (int k = 0; k < cells.Count; k++)
            {
                int i = cells[k].Index;
                byte c = colorsBefore[i] != 0 ? colorsBefore[i] : pieceColor;
                SpriteRenderer fx = _fx[i];
                fx.sprite = SpriteFactory.TileSprite(c);
                fx.color = Color.white;
                fx.enabled = true;
                _fxTf[i].localScale = Vector3.one;
                if (_fxStart[i] < 0f) _fxActive++;
                _fxStart[i] = now;
            }
        }

        /// <summary>Patlama solmasini ilerlet. Etkin solma yoksa hemen doner.</summary>
        public void TickFx(float now)
        {
            if (_fxActive == 0) return;
            for (int i = 0; i < Count; i++)
            {
                float start = _fxStart[i];
                if (start < 0f) continue;
                float t = (now - start) / InteractionConfig.ClearFadeDuration;
                if (t >= 1f)
                {
                    _fx[i].enabled = false;
                    _fxStart[i] = -1f;
                    _fxActive--;
                    continue;
                }
                float e = 1f - (1f - t) * (1f - t);
                _fxTf[i].localScale = new Vector3(1f - 0.4f * e, 1f - 0.4f * e, 1f);
                _fx[i].color = new Color(1f, 1f, 1f, 1f - e);
            }
        }

        public void ClearAllFx()
        {
            for (int i = 0; i < Count; i++)
            {
                _fx[i].enabled = false;
                _fxStart[i] = -1f;
            }
            _fxActive = 0;
        }

        /// <summary>GDD §7.1: dolu hucreler griye solar (t 0..1). Render'dan sonra cagrilir.</summary>
        public void SetGameOverGray(Grid grid, float t)
        {
            grid.CopyColorsTo(_colors);
            Color tint = new Color(1f, 1f, 1f, Mathf.Clamp01(t));
            for (int i = 0; i < Count; i++)
            {
                SpriteRenderer o = _overlay[i];
                if (_colors[i] == 0) { o.enabled = false; continue; }
                o.enabled = true;
                o.sprite = SpriteFactory.Gray;
                o.color = tint;
            }
        }
    }
}
