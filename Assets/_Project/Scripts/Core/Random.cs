using System;

namespace Blockbloom.Core
{
    /// <summary>Core'daki tum rastgelelik bu arayuzden gelir (GDD §5.1, §9).</summary>
    public interface IRandom
    {
        uint NextUInt();

        /// <summary>[0, maxExclusive) sapmasiz. maxExclusive &lt;= 0 -> ArgumentOutOfRangeException.</summary>
        int NextInt(int maxExclusive);
    }

    /// <summary>Sapmasiz sinirli tamsayi (GDD §5.1): esik = (2^32 - n) mod n; r &gt;= esik olana kadar tekrar.</summary>
    public static class RandomUtil
    {
        public static int NextIntUnbiased(IRandom rng, int maxExclusive)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive > 0 olmali.");
            uint n = (uint)maxExclusive;
            uint threshold = unchecked(0u - n) % n;
            while (true)
            {
                uint r = rng.NextUInt();
                if (r >= threshold) return (int)(r % n);
            }
        }
    }

    /// <summary>
    /// PCG32 (XSH-RR, 64-bit durum, 32-bit cikti), PCG referans uygulamasi (pcg_basic) ile birebir.
    /// Platformlar arasi bit duzeyinde ayni sonuc verir.
    /// </summary>
    public sealed class Pcg32 : IRandom
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong _state;
        private ulong _inc;

        /// <summary>pcg32_srandom(seed, stream) ile ayni baslatma.</summary>
        public Pcg32(ulong seed, ulong stream = CoreConfig.DefaultRngStream)
        {
            _state = 0UL;
            _inc = (stream << 1) | 1UL;
            NextUInt();
            unchecked { _state += seed; }
            NextUInt();
        }

        private Pcg32(ulong state, ulong inc, bool raw)
        {
            _state = state;
            _inc = inc;
        }

        public uint NextUInt()
        {
            ulong old = _state;
            unchecked
            {
                _state = old * Multiplier + _inc;
                uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
                int rot = (int)(old >> 59);
                return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
            }
        }

        public int NextInt(int maxExclusive) => RandomUtil.NextIntUnbiased(this, maxExclusive);

        public (ulong State, ulong Inc) GetState() => (_state, _inc);

        /// <summary>Kayitli durumdan devam. inc tek sayi olmali (referansta her zaman tek).</summary>
        public static Pcg32 FromState(ulong state, ulong inc)
        {
            if ((inc & 1UL) == 0) throw new ArgumentException("PCG32 inc tek sayi olmali.", nameof(inc));
            return new Pcg32(state, inc, true);
        }
    }
}
