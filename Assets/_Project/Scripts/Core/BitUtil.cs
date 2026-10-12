using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Blockbloom.Core
{
    /// <summary>Bit tahtasi yardimcilari. netstandard2.1 uyumlu (BitOperations kullanilmaz).</summary>
    internal static class BitUtil
    {
        public const ulong RowMask0 = 0xFFUL;
        public const ulong ColMask0 = 0x0101010101010101UL;

        /// <summary>8 bitlik maske -> artan sirada indeks listesi (onbellekli, salt okunur, ayirma yok).</summary>
        public static readonly IReadOnlyList<int>[] IndexLists = BuildIndexLists();

        public static int PopCount(ulong x)
        {
            unchecked
            {
                x -= (x >> 1) & 0x5555555555555555UL;
                x = (x & 0x3333333333333333UL) + ((x >> 2) & 0x3333333333333333UL);
                x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
                return (int)((x * 0x0101010101010101UL) >> 56);
            }
        }

        /// <summary>Tam dolu satirlarin maskesi (bit y = satir y dolu).</summary>
        public static byte FullRows(ulong bits)
        {
            int rows = 0;
            for (int y = 0; y < CoreConfig.BoardSize; y++)
            {
                if (((bits >> (y * 8)) & RowMask0) == RowMask0)
                {
                    rows |= 1 << y;
                }
            }
            return (byte)rows;
        }

        /// <summary>Tam dolu sutunlarin maskesi (bit x = sutun x dolu). Sekiz satir baytinin AND'i.</summary>
        public static byte FullCols(ulong bits)
        {
            ulong t = bits & (bits >> 32);
            t &= t >> 16;
            t &= t >> 8;
            return (byte)t;
        }

        /// <summary>Satir/sutun maskelerinden temizlenecek hucre maskesi.</summary>
        public static ulong ClearMask(byte rows, byte cols)
        {
            ulong mask = 0;
            for (int i = 0; i < CoreConfig.BoardSize; i++)
            {
                if ((rows & (1 << i)) != 0) mask |= RowMask0 << (i * 8);
                if ((cols & (1 << i)) != 0) mask |= ColMask0 << i;
            }
            return mask;
        }

        private static IReadOnlyList<int>[] BuildIndexLists()
        {
            var result = new IReadOnlyList<int>[256];
            for (int m = 0; m < 256; m++)
            {
                var list = new List<int>(8);
                for (int i = 0; i < 8; i++)
                {
                    if ((m & (1 << i)) != 0) list.Add(i);
                }
                result[m] = new ReadOnlyCollection<int>(list.ToArray());
            }
            return result;
        }
    }
}
