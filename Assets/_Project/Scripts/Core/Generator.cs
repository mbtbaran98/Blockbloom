using System;
using System.Collections.Generic;

namespace Blockbloom.Core
{
    /// <summary>
    /// Tepsi uretici (GDD §5.4). RNG tuketim sirasi sabittir: yuva 0,1,2 parca cekimleri -> onarim cekimleri
    /// (sirayla) -> renkler 0,1,2. Bu sirayi degistirmek eski tohumlarin oyunlarini degistirir (karar kaydi gerekir).
    /// Ic tamponlar onceden ayrilir; cagri basina tek ayirma donen 3'luk dizidir.
    /// </summary>
    public sealed class Generator
    {
        private readonly PieceDef[] _catalog;
        private readonly int[] _idKey;      // ayni kimlige sahip parcalar ayni anahtari tasir
        private readonly IRandom _rng;
        private readonly bool[] _fits;
        private readonly int[] _weights;
        private readonly int[] _slot = new int[CoreConfig.TraySlotCount];

        public Generator(IReadOnlyList<PieceDef> catalog, IRandom rng)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (catalog.Count == 0) throw new ArgumentException("Katalog bos olamaz.", nameof(catalog));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _catalog = new PieceDef[catalog.Count];
            _idKey = new int[catalog.Count];
            for (int i = 0; i < _catalog.Length; i++)
            {
                _catalog[i] = catalog[i] ?? throw new ArgumentException("Katalogda null parca.", nameof(catalog));
                _idKey[i] = i;
                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(_catalog[j].Id, _catalog[i].Id, StringComparison.Ordinal))
                    {
                        _idKey[i] = _idKey[j];
                        break;
                    }
                }
            }
            _fits = new bool[_catalog.Length];
            _weights = new int[_catalog.Length];
        }

        public TrayPiece[] NextTray(Grid g, DifficultyMode mode)
        {
            return NextTray(g, Difficulty.Profile(mode));
        }

        /// <summary>Dogrudan profil ile uretim (simulasyon/karsilastirma kosulari icin).</summary>
        public TrayPiece[] NextTray(Grid g, WeightProfile prof)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));

            for (int i = 0; i < _catalog.Length; i++) _fits[i] = g.FitsAnywhere(_catalog[i]);

            // 1) Uc yuvayi sirayla cek; mevcutlar = 0..s-1
            for (int s = 0; s < CoreConfig.TraySlotCount; s++)
            {
                _slot[s] = Draw(prof, s, -1, false);
            }

            // 2) Garanti onarimi
            int fitting = 0;
            for (int s = 0; s < CoreConfig.TraySlotCount; s++) if (_fits[_slot[s]]) fitting++;
            while (fitting < prof.MinFitting)
            {
                int j = -1;
                for (int s = CoreConfig.TraySlotCount - 1; s >= 0; s--)
                {
                    if (!_fits[_slot[s]]) { j = s; break; }
                }
                if (j < 0) break; // tum yuvalar sigiyor
                _slot[j] = Draw(prof, CoreConfig.TraySlotCount, j, true);
                fitting++;
            }

            // 3) Renkler (yalniz gorsel)
            var tray = new TrayPiece[CoreConfig.TraySlotCount];
            for (int s = 0; s < CoreConfig.TraySlotCount; s++)
            {
                byte color = (byte)(1 + _rng.NextInt(CoreConfig.ColorCount));
                tray[s] = new TrayPiece(_catalog[_slot[s]], color);
            }
            return tray;
        }

        /// <summary>
        /// AGIRLIKLI_CEK. Mevcutlar: _slot[0..existingCount-1] icinde excludeSlot haric olanlar.
        /// requireFit: yalniz tahtaya sigan parcalar aday.
        /// </summary>
        private int Draw(WeightProfile prof, int existingCount, int excludeSlot, bool requireFit)
        {
            int largeCount = 0;
            for (int k = 0; k < existingCount; k++)
            {
                if (k == excludeSlot) continue;
                if (_catalog[_slot[k]].Size == SizeClass.Large) largeCount++;
            }

            int total = 0;
            for (int i = 0; i < _catalog.Length; i++)
            {
                int w = 0;
                if ((!requireFit || _fits[i]) && Eligible(i, prof, existingCount, excludeSlot, largeCount))
                {
                    PieceDef p = _catalog[i];
                    w = p.BaseWeight * prof.ClassMul(p.Size) * (_fits[i] ? prof.FitMul : 1);
                }
                _weights[i] = w;
                total += w;
            }
            if (total <= 0) throw new InvalidOperationException("Uretimde aday parca kalmadi (katalog/profil gecersiz).");

            int r = _rng.NextInt(total);
            for (int i = 0; i < _catalog.Length; i++)
            {
                r -= _weights[i];
                if (r < 0) return i;
            }
            throw new InvalidOperationException("Agirlikli cekim basarisiz."); // ulasilamaz
        }

        /// <summary>UYGUN: ayni kimlik mevcutlarda en fazla 1 kez; B ise mevcut B sayisi &lt; MaxLarge.</summary>
        private bool Eligible(int i, WeightProfile prof, int existingCount, int excludeSlot, int largeCount)
        {
            int same = 0;
            int key = _idKey[i];
            for (int k = 0; k < existingCount; k++)
            {
                if (k == excludeSlot) continue;
                if (_idKey[_slot[k]] == key) same++;
            }
            if (same >= CoreConfig.MaxSamePiecePerTray) return false;
            if (_catalog[i].Size == SizeClass.Large && largeCount >= prof.MaxLarge) return false;
            return true;
        }
    }
}
