using System;
using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// CLAUDE.md "Gorsel kimlik" paleti. GDD §3.1 renk kimligi 1..6 bu sirayla: mavi, kirmizi, sari, yesil, pembe, mor.
    /// Degerler art-ui tarafindan netlestirilecek baslangic degerleridir.
    /// </summary>
    public static class Palette
    {
        public static readonly Color32 Background = new Color32(0x18, 0x11, 0x4C, 0xFF);
        public static readonly Color32 Accent = new Color32(0x1B, 0xD3, 0xF8, 0xFF);

        /// <summary>Bos hucre: zeminden biraz acik (M0 secimi; palette yok, art-ui netlestirir).</summary>
        public static readonly Color32 EmptyCell = new Color32(0x26, 0x1D, 0x6A, 0xFF);
        /// <summary>Oyun sonu gri (GDD §7.1). M0 secimi.</summary>
        public static readonly Color32 GameOverGray = new Color32(0x86, 0x82, 0xA0, 0xFF);
        /// <summary>Oyun sonu paneli. M0 secimi.</summary>
        public static readonly Color32 Panel = new Color32(0x2C, 0x22, 0x7A, 0xFF);
        /// <summary>Ikincil dugme zemini (menu "Yeni oyun" kayit varken, HUD menu dugmesi, oyun sonu "Menu"). M0 secimi.</summary>
        public static readonly Color32 ButtonSecondary = new Color32(0x45, 0x38, 0xA6, 0xFF);
        public static readonly Color32 Text = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color32 TextDim = new Color32(0xB9, 0xB2, 0xE8, 0xFF);

        private static readonly Color32[] Tiles =
        {
            new Color32(0x19, 0x9F, 0xEF, 0xFF), // 1 mavi
            new Color32(0xF2, 0x43, 0x3B, 0xFF), // 2 kirmizi
            new Color32(0xFD, 0xDA, 0x23, 0xFF), // 3 sari
            new Color32(0x76, 0xCF, 0x36, 0xFF), // 4 yesil
            new Color32(0xEA, 0x56, 0xA7, 0xFF), // 5 pembe
            new Color32(0xAD, 0x48, 0xE3, 0xFF), // 6 mor
        };

        public const int TileColorCount = 6;

        /// <summary>Renk kimligi 1..6 -> karo rengi. Aralik disi -> ArgumentOutOfRangeException.</summary>
        public static Color32 Tile(int colorId)
        {
            if (colorId < 1 || colorId > TileColorCount) throw new ArgumentOutOfRangeException(nameof(colorId), colorId, "Renk kimligi 1..6 olmali.");
            return Tiles[colorId - 1];
        }
    }
}
