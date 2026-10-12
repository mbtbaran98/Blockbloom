using Blockbloom.Core;
using Blockbloom.Game;
using NUnit.Framework;
using UnityEngine;

namespace Blockbloom.Tests.EditMode
{
    /// <summary>PlayerPrefs + JsonUtility uzerinden yarim oyun kaydi gidis-donusu (gercek Unity serilestiricisi).</summary>
    public class SnapshotStoreTests
    {
        private const string Key = "bb.test.savedGame";
        private const string RecordPrefix = "bb.test.records.";

        [TearDown]
        public void Cleanup()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.DeleteKey(RecordPrefix + "bestScore");
            PlayerPrefs.DeleteKey(RecordPrefix + "gamesPlayed");
            PlayerPrefs.DeleteKey(RecordPrefix + "bestCombo");
            PlayerPrefs.Save();
        }

        private static GameState PlayFewMoves(ulong seed, int moves)
        {
            GameState gs = GameState.NewGame(seed);
            var buffer = new System.Collections.Generic.List<(int slot, Cell anchor)>();
            for (int i = 0; i < moves && !gs.IsGameOver; i++)
            {
                gs.LegalMoves(buffer);
                var m = buffer[buffer.Count / 2];
                gs.TryPlace(m.slot, m.anchor.X, m.anchor.Y);
            }
            return gs;
        }

        [Test]
        public void RoundTrip_PreservesFullState_IncludingEmptySlotAndLargeUlongs()
        {
            GameState gs = PlayFewMoves(ulong.MaxValue - 12345UL, 4); // 4 hamle: en az bir bos yuva olabilir
            GameSnapshot before = gs.ToSnapshot();
            var store = new SnapshotStore(Key);
            store.Save(before, 777);

            Assert.That(store.TryLoad(out GameSnapshot after, out int bestAtStart), Is.True);
            Assert.That(bestAtStart, Is.EqualTo(777));
            Assert.That(after.Seed, Is.EqualTo(before.Seed));
            Assert.That(after.Bits, Is.EqualTo(before.Bits));
            Assert.That(after.Colors, Is.EqualTo(before.Colors));
            Assert.That(after.TrayIds, Is.EqualTo(before.TrayIds)); // bos yuva null olarak geri gelir
            Assert.That(after.TrayColors, Is.EqualTo(before.TrayColors));
            Assert.That(after.RngState, Is.EqualTo(before.RngState));
            Assert.That(after.RngInc, Is.EqualTo(before.RngInc));
            Assert.That(after.Score, Is.EqualTo(before.Score));
            Assert.That(after.Combo, Is.EqualTo(before.Combo));
            Assert.That(after.MaxCombo, Is.EqualTo(before.MaxCombo));
            Assert.That(after.TraysWithoutClear, Is.EqualTo(before.TraysWithoutClear));
            Assert.That(after.ClearedInCurrentTray, Is.EqualTo(before.ClearedInCurrentTray));
            Assert.That(after.Stats.Moves, Is.EqualTo(before.Stats.Moves));
            Assert.That(after.Stats.TraysDealt, Is.EqualTo(before.Stats.TraysDealt));

            // Devam eden oyun kesintisiz oyunla ayni ilerler.
            GameState resumed = GameState.FromSnapshot(after);
            Assert.That(resumed.ToSnapshot().RngState, Is.EqualTo(before.RngState));
        }

        [Test]
        public void EmptySlot_IsStoredAndRestoredAsNull()
        {
            GameState gs = PlayFewMoves(42UL, 1);
            GameSnapshot s = gs.ToSnapshot();
            Assert.That(System.Array.IndexOf(s.TrayIds, null), Is.GreaterThanOrEqualTo(0));
            var store = new SnapshotStore(Key);
            store.Save(s, 0);
            Assert.That(store.TryLoad(out GameSnapshot back, out _), Is.True);
            Assert.That(back.TrayIds, Is.EqualTo(s.TrayIds));
        }

        [Test]
        public void CorruptSave_IsDeleted_AndReportsNoSave()
        {
            PlayerPrefs.SetString(Key, "{bozuk");
            var store = new SnapshotStore(Key);
            LogAssertExpectWarning();
            Assert.That(store.TryLoad(out _, out _), Is.False);
            Assert.That(store.HasSave, Is.False);
        }

        private static void LogAssertExpectWarning()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("kayitli oyun okunamadi"));
        }

        [Test]
        public void RecordStore_PersistsAcrossInstances()
        {
            var a = new PlayerPrefsRecordStore(RecordPrefix);
            a.Save(1234, 5, 7);
            var b = new PlayerPrefsRecordStore(RecordPrefix);
            Assert.That(b.BestScore, Is.EqualTo(1234));
            Assert.That(b.GamesPlayed, Is.EqualTo(5));
            Assert.That(b.BestCombo, Is.EqualTo(7));
        }
    }
}
