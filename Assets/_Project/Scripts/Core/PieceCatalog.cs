using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Blockbloom.Core
{
    /// <summary>
    /// GDD §4 parca katalogu: 37 parca, taban agirlik toplami 124. Sira = tablo sirasi (uretici bu sirayla tarar).
    /// Kimlikler kalicidir; sirayi veya agirliklari degistirmek karar kaydi gerektirir.
    /// </summary>
    public static class PieceCatalog
    {
        private static readonly PieceDef[] Defs = Build();
        private static readonly Dictionary<string, PieceDef> ById = BuildIndex(Defs);

        public static IReadOnlyList<PieceDef> All { get; } = new ReadOnlyCollection<PieceDef>(Defs);

        public static int TotalBaseWeight { get; } = SumWeights(Defs);

        /// <summary>Kimlikle parca. Bilinmeyen kimlik -> ArgumentException.</summary>
        public static PieceDef Get(string id)
        {
            PieceDef def;
            if (id == null || !ById.TryGetValue(id, out def))
                throw new ArgumentException("Bilinmeyen parca kimligi: " + (id ?? "null"), nameof(id));
            return def;
        }

        /// <summary>Kimlikle parca; bulunamazsa false.</summary>
        public static bool TryGet(string id, out PieceDef def)
        {
            def = null;
            return id != null && ById.TryGetValue(id, out def);
        }

        private static Cell C(int x, int y) => new Cell(x, y);

        private static PieceDef[] Build()
        {
            return new[]
            {
                new PieceDef("M1", 4, C(0,0)),
                new PieceDef("I2H", 6, C(0,0), C(1,0)),
                new PieceDef("I2V", 6, C(0,0), C(0,1)),
                new PieceDef("I3H", 6, C(0,0), C(1,0), C(2,0)),
                new PieceDef("I3V", 6, C(0,0), C(0,1), C(0,2)),
                new PieceDef("V3A", 4, C(0,0), C(1,0), C(0,1)),
                new PieceDef("V3B", 4, C(0,0), C(1,0), C(1,1)),
                new PieceDef("V3C", 4, C(0,0), C(0,1), C(1,1)),
                new PieceDef("V3D", 4, C(1,0), C(0,1), C(1,1)),
                new PieceDef("I4H", 5, C(0,0), C(1,0), C(2,0), C(3,0)),
                new PieceDef("I4V", 5, C(0,0), C(0,1), C(0,2), C(0,3)),
                new PieceDef("O4", 8, C(0,0), C(1,0), C(0,1), C(1,1)),
                new PieceDef("T4U", 3, C(1,0), C(0,1), C(1,1), C(2,1)),
                new PieceDef("T4D", 3, C(0,0), C(1,0), C(2,0), C(1,1)),
                new PieceDef("T4L", 3, C(1,0), C(0,1), C(1,1), C(1,2)),
                new PieceDef("T4R", 3, C(0,0), C(0,1), C(1,1), C(0,2)),
                new PieceDef("S4H", 2, C(1,0), C(2,0), C(0,1), C(1,1)),
                new PieceDef("S4V", 2, C(0,0), C(0,1), C(1,1), C(1,2)),
                new PieceDef("Z4H", 2, C(0,0), C(1,0), C(1,1), C(2,1)),
                new PieceDef("Z4V", 2, C(1,0), C(0,1), C(1,1), C(0,2)),
                new PieceDef("L4A", 2, C(0,0), C(0,1), C(0,2), C(1,2)),
                new PieceDef("L4B", 2, C(0,0), C(1,0), C(2,0), C(0,1)),
                new PieceDef("L4C", 2, C(0,0), C(1,0), C(1,1), C(1,2)),
                new PieceDef("L4D", 2, C(2,0), C(0,1), C(1,1), C(2,1)),
                new PieceDef("J4A", 2, C(1,0), C(1,1), C(0,2), C(1,2)),
                new PieceDef("J4B", 2, C(0,0), C(0,1), C(1,1), C(2,1)),
                new PieceDef("J4C", 2, C(0,0), C(1,0), C(0,1), C(0,2)),
                new PieceDef("J4D", 2, C(0,0), C(1,0), C(2,0), C(2,1)),
                new PieceDef("I5H", 4, C(0,0), C(1,0), C(2,0), C(3,0), C(4,0)),
                new PieceDef("I5V", 4, C(0,0), C(0,1), C(0,2), C(0,3), C(0,4)),
                new PieceDef("V5A", 2, C(0,0), C(1,0), C(2,0), C(0,1), C(0,2)),
                new PieceDef("V5B", 2, C(0,0), C(1,0), C(2,0), C(2,1), C(2,2)),
                new PieceDef("V5C", 2, C(0,0), C(0,1), C(0,2), C(1,2), C(2,2)),
                new PieceDef("V5D", 2, C(2,0), C(2,1), C(0,2), C(1,2), C(2,2)),
                new PieceDef("R6H", 3, C(0,0), C(1,0), C(2,0), C(0,1), C(1,1), C(2,1)),
                new PieceDef("R6V", 3, C(0,0), C(1,0), C(0,1), C(1,1), C(0,2), C(1,2)),
                new PieceDef("O9", 4, C(0,0), C(1,0), C(2,0), C(0,1), C(1,1), C(2,1), C(0,2), C(1,2), C(2,2)),
            };
        }

        private static Dictionary<string, PieceDef> BuildIndex(PieceDef[] defs)
        {
            var d = new Dictionary<string, PieceDef>(defs.Length, StringComparer.Ordinal);
            for (int i = 0; i < defs.Length; i++) d.Add(defs[i].Id, defs[i]);
            return d;
        }

        private static int SumWeights(PieceDef[] defs)
        {
            int s = 0;
            for (int i = 0; i < defs.Length; i++) s += defs[i].BaseWeight;
            return s;
        }
    }
}
