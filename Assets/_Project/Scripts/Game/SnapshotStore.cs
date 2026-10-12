using System;
using System.Globalization;
using System.Text;
using Blockbloom.Core;
using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>
    /// Yarida kalan oyunun Unity tarafi kayit bicimi (PlayerPrefs'te JSON). JsonUtility'nin ulong ve null dizi ogesi
    /// davranisina guvenmemek icin ulong'lar ondalik dizgi, renkler onaltilik dizgi, bos yuva "" olarak yazilir.
    /// BestAtStart: GDD §7.2 oyunBasiRekor; devam ettirilen oyunda depodaki guncel rekor yerine bu kullanilir.
    /// </summary>
    [Serializable]
    public sealed class SavedGameDto
    {
        public int format = SnapshotCodec.FormatVersion;
        public int version;
        public string seed;
        public string bits;
        public string colors;
        public string[] trayIds;
        public int[] trayColors;
        public int score;
        public int combo;
        public int maxCombo;
        public int traysWithoutClear;
        public bool clearedInCurrentTray;
        public string rngState;
        public string rngInc;
        public int moves;
        public int linesCleared;
        public int statsMaxCombo;
        public int perfectClears;
        public int traysDealt;
        public int bestAtStart;
    }

    /// <summary>GameSnapshot &lt;-&gt; SavedGameDto donusumu. Saf C# (UnityEngine kullanmaz).</summary>
    public static class SnapshotCodec
    {
        public const int FormatVersion = 1;

        public static SavedGameDto ToDto(GameSnapshot s, int bestAtStart)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            var d = new SavedGameDto();
            d.version = s.Version;
            d.seed = U64(s.Seed);
            d.bits = U64(s.Bits);
            d.colors = Hex(s.Colors);
            d.trayIds = new string[s.TrayIds.Length];
            d.trayColors = new int[s.TrayColors.Length];
            for (int i = 0; i < s.TrayIds.Length; i++) d.trayIds[i] = s.TrayIds[i] ?? "";
            for (int i = 0; i < s.TrayColors.Length; i++) d.trayColors[i] = s.TrayColors[i];
            d.score = s.Score;
            d.combo = s.Combo;
            d.maxCombo = s.MaxCombo;
            d.traysWithoutClear = s.TraysWithoutClear;
            d.clearedInCurrentTray = s.ClearedInCurrentTray;
            d.rngState = U64(s.RngState);
            d.rngInc = U64(s.RngInc);
            d.moves = s.Stats.Moves;
            d.linesCleared = s.Stats.LinesCleared;
            d.statsMaxCombo = s.Stats.MaxCombo;
            d.perfectClears = s.Stats.PerfectClears;
            d.traysDealt = s.Stats.TraysDealt;
            d.bestAtStart = bestAtStart;
            return d;
        }

        /// <summary>Bicim hatasi -> FormatException/ArgumentException. Oyun tutarliligi GameState.FromSnapshot'ta denetlenir.</summary>
        public static GameSnapshot FromDto(SavedGameDto d, out int bestAtStart)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            if (d.format != FormatVersion) throw new FormatException("Desteklenmeyen kayit bicimi: " + d.format);
            if (d.trayIds == null || d.trayColors == null || d.trayIds.Length != d.trayColors.Length)
                throw new FormatException("Tepsi verisi eksik.");
            var s = new GameSnapshot();
            s.Version = d.version;
            s.Seed = ParseU64(d.seed);
            s.Bits = ParseU64(d.bits);
            s.Colors = ParseHex(d.colors);
            s.TrayIds = new string[d.trayIds.Length];
            s.TrayColors = new byte[d.trayColors.Length];
            for (int i = 0; i < d.trayIds.Length; i++) s.TrayIds[i] = string.IsNullOrEmpty(d.trayIds[i]) ? null : d.trayIds[i];
            for (int i = 0; i < d.trayColors.Length; i++)
            {
                if (d.trayColors[i] < 0 || d.trayColors[i] > 255) throw new FormatException("Renk aralik disi.");
                s.TrayColors[i] = (byte)d.trayColors[i];
            }
            s.Score = d.score;
            s.Combo = d.combo;
            s.MaxCombo = d.maxCombo;
            s.TraysWithoutClear = d.traysWithoutClear;
            s.ClearedInCurrentTray = d.clearedInCurrentTray;
            s.RngState = ParseU64(d.rngState);
            s.RngInc = ParseU64(d.rngInc);
            s.Stats = new GameStats
            {
                Moves = d.moves,
                LinesCleared = d.linesCleared,
                MaxCombo = d.statsMaxCombo,
                PerfectClears = d.perfectClears,
                TraysDealt = d.traysDealt,
            };
            if (d.bestAtStart < 0) throw new FormatException("bestAtStart negatif.");
            bestAtStart = d.bestAtStart;
            return s;
        }

        private static string U64(ulong v) => v.ToString(CultureInfo.InvariantCulture);

        private static ulong ParseU64(string s)
        {
            if (string.IsNullOrEmpty(s)) throw new FormatException("Bos sayi alani.");
            return ulong.Parse(s, NumberStyles.None, CultureInfo.InvariantCulture);
        }

        private static string Hex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private static byte[] ParseHex(string s)
        {
            if (s == null || (s.Length & 1) != 0) throw new FormatException("Gecersiz onaltilik renk dizgisi.");
            var b = new byte[s.Length / 2];
            for (int i = 0; i < b.Length; i++)
                b[i] = byte.Parse(s.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
            return b;
        }
    }

    /// <summary>Yarida kalan oyunu PlayerPrefs'e JSON olarak yazar/okur.</summary>
    public sealed class SnapshotStore
    {
        public const string DefaultKey = "bb.savedGame.v1";

        private readonly string _key;

        public SnapshotStore(string key = DefaultKey)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Anahtar bos olamaz.", nameof(key));
            _key = key;
        }

        public bool HasSave => PlayerPrefs.HasKey(_key);

        public void Save(GameSnapshot snapshot, int bestAtStart)
        {
            string json = JsonUtility.ToJson(SnapshotCodec.ToDto(snapshot, bestAtStart));
            PlayerPrefs.SetString(_key, json);
            PlayerPrefs.Save();
        }

        /// <summary>Kayit yoksa ya da okunamazsa false (bozuk kayit silinir).</summary>
        public bool TryLoad(out GameSnapshot snapshot, out int bestAtStart)
        {
            snapshot = null;
            bestAtStart = 0;
            if (!PlayerPrefs.HasKey(_key)) return false;
            try
            {
                SavedGameDto dto = JsonUtility.FromJson<SavedGameDto>(PlayerPrefs.GetString(_key));
                snapshot = SnapshotCodec.FromDto(dto, out bestAtStart);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Blockbloom: kayitli oyun okunamadi, siliniyor. " + e.Message);
                Clear();
                snapshot = null;
                bestAtStart = 0;
                return false;
            }
        }

        public void Clear()
        {
            if (!PlayerPrefs.HasKey(_key)) return;
            PlayerPrefs.DeleteKey(_key);
            PlayerPrefs.Save();
        }
    }
}
