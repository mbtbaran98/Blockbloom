using Blockbloom.Core;
using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// Bir tepsi yuvasinin parca gorunumu. Pivot parcanin alt-orta noktasidir (GDD §8 "alt kenar parmagin 1,2 hucre
    /// yukarisinda" dogrudan uygulanir). 9 hucrelik havuz (en buyuk parca O9) kurulumda olusturulur.
    /// </summary>
    public sealed class PieceView
    {
        public const int OrderTray = 10;
        public const int OrderDragged = 20;
        private const int MaxCells = 9;

        private readonly SpriteRenderer[] _cells = new SpriteRenderer[MaxCells];
        private int _count;

        public PieceView(Transform parent, int slot)
        {
            var go = new GameObject("Slot" + slot);
            Root = go.transform;
            Root.SetParent(parent, false);
            for (int i = 0; i < MaxCells; i++)
            {
                var c = new GameObject("C" + i);
                c.transform.SetParent(Root, false);
                var sr = c.AddComponent<SpriteRenderer>();
                sr.sortingOrder = OrderTray;
                sr.enabled = false;
                _cells[i] = sr;
            }
        }

        public Transform Root { get; }
        public bool Visible { get; private set; }
        /// <summary>Hucre biriminde genislik/yukseklik (olcek 1).</summary>
        public float Width { get; private set; }
        public float Height { get; private set; }

        public void Show(PieceDef def, byte colorId)
        {
            Width = def.Width;
            Height = def.Height;
            var cells = def.Cells;
            _count = cells.Count;
            Sprite sprite = SpriteFactory.TileSprite(colorId);
            for (int i = 0; i < MaxCells; i++)
            {
                SpriteRenderer sr = _cells[i];
                if (i < _count)
                {
                    Cell c = cells[i];
                    sr.transform.localPosition = new Vector3(-Width * 0.5f + c.X + 0.5f, Height - c.Y - 0.5f, 0f);
                    sr.sprite = sprite;
                    sr.enabled = true;
                }
                else
                {
                    sr.enabled = false;
                }
            }
            Visible = true;
        }

        public void Hide()
        {
            for (int i = 0; i < MaxCells; i++) _cells[i].enabled = false;
            Visible = false;
        }

        public void SetAlpha(float a)
        {
            var col = new Color(1f, 1f, 1f, a);
            for (int i = 0; i < _count; i++) _cells[i].color = col;
        }

        public void SetSortingOrder(int order)
        {
            for (int i = 0; i < MaxCells; i++) _cells[i].sortingOrder = order;
        }

        public void SetPose(Vector2 pivot, float scale)
        {
            Root.localPosition = new Vector3(pivot.x, pivot.y, 0f);
            Root.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Yuvada dururken pivot: parca yuva merkezinde (tepsi olceginde).</summary>
        public Vector2 RestPivot(Vector2 slotCenter)
        {
            return new Vector2(slotCenter.x, slotCenter.y - Height * InteractionConfig.TrayScale * 0.5f);
        }
    }
}
