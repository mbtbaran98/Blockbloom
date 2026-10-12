using System;
using Blockbloom.Core;
using UnityEngine;

namespace Blockbloom.Game
{
    /// <summary>GDD §7.3 kalici rekor: PlayerPrefs (cihazda, cevrimdisi). Degerler kurulumda bir kez okunur.</summary>
    public sealed class PlayerPrefsRecordStore : IRecordStore
    {
        public const string DefaultPrefix = "bb.records.";

        private readonly string _bestKey, _playedKey, _comboKey;

        public PlayerPrefsRecordStore(string prefix = DefaultPrefix)
        {
            if (string.IsNullOrEmpty(prefix)) throw new ArgumentException("Onek bos olamaz.", nameof(prefix));
            _bestKey = prefix + "bestScore";
            _playedKey = prefix + "gamesPlayed";
            _comboKey = prefix + "bestCombo";
            BestScore = Mathf.Max(0, PlayerPrefs.GetInt(_bestKey, 0));
            GamesPlayed = Mathf.Max(0, PlayerPrefs.GetInt(_playedKey, 0));
            BestCombo = Mathf.Max(0, PlayerPrefs.GetInt(_comboKey, 0));
        }

        public int BestScore { get; private set; }
        public int GamesPlayed { get; private set; }
        public int BestCombo { get; private set; }

        public void Save(int bestScore, int gamesPlayed, int bestCombo)
        {
            if (bestScore < 0 || gamesPlayed < 0 || bestCombo < 0) throw new ArgumentOutOfRangeException(nameof(bestScore));
            BestScore = bestScore;
            GamesPlayed = gamesPlayed;
            BestCombo = bestCombo;
            PlayerPrefs.SetInt(_bestKey, bestScore);
            PlayerPrefs.SetInt(_playedKey, gamesPlayed);
            PlayerPrefs.SetInt(_comboKey, bestCombo);
            PlayerPrefs.Save();
        }
    }
}
