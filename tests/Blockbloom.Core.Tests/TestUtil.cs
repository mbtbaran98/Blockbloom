using System;
using System.Collections.Generic;
using Blockbloom.Core;
using NUnit.Framework;

namespace Blockbloom.Core.Tests
{
    /// <summary>NextInt donus degerlerini senaryodan veren, cagri argumanlarini kaydeden RNG.</summary>
    internal sealed class ScriptedRandom : IRandom
    {
        private readonly Queue<int> _values;
        public readonly List<int> NextIntArgs = new List<int>();

        public ScriptedRandom(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public uint NextUInt() => throw new InvalidOperationException("Senaryo RNG NextUInt desteklemez.");

        public int NextInt(int maxExclusive)
        {
            NextIntArgs.Add(maxExclusive);
            int v = _values.Count > 0 ? _values.Dequeue() : 0;
            Assert.That(v, Is.GreaterThanOrEqualTo(0).And.LessThan(maxExclusive), "senaryo degeri aralik disi");
            return v;
        }
    }

    /// <summary>Ham NextUInt degerlerini senaryodan veren RNG (sapmasizlik testi).</summary>
    internal sealed class RawScriptedRandom : IRandom
    {
        private readonly Queue<uint> _values;
        public int Calls;

        public RawScriptedRandom(params uint[] values)
        {
            _values = new Queue<uint>(values);
        }

        public uint NextUInt()
        {
            Calls++;
            return _values.Dequeue();
        }

        public int NextInt(int maxExclusive) => RandomUtil.NextIntUnbiased(this, maxExclusive);
    }

    internal static class TestUtil
    {
        public static ulong Bit(int x, int y) => 1UL << (y * 8 + x);

        public static ulong Row(int y) => 0xFFUL << (y * 8);

        public static ulong Col(int x) => 0x0101010101010101UL << x;

        /// <summary>(x+y) cift olan hucreler dolu: 32 dolu, hic bitisik bos hucre yok -> yalniz M1 sigar.</summary>
        public static ulong Checkerboard()
        {
            ulong b = 0;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if (((x + y) & 1) == 0) b |= Bit(x, y);
            return b;
        }

        /// <summary>Elle kurulmus oyun durumu (anlik goruntu uzerinden).</summary>
        public static GameState Build(ulong bits, string[] trayIds, ulong seed = 7, int combo = 0,
            int traysWithoutClear = 0, bool clearedInTray = false, int score = 0)
        {
            var colors = new byte[64];
            for (int i = 0; i < 64; i++) if ((bits & (1UL << i)) != 0) colors[i] = 1;
            var rng = new Pcg32(seed).GetState();
            var s = new GameSnapshot
            {
                Seed = seed,
                Bits = bits,
                Colors = colors,
                Score = score,
                Combo = combo,
                MaxCombo = combo,
                TraysWithoutClear = traysWithoutClear,
                ClearedInCurrentTray = clearedInTray,
                RngState = rng.State,
                RngInc = rng.Inc,
            };
            for (int i = 0; i < 3; i++)
            {
                s.TrayIds[i] = trayIds[i];
                s.TrayColors[i] = trayIds[i] == null ? (byte)0 : (byte)(i + 1);
            }
            s.Stats.MaxCombo = combo;
            s.Stats.TraysDealt = 1;
            return GameState.FromSnapshot(s);
        }

        public static void AssertSnapshotsEqual(GameSnapshot a, GameSnapshot b)
        {
            Assert.That(b.Version, Is.EqualTo(a.Version), "Version");
            Assert.That(b.Seed, Is.EqualTo(a.Seed), "Seed");
            Assert.That(b.Bits, Is.EqualTo(a.Bits), "Bits");
            Assert.That(b.Colors, Is.EqualTo(a.Colors), "Colors");
            Assert.That(b.TrayIds, Is.EqualTo(a.TrayIds), "TrayIds");
            Assert.That(b.TrayColors, Is.EqualTo(a.TrayColors), "TrayColors");
            Assert.That(b.Score, Is.EqualTo(a.Score), "Score");
            Assert.That(b.Combo, Is.EqualTo(a.Combo), "Combo");
            Assert.That(b.MaxCombo, Is.EqualTo(a.MaxCombo), "MaxCombo");
            Assert.That(b.TraysWithoutClear, Is.EqualTo(a.TraysWithoutClear), "T");
            Assert.That(b.ClearedInCurrentTray, Is.EqualTo(a.ClearedInCurrentTray), "ClearedInCurrentTray");
            Assert.That(b.RngState, Is.EqualTo(a.RngState), "RngState");
            Assert.That(b.RngInc, Is.EqualTo(a.RngInc), "RngInc");
            Assert.That(b.Stats.Moves, Is.EqualTo(a.Stats.Moves), "Stats.Moves");
            Assert.That(b.Stats.LinesCleared, Is.EqualTo(a.Stats.LinesCleared), "Stats.LinesCleared");
            Assert.That(b.Stats.MaxCombo, Is.EqualTo(a.Stats.MaxCombo), "Stats.MaxCombo");
            Assert.That(b.Stats.PerfectClears, Is.EqualTo(a.Stats.PerfectClears), "Stats.PerfectClears");
            Assert.That(b.Stats.TraysDealt, Is.EqualTo(a.Stats.TraysDealt), "Stats.TraysDealt");
        }

        /// <summary>Rastgele bot (GDD §10): LegalMoves icinden tekduze secim, ayri Pcg32(seed + 1_000_000).</summary>
        public static (int slot, Cell anchor) PickRandomMove(GameState gs, Pcg32 botRng, List<(int slot, Cell anchor)> buffer)
        {
            int n = gs.LegalMoves(buffer);
            Assert.That(n, Is.GreaterThan(0), "oyun bitmemisken yasal hamle olmali");
            return buffer[botRng.NextInt(n)];
        }
    }
}
