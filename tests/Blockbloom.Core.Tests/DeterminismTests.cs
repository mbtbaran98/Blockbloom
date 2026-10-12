using System;
using System.Collections.Generic;
using System.Text.Json;
using Blockbloom.Core;
using NUnit.Framework;
using static Blockbloom.Core.Tests.TestUtil;

namespace Blockbloom.Core.Tests
{
    public class DeterminismTests
    {
        private static List<string> PlayRecorded(ulong seed, int maxMoves, out GameSnapshot final)
        {
            var log = new List<string>();
            var buffer = new List<(int slot, Cell anchor)>();
            GameState gs = GameState.NewGame(seed);
            var bot = new Pcg32(seed + 1000000UL);
            int moves = 0;
            while (!gs.IsGameOver && moves < maxMoves)
            {
                var m = PickRandomMove(gs, bot, buffer);
                MoveResult r = gs.TryPlace(m.slot, m.anchor.X, m.anchor.Y);
                log.Add(m.slot + ":" + m.anchor + ":" + r.Score.Total + ":" + r.NewTrayMode + ":" + gs.Grid.Bits);
                moves++;
            }
            final = gs.ToSnapshot();
            return log;
        }

        [Test]
        public void SameSeedSameMoves_SameGame()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                List<string> a = PlayRecorded(seed, 5000, out GameSnapshot fa);
                List<string> b = PlayRecorded(seed, 5000, out GameSnapshot fb);
                Assert.That(b, Is.EqualTo(a));
                AssertSnapshotsEqual(fa, fb);
            }
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentTrays()
        {
            var firstTrays = new HashSet<string>();
            for (ulong seed = 1; seed <= 50; seed++)
            {
                GameState gs = GameState.NewGame(seed);
                firstTrays.Add(gs.Tray[0].Value.Def.Id + gs.Tray[1].Value.Def.Id + gs.Tray[2].Value.Def.Id
                    + gs.Tray[0].Value.Color + gs.Tray[1].Value.Color + gs.Tray[2].Value.Color);
            }
            Assert.That(firstTrays.Count, Is.GreaterThan(40));
        }

        [Test]
        public void Snapshot_RoundTrip_IsIdentity()
        {
            var buffer = new List<(int slot, Cell anchor)>();
            GameState gs = GameState.NewGame(9UL);
            var bot = new Pcg32(9UL + 1000000UL);
            for (int i = 0; i < 40 && !gs.IsGameOver; i++)
            {
                GameSnapshot s = gs.ToSnapshot();
                GameState restored = GameState.FromSnapshot(s);
                AssertSnapshotsEqual(s, restored.ToSnapshot());
                Assert.That(restored.IsGameOver, Is.EqualTo(gs.IsGameOver));
                var m = PickRandomMove(gs, bot, buffer);
                gs.TryPlace(m.slot, m.anchor.X, m.anchor.Y);
            }
        }

        [Test]
        public void ResumeFromSnapshot_EqualsUninterruptedGame()
        {
            var buffer = new List<(int slot, Cell anchor)>();
            for (ulong seed = 100; seed < 130; seed++)
            {
                GameState a = GameState.NewGame(seed);
                var botA = new Pcg32(seed + 1000000UL);
                int splitAt = (int)(seed % 17) + 1;
                for (int i = 0; i < splitAt && !a.IsGameOver; i++)
                {
                    var m = PickRandomMove(a, botA, buffer);
                    a.TryPlace(m.slot, m.anchor.X, m.anchor.Y);
                }

                GameState b = GameState.FromSnapshot(a.ToSnapshot());
                var st = botA.GetState();
                var botB = Pcg32.FromState(st.State, st.Inc);
                while (!a.IsGameOver)
                {
                    var ma = PickRandomMove(a, botA, buffer);
                    var mb = PickRandomMove(b, botB, buffer);
                    Assert.That(mb, Is.EqualTo(ma));
                    MoveResult ra = a.TryPlace(ma.slot, ma.anchor.X, ma.anchor.Y);
                    MoveResult rb = b.TryPlace(mb.slot, mb.anchor.X, mb.anchor.Y);
                    Assert.That(rb.Score.Total, Is.EqualTo(ra.Score.Total));
                    Assert.That(rb.NewTrayMode, Is.EqualTo(ra.NewTrayMode));
                    Assert.That(rb.IsGameOver, Is.EqualTo(ra.IsGameOver));
                }
                Assert.That(b.IsGameOver, Is.True);
                AssertSnapshotsEqual(a.ToSnapshot(), b.ToSnapshot());
            }
        }

        [Test]
        public void Snapshot_SurvivesJsonRoundTrip()
        {
            GameState gs = GameState.NewGame(ulong.MaxValue);
            gs.TryPlace(0, 0, 0);
            GameSnapshot s = gs.ToSnapshot();
            var opts = new JsonSerializerOptions { IncludeFields = true };
            string json = JsonSerializer.Serialize(s, opts);
            GameSnapshot back = JsonSerializer.Deserialize<GameSnapshot>(json, opts);
            AssertSnapshotsEqual(s, back);
            AssertSnapshotsEqual(s, GameState.FromSnapshot(back).ToSnapshot());
        }

        [Test]
        public void FromSnapshot_TreatsEmptyStringAsEmptySlot()
        {
            GameState gs = GameState.NewGame(4UL);
            var m = gs.LegalMoves().GetEnumerator();
            m.MoveNext();
            gs.TryPlace(m.Current.slot, m.Current.anchor.X, m.Current.anchor.Y);
            GameSnapshot s = gs.ToSnapshot();
            int empty = System.Array.IndexOf(s.TrayIds, null);
            Assert.That(empty, Is.GreaterThanOrEqualTo(0));
            s.TrayIds[empty] = "";
            GameState back = GameState.FromSnapshot(s);
            Assert.That(back.Tray[empty].HasValue, Is.False);
            Assert.That(back.ToSnapshot().TrayIds[empty], Is.Null);
        }

        [Test]
        public void Snapshot_IsDetachedFromLiveState()
        {
            GameState gs = GameState.NewGame(3UL);
            GameSnapshot s = gs.ToSnapshot();
            s.Stats.Moves = 999;
            s.Colors[0] = 5;
            Assert.That(gs.Stats.Moves, Is.EqualTo(0));
            Assert.That(gs.Grid.ColorAt(0, 0), Is.EqualTo(0));
        }

        [Test]
        public void FromSnapshot_RejectsInvalidData()
        {
            GameSnapshot Valid() => GameState.NewGame(1UL).ToSnapshot();

            GameSnapshot s = Valid(); s.Version = 2;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = Valid(); s.TrayIds[0] = "NOPE";
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = Valid(); s.TrayIds = new string[3]; s.TrayColors = new byte[3];
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = Valid(); s.Bits = 1UL;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = Valid(); s.RngInc = 2UL;
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            s = Valid(); s.Colors = new byte[10];
            Assert.Throws<ArgumentException>(() => GameState.FromSnapshot(s));
            Assert.Throws<ArgumentNullException>(() => GameState.FromSnapshot(null));
        }
    }
}
