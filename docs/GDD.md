# Blockbloom — Oyun Tasarım Belgesi (GDD)

- **Sürüm:** v0.1 — 2026-10-12
- **Kapsam:** M0 (çekirdek oyun, klasik mod). M1–M4 yalnızca §12'de listelenir.
- **Sahibi:** game-designer. Değişiklikler `docs/decisions.md` ile eşlenir.
- **İlke atıfları:** "İ1…İ8" = `CLAUDE.md` → *Tasarım ilkeleri* maddeleri.
- Tüm sayılar başlangıç değeridir; §10'daki simülasyonla ayarlanır. Değiştirilebilir sabitler `KALIN_BÜYÜK` adla verilir; programcı bunları tek bir yapılandırma sınıfında toplar.

---

## 1. Özet

8×8 ızgara. Oyuncuya aynı anda 3 parça verilir. Parçalar sürükle-bırak ile yerleştirilir; dolan satır ve sütunlar patlar. Süre yok, ceza yok, döndürme yok. Elindeki kalan parçaların hiçbiri hiçbir yere sığmadığında oyun biter. Amaç: kişisel rekor. (İ1)

Oyuncu tipleri: **rahatlamak isteyen** (süre yok, görünmez kolaylaştırma — İ5), **zihnini çalıştıran** (3 parçalık planlama, kombo — İ1), **meydan okuma arayan** (kombo çarpanı, rekor; açık tahtada büyük parça ağırlığı — §5.3).

---

## 2. Terimler

| Terim | Tanım |
|---|---|
| Hücre | Izgaradaki bir kare, `(x, y)`; `x` 0–7 sağa, `y` 0–7 aşağı. İndeks = `y*8 + x`. |
| Parça tanımı (`PieceDef`) | Sabit şekil; §4 kataloğundaki bir satır. |
| Tepsi parçası | Tepsideki bir yuvada duran parça örneği (tanım + renk). |
| Çapa (anchor) | Parçanın sınır kutusunun sol-üst köşesinin ızgaradaki konumu `(ax, ay)`. Parça hücresi `(cx, cy)` → ızgara `(ax+cx, ay+cy)`. |
| Hamle | Bir parçanın tahtaya başarıyla yerleştirilmesi. |
| Çizgi | Tam dolu bir satır veya sütun. |
| Kombo `K` | Art arda patlatan hamle sayacı (§6). |

---

## 3. Kurallar

### 3.1 Izgara
- 8×8 = 64 hücre. Her hücre boş ya da dolu (dolu hücre bir renk kimliği 1–6 taşır; renk yalnız görseldir, kurala etkisi yok).
- Oyun boş tahtayla başlar.

### 3.2 Tepsi
- 3 yuva (0, 1, 2). Oyuncu parçaları **istediği sırayla** yerleştirir. (İ1: planlama derinliği)
- Yerleştirilen parçanın yuvası **boş kalır**; diğer parçalar kaymaz (konum hafızası bozulmaz).
- **Yenileme:** yalnızca üç yuva da boşaldığında, o hamlenin patlatma işlemi bittikten **sonra** 3 yeni parça üretilir (§5). Takılınca yenileme yoktur.

### 3.3 Döndürme yok
- Parçalar döndürülemez ve aynalanamaz. Her yön ayrı bir parça tanımıdır (§4).
- Gerekçe: İ1 — tek hareket (sürükle) tek karar; döndürme düğmesi/hareketi girdi karmaşıklığı ve yanlış dokunma getirir. Derinlik, "bu üç şekli hangi sırayla, nereye" sorusundan gelir. Yön çeşitliliği üreticinin ağırlıklarıyla kontrol edilir (§5), böylece zorluk ayarı (İ5) yön düzeyinde yapılabilir.

### 3.4 Yerleştirme geçerliliği
`CanPlace(parça, ax, ay)` doğrudur ⇔ parçanın **her** hücresi için:
1. `0 ≤ ax+cx ≤ 7` ve `0 ≤ ay+cy ≤ 7` (tamamen tahta içinde), ve
2. `(ax+cx, ay+cy)` boş.

Komşuluk şartı yoktur; tahtanın herhangi bir yerine konabilir.

### 3.5 Hamle sırası (tek hamlede, bu sırayla)
1. Parça hücreleri doldurulur. Puan: hücre başı +1 (§6).
2. **Yeni** tahtada tüm satırlar ve tüm sütunlar **aynı anda** denetlenir. Dolu satır kümesi `R`, dolu sütun kümesi `C`.
3. `R ∪ C` içindeki tüm hücreler tek seferde boşaltılır. Satır-sütun kesişimindeki hücre **bir kez** boşaltılır ve bir kez sayılır: `temizlenen_hücre = 8·|R| + 8·|C| − |R|·|C|`.
4. `çizgi_sayısı = |R| + |C|` (kesişim çizgi sayısını etkilemez). Kombo ve patlatma puanı uygulanır (§6).
5. Tahta tamamen boşsa tam temizlik bonusu (§6.3).
6. Tepsi boşsa yenilenir (§5).
7. Oyun sonu denetimi (§3.6).

### 3.6 Oyun sonu
- Her hamleden sonra (yenileme dahil) denetlenir: tepside kalan parçaların **hiçbiri** tahtada **hiçbir** çapaya `CanPlace` ile sığmıyorsa oyun biter.
- Parçalar tek tek denetlenir; "üçü birlikte sığar mı" denetlenmez.
- Yeni tepside en az bir parça her zaman sığar (§5.4 garantisi), bu yüzden oyun hiçbir zaman yenilemenin hemen ardından bitmez.

---

## 4. Parça kataloğu

37 parça tanımı. Koordinatlar `(x, y)`, sınır kutusu `(0,0)`'dan başlar (min x = 0, min y = 0). Sınıf: **K** küçük (≤3 hücre), **O** orta (4), **B** büyük (≥5). Ağırlıklar tamsayıdır; toplam **124**. Tablo sırası = katalog sırası (üretici bu sırayla tarar; deterministik).

| # | Kimlik | Hücreler | Hücre | G×Y | Sınıf | Ağırlık |
|---|---|---|---|---|---|---|
| 1 | `M1` | (0,0) | 1 | 1×1 | K | 4 |
| 2 | `I2H` | (0,0),(1,0) | 2 | 2×1 | K | 6 |
| 3 | `I2V` | (0,0),(0,1) | 2 | 1×2 | K | 6 |
| 4 | `I3H` | (0,0),(1,0),(2,0) | 3 | 3×1 | K | 6 |
| 5 | `I3V` | (0,0),(0,1),(0,2) | 3 | 1×3 | K | 6 |
| 6 | `V3A` | (0,0),(1,0),(0,1) | 3 | 2×2 | K | 4 |
| 7 | `V3B` | (0,0),(1,0),(1,1) | 3 | 2×2 | K | 4 |
| 8 | `V3C` | (0,0),(0,1),(1,1) | 3 | 2×2 | K | 4 |
| 9 | `V3D` | (1,0),(0,1),(1,1) | 3 | 2×2 | K | 4 |
| 10 | `I4H` | (0,0),(1,0),(2,0),(3,0) | 4 | 4×1 | O | 5 |
| 11 | `I4V` | (0,0),(0,1),(0,2),(0,3) | 4 | 1×4 | O | 5 |
| 12 | `O4` | (0,0),(1,0),(0,1),(1,1) | 4 | 2×2 | O | 8 |
| 13 | `T4U` | (1,0),(0,1),(1,1),(2,1) | 4 | 3×2 | O | 3 |
| 14 | `T4D` | (0,0),(1,0),(2,0),(1,1) | 4 | 3×2 | O | 3 |
| 15 | `T4L` | (1,0),(0,1),(1,1),(1,2) | 4 | 2×3 | O | 3 |
| 16 | `T4R` | (0,0),(0,1),(1,1),(0,2) | 4 | 2×3 | O | 3 |
| 17 | `S4H` | (1,0),(2,0),(0,1),(1,1) | 4 | 3×2 | O | 2 |
| 18 | `S4V` | (0,0),(0,1),(1,1),(1,2) | 4 | 2×3 | O | 2 |
| 19 | `Z4H` | (0,0),(1,0),(1,1),(2,1) | 4 | 3×2 | O | 2 |
| 20 | `Z4V` | (1,0),(0,1),(1,1),(0,2) | 4 | 2×3 | O | 2 |
| 21 | `L4A` | (0,0),(0,1),(0,2),(1,2) | 4 | 2×3 | O | 2 |
| 22 | `L4B` | (0,0),(1,0),(2,0),(0,1) | 4 | 3×2 | O | 2 |
| 23 | `L4C` | (0,0),(1,0),(1,1),(1,2) | 4 | 2×3 | O | 2 |
| 24 | `L4D` | (2,0),(0,1),(1,1),(2,1) | 4 | 3×2 | O | 2 |
| 25 | `J4A` | (1,0),(1,1),(0,2),(1,2) | 4 | 2×3 | O | 2 |
| 26 | `J4B` | (0,0),(0,1),(1,1),(2,1) | 4 | 3×2 | O | 2 |
| 27 | `J4C` | (0,0),(1,0),(0,1),(0,2) | 4 | 2×3 | O | 2 |
| 28 | `J4D` | (0,0),(1,0),(2,0),(2,1) | 4 | 3×2 | O | 2 |
| 29 | `I5H` | (0,0),(1,0),(2,0),(3,0),(4,0) | 5 | 5×1 | B | 4 |
| 30 | `I5V` | (0,0),(0,1),(0,2),(0,3),(0,4) | 5 | 1×5 | B | 4 |
| 31 | `V5A` | (0,0),(1,0),(2,0),(0,1),(0,2) | 5 | 3×3 | B | 2 |
| 32 | `V5B` | (0,0),(1,0),(2,0),(2,1),(2,2) | 5 | 3×3 | B | 2 |
| 33 | `V5C` | (0,0),(0,1),(0,2),(1,2),(2,2) | 5 | 3×3 | B | 2 |
| 34 | `V5D` | (2,0),(2,1),(0,2),(1,2),(2,2) | 5 | 3×3 | B | 2 |
| 35 | `R6H` | (0,0),(1,0),(2,0),(0,1),(1,1),(2,1) | 6 | 3×2 | B | 3 |
| 36 | `R6V` | (0,0),(1,0),(0,1),(1,1),(0,2),(1,2) | 6 | 2×3 | B | 3 |
| 37 | `O9` | 3×3'ün 9 hücresinin tümü: x∈{0,1,2}, y∈{0,1,2} | 9 | 3×3 | B | 4 |

**Sınıf payları (temel ağırlıkla):** K 44/124 = %35,5 · O 54/124 = %43,5 · B 26/124 = %21,0. Parça başı beklenen hücre = 480/124 ≈ **3,87**; tepsi başı ≈ 11,6 hücre.

**Tasarım notları:**
- Bağlantısız/çapraz parça yok: okunabilirlik (İ1, İ8 düşük donanımda da net görünüm).
- Düz çizgiler (I2–I5) çizgi tamamlamanın ana aracıdır; ağırlıkları yüksek tutuldu (patlatma hissi — İ2).
- S/Z ve L/J yön başına düşük ağırlık (2): toplamda sık gelirler ama tek bir yön nadirdir; "zor" parçalar.
- `O9` ve `R6`: tahtayı hızla dolduran baskı parçaları; açık tahtada (§5.3 AÇIK modu) oranları artar.
- Katalog verisi kodda sabit tablo olarak tutulur; kimlikler kalıcıdır (kayıt/anlık görüntü bunlara referans verir).

---

## 5. Parça üretimi ve görünmez zorluk ayarı

### 5.1 Rastgele sayı üreteci (RNG)
- **PCG32** (XSH-RR, 64-bit durum, 32-bit çıktı), PCG referans uygulamasına birebir uygun. `System.Random` / `UnityEngine.Random` **kullanılmaz** (platformlar arası ve sürümler arası aynılık garantisi yok).
- Tohum: `Pcg32(ulong seed, ulong stream = 0xDA3E39CB94B95BDB)`; referanstaki `pcg32_srandom` ile aynı başlatma.
- `NextUInt()` → 32-bit. `NextInt(n)` → `[0, n)` sapmasız: `eşik = (2^32 − n) mod n`; `r = NextUInt()`; `r ≥ eşik` olana kadar tekrar; `r mod n` döndür.
- RNG durumu (`state`, `inc`) anlık görüntüye yazılabilir; aynı tohum + aynı hamleler = bit düzeyinde aynı oyun. (Test, simülasyon, M2 günlük bulmaca)
- Yeni oyun tohumu Unity katmanında üretilir (ör. zaman damgası); Core yalnızca tohumu alır.

### 5.2 Zorluk girdileri (her tepsi üretiminde hesaplanır)
- `dolu` = tahtadaki dolu hücre sayısı (hamlenin patlatması bittikten sonra).
- `T` = **patlatmasız tepsi serisi**: tamamlanan son tepside (3 hamlenin hiçbiri çizgi patlatmadıysa) `T += 1`, en az biri patlattıysa `T = 0`. Oyun başında `T = 0`.

### 5.3 Zorluk modları (İ5)

Kurallar sırayla denenir, ilk tutan seçilir:

| Mod | Koşul | Çarpan K / O / B (onda bir, tamsayı) | Sığma çarpanı | Asgari sığan parça | Ek kural |
|---|---|---|---|---|---|
| `RAHATLATMA` | `dolu ≥ 32` **veya** `T ≥ 2` | 15 / 10 / 5 | sığan parça ×3, sığmayan ×1 | 2 | tepside en fazla 1 B parça |
| `AÇIK` | `dolu ≤ 15` **ve** `T = 0` | 8 / 10 / 13 | ×1 | 1 | — |
| `NORMAL` | diğer durumlar | 10 / 10 / 10 | ×1 | 1 | — |

- Etkin ağırlık (tamsayı): `w(p) = taban(p) × sınıfÇarpanı(p) × sığmaÇarpanı(p)`. Ölçek farkı önemsizdir (oransal seçim).
- `RAHATLATMA` örnek etkisi: B sınıfının payı %21 → ≈%10'a iner (sığma çarpanı öncesi); yerleşebilir parçalar 3 kat öne çıkar.
- `AÇIK` örnek etkisi: B payı %21 → ≈%27; boş tahtada sıkılmayı önler, meydan okuma arayana baskı sağlar.
- Mod **oyuncuya gösterilmez**, UI'da ipucu yoktur (İ5 "görünmez"). Yalnızca hata ayıklama/simülasyon çıktısında görünür.

### 5.4 Üretim algoritması (sözde kod)

```
TEPSİ_ÜRET(tahta, rng, zorlukDurumu) -> TepsiParçası[3]
  mod   ← ZORLUK_DEĞERLENDİR(tahta.dolu, zorlukDurumu.T)        // §5.3
  prof  ← PROFİL(mod)                                            // çarpanlar, asgariSığan, maxB
  sığar[p] ← tahta.HerhangiYereSığar(p)  her p ∈ KATALOG için    // 37 parça, önceden bir kez

  // 1) Üç yuvayı sırayla çek
  için i = 0..2:
      tepsi[i] ← AĞIRLIKLI_ÇEK(rng, KATALOG, p ->
                    UYGUN(p, tepsi[0..i-1], prof) ? w(p, prof, sığar) : 0)

  // 2) Garanti onarımı
  sığanSay ← |{ i : sığar[tepsi[i]] }|
  döngü sığanSay < prof.asgariSığan:
      j ← sığmayan yuvalar içinde EN BÜYÜK indeks
      tepsi[j] ← AĞIRLIKLI_ÇEK(rng, KATALOG, p ->
                    (sığar[p] ve UYGUN(p, tepsi[diğer yuvalar], prof)) ? w(p, prof, sığar) : 0)
      sığanSay ← sığanSay + 1

  // 3) Renk (yalnız görsel)
  için i = 0..2:  tepsi[i].renk ← 1 + rng.NextInt(6)

  döndür tepsi

UYGUN(p, mevcutlar, prof):
  mevcutlar içinde p.Kimlik en fazla 1 kez geçiyorsa        // aynı parça bir tepside en fazla 2 kez
  ve (p.Sınıf ≠ B veya mevcutlardaki B sayısı < prof.maxB)  // maxB: RAHATLATMA=1, diğerleri=3
  → doğru

AĞIRLIKLI_ÇEK(rng, liste, w):
  toplam ← Σ w(p)
  r ← rng.NextInt(toplam)
  liste sırasıyla (§4 tablo sırası): r ← r − w(p); r < 0 ise p döndür
```

**Garanti kanıtı:** Bir hamlenin patlatması sonrası tahtada en az bir boş hücre vardır (64 hücre dolarsa tüm satırlar dolu olur ve patlar). Dolayısıyla `M1` her zaman sığar; onarım adımında aday kümesi asla boş olmaz. `M1` en fazla 2 kez kuralı asgari 2 şartıyla çelişmez.

**RNG tüketim sırası sabittir:** yuva 0, 1, 2 parça çekimleri → onarım çekimleri (sırayla) → renkler 0, 1, 2. Bu sıra değişirse eski tohumların oyunları değişir; değişiklik karar kaydı gerektirir.

**Performans (İ8):** `HerhangiYereSığar` 37 parça × en fazla 64 çapa; bit tahtası (`ulong`) ile parça maskesi kaydırılıp `(tahta & maske) == 0` denetlenir. Tepsi üretimi hedefi < 0,1 ms.

---

## 6. Puanlama

### 6.1 Formül (CLAUDE.md başlangıç değerleri)
- Yerleştirme: `+1 × parçanın hücre sayısı`.
- Patlatma: `10 × n² × K`, `n` = bu hamlede patlayan çizgi sayısı (satır + sütun).
  - Hızlı tablo (K=1): n=1→10, 2→40, 3→90, 4→160, 5→250, 6→360.
- Hamle puanı = yerleştirme + patlatma + tam temizlik bonusu.

### 6.2 Kombo kararları
| Soru | Karar | İlke |
|---|---|---|
| Başlangıç değeri | `K = 0` | — |
| Patlatan hamle | Önce `K ← K + 1`, sonra puan `10 × n² × K`. **İlk patlatmada K = 1** (çarpan 1). | İ2 kademeli |
| Patlatmayan hamle | `K ← 0` | CLAUDE.md |
| Tepsi yenilenince | **Sıfırlanmaz** (yenileme hamle değildir). Kombo tepsiler arasında sürebilir. | İ1 planlama ödüllenir |
| Tek hamlede çok çizgi | Kombo yalnız +1 artar; çok çizgi `n²` ile ödüllenir. | İ2 |
| Üst sınır | Yok (M0). Simülasyonda p99 izlenir (§10). | — |
| Kesişim hücresi | Puanı etkilemez (puan çizgi sayısına bağlı); yalnız bir kez boşaltılır. | — |

### 6.3 Tam temizlik bonusu
- **Var.** Hamle sonrası tahta tamamen boşsa `+300` (sabit, kombo ile çarpılmaz; `TAM_TEMİZLİK_BONUSU = 300`).
- Gerekçe: İ2 — en nadir ve en tatmin edici an, en üst kademe geri bildirim almalı. Çarpılmaması puan patlamasını sınırlar. Oyun başındaki boş tahta bonus vermez (yalnız patlatma sonucu boşalma sayılır).

### 6.4 Sayısal örnekler
Her örnek bir öncekinin devamıdır; başlangıçta `K = 0`.

| # | Durum | Hesap | Hamle puanı | K sonrası |
|---|---|---|---|---|
| 1 | `L4A` konur, patlatma yok | 4 | **4** | 0 |
| 2 | `I3H` 1 satırı tamamlar | 3 + 10×1²×1 | **13** | 1 |
| 3 | (Tepsi yenilendi — K korunur.) `O4` 1 satır + 1 sütun tamamlar (kesişim 1 hücre; 15 hücre boşalır) | 4 + 10×2²×2 | **84** | 2 |
| 4 | `I2V` alt alta 2 satırı tamamlar; tahtada başka hücre kalmaz | 2 + 10×2²×3 + 300 | **422** | 3 |
| 5 | `M1` konur, patlatma yok | 1 | **1** | 0 |

Toplam: 524.

---

## 7. Oyun sonu ve rekor

### 7.1 Oyun sonu akışı
1. §3.6 koşulu sağlanır → girdi kilitlenir.
2. 600 ms bekleme; sonra dolu hücreler 400 ms içinde griye solar (M1'de efektle zenginleşir).
3. Oyun sonu paneli açılır. **M0'da devam ettirme yok; reklam yok** (İ6: ödül/sonuç ekranında geçiş reklamı hiçbir aşamada yok).

### 7.2 Oyun sonu ekranında
- Skor (büyük), kişisel rekor.
- Skor > önceki rekor ise "Yeni rekor!" etiketi (eşitlik rekor sayılmaz).
- Bu oyunun istatistikleri: hamle sayısı, patlatılan toplam çizgi, en yüksek kombo.
- Düğmeler: **Tekrar oyna** (tek dokunuş → yeni oyun, yeni tohum), **Menü**.

### 7.3 Kalıcı kayıt (cihazda, çevrimdışı — İ1)
- Saklanan: `enYüksekSkor` (int), `oynananOyun` (int), `enYüksekKombo` (int).
- Yazma anları: oyun sonu; ayrıca uygulama arka plana atılırken anlık skor rekoru aşmışsa rekor hemen yazılır (rekor kaybolmaz).
- HUD: üstte anlık skor ve rekor; anlık skor rekoru geçince rekor alanı anlık skoru gösterir.
- Depolama yöntemi Unity katmanının işidir (`IRecordStore` arayüzü, §9). Yarıda kalan oyunun kaydı (anlık görüntü) API'de hazırdır; M0'da UI'a bağlanması **isteğe bağlıdır** (açık soru).

---

## 8. Etkileşim (M0)

Tüm süre/mesafe değerleri yapılandırılabilir sabittir. Birim "hücre" = tahta hücre kenarı.

| Konu | Davranış |
|---|---|
| Tepsi görünümü | Parçalar tahta ölçeğinin **0,55** katında, yuva ortasında. |
| Dokunma alanı | Yuvanın **tamamı** (ekran genişliğinin 1/3'ü × tepsi yüksekliği), yalnız parça pikselleri değil. Küçük parçalar kolay tutulur (niyet desteği, research.md: Pichlmair & Johansen). |
| Kaldırma | Dokunuşta parça 80 ms içinde 1,0 ölçeğe büyür. |
| Parmak ofseti | Parça yatayda parmağa ortalanır; parçanın **alt kenarı parmağın 1,2 hücre yukarısında** durur (`SÜRÜKLEME_OFSETİ = 1.2`). Parmak hedefi örtmez. |
| Çapa hesabı | Sürüklenen parçanın sol-üst hücre merkezinin tahtaya göre konumu `(fx, fy)` (hücre birimi, kesirli). Aday çapalar: `(⌊fx⌋,⌊fy⌋)`, `(⌈fx⌉,⌊fy⌋)`, `(⌊fx⌋,⌈fy⌉)`, `(⌈fx⌉,⌈fy⌉)`. `CanPlace` doğru olanlar arasından `(fx,fy)`'ye en yakını seçilir; eşitlikte önce küçük y, sonra küçük x. Seçilenin uzaklığı > 0,75 hücre ise geçerli çapa yok. |
| Bırakma önizlemesi | Geçerli çapa varsa parça hücreleri tahtada %45 opaklıkla (parça renginde) gösterilir. Geçersizse önizleme yok. |
| Patlayacak çizgi vurgusu | Önizleme konumunda patlayacak satır/sütunların tüm hücreleri vurgulanır (M0: parlaklık artışı; renk/efekt art-ui'nin). Core `PreviewLines` ile verilir. Kombo devam edecekse M1'de ek ipucu. |
| Bırakma — geçerli | Parça hemen yerleşir (≤ 1 kare), §3.5 akışı işler. |
| Bırakma — geçersiz / tahta dışı | Parça 150 ms ease-out ile kendi yuvasına, tepsi ölçeğine döner. Ceza yok (İ1). |
| Sığmayan tepsi parçası | %40 opaklıkla soluk gösterilir; yine de sürüklenebilir (bırakılınca geri döner). Planlamayı kolaylaştırır, cezalandırmaz (İ1). |
| Çoklu dokunuş | Yalnızca ilk temas izlenir; sürükleme sırasında diğer dokunuşlar yok sayılır. |
| Geri al | M0'da yok. |

---

## 9. Core API önerisi (`Blockbloom.Core`, saf C# 9, `UnityEngine` yok)

Yalnızca imzalar ve davranış. Koleksiyonlar dışarıya salt okunur verilir. Hata durumunda (geçersiz yuva vb.) istisna değil, sonuç nesnesi döndürülür.

```
namespace Blockbloom.Core

readonly struct Cell { int X; int Y; }                         // x sağa, y aşağı

enum SizeClass { Small, Medium, Large }                        // K / O / B
enum DifficultyMode { Open, Normal, Relief }                   // AÇIK / NORMAL / RAHATLATMA

sealed class PieceDef
  string Id; IReadOnlyList<Cell> Cells; int Width; int Height;
  int CellCount; int BaseWeight; SizeClass Size
  ulong MaskAt(int ax, int ay)          // çapada bit maskesi; önceden hesaplanır

static class PieceCatalog
  IReadOnlyList<PieceDef> All           // §4 tablo sırası, 37 öğe
  PieceDef Get(string id)               // bilinmeyen kimlik → ArgumentException
  int TotalBaseWeight                   // 124

interface IRandom
  uint NextUInt(); int NextInt(int maxExclusive)
sealed class Pcg32 : IRandom
  Pcg32(ulong seed, ulong stream = 0xDA3E39CB94B95BDB)
  (ulong State, ulong Inc) GetState(); static Pcg32 FromState(ulong state, ulong inc)

sealed class Grid
  const int Size = 8
  ulong Bits                            // dolu hücre bit tahtası, indeks y*8+x
  byte ColorAt(int x, int y)            // 0 boş, 1–6 renk
  int FilledCount
  bool IsEmpty
  bool CanPlace(PieceDef p, int ax, int ay)
  bool FitsAnywhere(PieceDef p)
  IEnumerable<Cell> ValidAnchors(PieceDef p)                    // y sonra x artan sıra
  LinePreview PreviewLines(PieceDef p, int ax, int ay)          // tahtayı değiştirmez
  PlacementResult Place(PieceDef p, int ax, int ay, byte color) // §3.5 adım 1–3; CanPlace yanlışsa InvalidOperationException
  Grid Clone()

readonly struct LinePreview { IReadOnlyList<int> Rows; IReadOnlyList<int> Cols; int LineCount; }

sealed class PlacementResult
  int CellsPlaced; IReadOnlyList<int> ClearedRows; IReadOnlyList<int> ClearedCols
  int LineCount; int CellsCleared                               // kesişim bir kez
  IReadOnlyList<Cell> ClearedCells; bool BoardCleared

readonly struct ScoreBreakdown { int PlacementPoints; int LinePoints; int PerfectClearBonus; int Total; int ComboAfter; }

sealed class ScoreKeeper
  int Score; int Combo; int MaxCombo
  ScoreBreakdown Apply(PlacementResult r)   // §6 kurallarını uygular, durumu günceller

sealed class DifficultyState
  int TraysWithoutClear                     // T
  void OnTrayCompleted(bool anyClearInTray)

readonly struct WeightProfile { int SmallMul; int MediumMul; int LargeMul; int FitMul; int MinFitting; int MaxLarge; }

static class Difficulty
  DifficultyMode Evaluate(int filledCount, DifficultyState s)   // §5.3
  WeightProfile Profile(DifficultyMode m)

readonly struct TrayPiece { PieceDef Def; byte Color; }

sealed class Tray
  const int SlotCount = 3
  TrayPiece? this[int slot]
  int RemainingCount; bool IsEmpty
  TrayPiece Take(int slot)                  // yuvayı boşaltır

sealed class Generator
  Generator(IReadOnlyList<PieceDef> catalog, IRandom rng)
  TrayPiece[] NextTray(Grid g, DifficultyMode mode)             // §5.4; garanti sağlanır

sealed class MoveResult
  bool Success; string? FailReason                              // "EmptySlot", "InvalidPlacement", "GameOver"
  PlacementResult? Placement; ScoreBreakdown Score
  bool TrayRefilled; DifficultyMode? NewTrayMode                // NewTrayMode yalnız hata ayıklama/simülasyon
  bool IsGameOver

sealed class GameStats { int Moves; int LinesCleared; int MaxCombo; int PerfectClears; int TraysDealt; }

sealed class GameState
  static GameState NewGame(ulong seed)
  Grid Grid; Tray Tray; ScoreKeeper Scoring; DifficultyState Difficulty
  GameStats Stats; bool IsGameOver; ulong Seed
  bool CanPlace(int slot, int ax, int ay)
  bool SlotFitsAnywhere(int slot)                               // soluk gösterim için
  MoveResult TryPlace(int slot, int ax, int ay)                 // §3.5'in tamamı
  IEnumerable<(int slot, Cell anchor)> LegalMoves()             // botlar için; yuva, y, x artan sıra
  GameSnapshot ToSnapshot()
  static GameState FromSnapshot(GameSnapshot s)

sealed class GameSnapshot                                       // düz veri, JSON'a uygun
  int Version; ulong Seed; ulong Bits; byte[64] Colors
  string?[3] TrayIds; byte[3] TrayColors
  int Score; int Combo; int MaxCombo; int TraysWithoutClear
  ulong RngState; ulong RngInc; GameStats Stats

interface IRecordStore                                          // Unity katmanı uygular
  int BestScore; int GamesPlayed; int BestCombo
  void Save(int bestScore, int gamesPlayed, int bestCombo)
```

Davranış notları:
- `TryPlace` başarısızsa durum **değişmez** ve RNG tüketilmez.
- Tepsi yenilemesi yalnız `TryPlace` içinde olur; `DifficultyState.OnTrayCompleted` yenilemeden **önce** çağrılır, mod sonra değerlendirilir.
- `NewGame`: boş tahta, `K=0`, `T=0`, ilk tepsi üretilir (mod `AÇIK`).
- Core hiçbir zaman zaman/saat okumaz; tüm rastgelelik `IRandom`'dan gelir.

---

## 10. Denge simülasyonu — kabul kriterleri (M0)

**Kurulum (qa-tester, `tools/` altında dotnet konsol):**
- Tohumlar `1..1000`, her tohum için iki bot: **Rastgele** ve **Açgözlü**. Toplam 2.000 oyun.
- **Rastgele bot:** `LegalMoves()` içinden tekdüze seçim; kendi `Pcg32(seed + 1_000_000)` örneğini kullanır (oyun RNG'sinden ayrı).
- **Açgözlü bot:** her yasal hamlenin anlık `ScoreBreakdown.Total`'ını hesaplar, en büyüğünü seçer. Eşitlikte: hamle sonrası `FilledCount` küçük olan → yuva indeksi küçük → y küçük → x küçük. Tamamen deterministik.
- Güvenlik sınırı: 5.000 hamle; aşan oyun "bitmedi" olarak kaydedilir.
- Karşılaştırma koşusu: aynı tohumlarla zorluk ayarı kapalı (`her zaman NORMAL`, asgari sığan 1) — İ5 etkisini ölçmek için.

**Ölçülecek metrikler** (her biri için ortalama, medyan, p10, p90, p99):
oyun başına hamle sayısı, skor, patlatılan çizgi, en yüksek kombo, tam temizlik sayısı; tepsi başına mod dağılımı; patlatan hamle oranı; çizgi sayısı dağılımı (n=1,2,3,4+); parça kimliği frekansı; tepsi üretim süresi.

**Hedefler.** Kesin kriterler bağlayıcıdır; **tahmini aralıklar ilk tasarım tahminidir**, ilk simülasyondan sonra bu belgede güncellenir.

| # | Metrik | Hedef | Tür |
|---|---|---|---|
| S1 | Yeni tepside hiçbir parçanın sığmadığı tepsi oranı | **= 0** | Kesin |
| S2 | `RAHATLATMA` tepsilerinde sığan parça sayısı < 2 olan oran | **= 0** | Kesin |
| S3 | Determinizm: aynı tohum + bot iki kez → aynı hamle ve skor dizisi; anlık görüntüden devam eden oyun kesintisizle aynı | **%100** | Kesin |
| S4 | Puan muhasebesi: Σ hamle puanı = son skor; kesişimde `CellsCleared = 8n_r + 8n_c − n_r·n_c` | **%100** | Kesin |
| S5 | Rastgele bot — oyun başına ortalama hamle | 20–60 | Tahmin |
| S6 | Açgözlü bot — oyun başına ortalama hamle | 150–800 | Tahmin |
| S7 | Açgözlü / rastgele ortalama skor oranı | ≥ 5 (beceri fark yaratmalı — İ1) | Tahmin |
| S8 | Açgözlü botta ilk 3 tepside (≤ 9 hamle) biten oyun oranı | ≤ %2 | Tahmin |
| S9 | Açgözlü botta "bitmedi" (5.000 hamle) oranı | ≤ %1 (oyun sonsuz olmamalı) | Tahmin |
| S10 | Açgözlü botta tepsi mod payları | RAHATLATMA %10–35, AÇIK %10–40 | Tahmin |
| S11 | Açgözlü botta patlatan hamle oranı | %20–45 | Tahmin |
| S12 | Açgözlü botta oyun başı en yüksek kombo, medyan | ≥ 3 | Tahmin |
| S13 | Zorluk ayarı açık / kapalı, açgözlü ortalama hamle oranı | 1,15–2,0 (etkili ama oyunu bozmayan) | Tahmin |
| S14 | NORMAL mod tepsilerinde parça frekansı ile `taban/124` farkı | her parça için ≤ 1,5 yüzde puanı (onarım hariç sayım) | Kesin |
| S15 | Tepsi üretim süresi (geliştirme makinesi) | ortalama < 0,1 ms; 2.000 oyunun tamamı < 60 s | Kesin |

**Ayar yönü (hedef dışı çıkarsa):** S6 düşükse → B ağırlıklarını ya da `RAHATLATMA` eşiğini (`dolu ≥ 32` → 28) düşür; S6/S9 yüksekse → `RAHATLATMA` sığma çarpanını 3 → 2 yap, `AÇIK` B çarpanını 13 → 15 yap. Her ayar `decisions.md`'ye yazılır.

---

## 11. Kararlar ve dayandığı ilkeler

| Karar | Bölüm | İlke |
|---|---|---|
| Süre, ceza, geri alma yok; 3 parça serbest sırayla | §3 | İ1 |
| Döndürme yok; 37 yön-tanımlı parça | §3.3, §4 | İ1 |
| Yenileme yalnız 3 parça bitince; yuvalar kaymaz | §3.2 | İ1 |
| Satır+sütun aynı anda; kesişim bir kez | §3.5 | İ2 (çoklu çizgi ödülü net) |
| Kombo: ilk patlatma K=1, patlatmayan hamlede 0, tepsi geçişinde korunur | §6.2 | İ1, İ2 |
| Tam temizlik +300, çarpansız | §6.3 | İ2 |
| Her tepside ≥1 sığan parça; RAHATLATMA'da ≥2 | §5 | İ5 |
| 3 modlu görünmez zorluk; AÇIK mod meydan okuma için | §5.3 | İ5, oyuncu tipleri |
| PCG32 + tamsayı ağırlık → deterministik | §5.1 | Test edilebilir çekirdek (CLAUDE.md teknik yığın) |
| Bit tahtası, < 0,1 ms üretim | §5.4, §9 | İ8 |
| Sığmayan parça soluk; geniş dokunma alanı; esnek çapa | §8 | İ1 |
| Oyun sonunda reklam/devam yok | §7 | İ6 |

---

## 12. Sonraki aşamalar (kapsam dışı, yalnız başlıklar)

- **M1 – His:** kademeli patlatma efektleri (1 çizgi < çok çizgi < kombo), ses katmanları, görüntüyle senkron kısa titreşim, övgü metinleri, kombo göstergesi, oyun sonu animasyonu (İ2, İ3).
- **M2 – İçerik:** macera modu (çini deseni açan bölümler, haftalık içerik), tarih tohumlu günlük bulmaca, oyuncu geçmişine dayalı gelişmiş zorluk ayarı (İ4, İ5).
- **M3 – Gelir ve analitik:** ödüllü reklamla tek seferlik "devam", reklamsız satın alma, olay takibi (oyun uzunluğu, mod dağılımı, kopma anı) (İ6).
- **M4 – Yayın:** iOS/Android derleme, mağaza sayfası ve gerçek oynanış videoları, soft launch (İ7, İ8).
