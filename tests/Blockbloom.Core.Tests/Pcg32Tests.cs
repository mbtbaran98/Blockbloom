using System;
using Blockbloom.Core;
using NUnit.Framework;

namespace Blockbloom.Core.Tests
{
    public class Pcg32Tests
    {
        [Test]
        public void ReferenceDemoOutput_Seed42_Stream54()
        {
            var rng = new Pcg32(42UL, 54UL);
            uint[] expected = { 0xa15c02b7u, 0x7b47f409u, 0xba1d3330u, 0x83d2f293u, 0xbfa4784bu, 0xcbed606eu };
            for (int i = 0; i < expected.Length; i++)
                Assert.That(rng.NextUInt(), Is.EqualTo(expected[i]), "deger " + i);
        }

        [Test]
        public void DefaultStream_IsGddConstant()
        {
            var a = new Pcg32(123UL);
            var b = new Pcg32(123UL, 0xDA3E39CB94B95BDBUL);
            for (int i = 0; i < 100; i++) Assert.That(a.NextUInt(), Is.EqualTo(b.NextUInt()));
        }

        [Test]
        public void GetState_FromState_ContinuesIdentically()
        {
            var a = new Pcg32(99UL);
            for (int i = 0; i < 17; i++) a.NextUInt();
            var st = a.GetState();
            var b = Pcg32.FromState(st.State, st.Inc);
            for (int i = 0; i < 1000; i++) Assert.That(b.NextUInt(), Is.EqualTo(a.NextUInt()));
            Assert.Throws<ArgumentException>(() => Pcg32.FromState(1, 2));
        }

        [Test]
        public void NextInt_RejectsBelowThreshold()
        {
            // n = 3: esik = (2^32 - 3) mod 3 = 1 -> r = 0 reddedilir, sonraki r = 5 -> 5 mod 3 = 2
            var raw = new RawScriptedRandom(0u, 5u);
            Assert.That(raw.NextInt(3), Is.EqualTo(2));
            Assert.That(raw.Calls, Is.EqualTo(2));

            // n = 6: esik = 4294967290 mod 6 = 4 -> 0..3 reddedilir
            raw = new RawScriptedRandom(3u, 4u);
            Assert.That(raw.NextInt(6), Is.EqualTo(4));
            Assert.That(raw.Calls, Is.EqualTo(2));

            // n = 2^k: esik 0, hic red yok
            raw = new RawScriptedRandom(0u);
            Assert.That(raw.NextInt(8), Is.EqualTo(0));
            Assert.That(raw.Calls, Is.EqualTo(1));

            // buyuk n: esik = 2^32 mod n
            int big = int.MaxValue; // 2^31-1 -> esik = (2^32 - n) mod n = 2
            raw = new RawScriptedRandom(1u, 2u);
            Assert.That(raw.NextInt(big), Is.EqualTo(2));
            Assert.That(raw.Calls, Is.EqualTo(2));
        }

        [Test]
        public void NextInt_InRange_AndRoughlyUniform()
        {
            var rng = new Pcg32(2024UL);
            var counts = new int[6];
            const int N = 600000;
            for (int i = 0; i < N; i++)
            {
                int v = rng.NextInt(6);
                Assert.That(v, Is.InRange(0, 5));
                counts[v]++;
            }
            // ki-kare, sd = 5; %99,9 kritik deger ~20.5
            double chi = 0;
            foreach (int c in counts) chi += Math.Pow(c - N / 6.0, 2) / (N / 6.0);
            Assert.That(chi, Is.LessThan(20.5));

            Assert.That(rng.NextInt(1), Is.EqualTo(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(-5));
        }
    }
}
