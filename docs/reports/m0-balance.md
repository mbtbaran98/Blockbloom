# M0 denge simülasyonu raporu

- **Tarih:** 2026-10-12 · **Hazırlayan:** qa-tester · **Kapsam:** GDD v0.1 §10 (S1–S15), Core `CoreConfig` başlangıç değerleri
- **Ham veri:** `docs/reports/m0-balance.json` (ana koşu + rastgele/açgözlü/sezgisel tarama), `docs/reports/m0-balance-planner-sweep.json` (planlayıcı bot taraması, 300 oyun)
- **Araç:** `tools/Blockbloom.Sim` (net8.0 konsol, Core kaynaklarını doğrudan derler; Core değiştirilmedi)

## Özet

- **Kesin kriterlerin hepsi geçti** (S1, S2, S3, S4, S14, S15). Sığma garantisi 202.508 tepside bir kez bile bozulmadı. Determinizm, anlık görüntüden devam ve puan muhasebesi %100 tuttu.
- **İki tahmin kriteri kaldı: S5 ve S6.** İkisi de "oyun beklenenden kısa" yönünde. Rastgele bot ortalama 16,9 hamle oynadı (hedef 20–60). Açgözlü bot 54,4 hamle oynadı (hedef 150–800). Diğer tahmin kriterleri (S7–S13) hedef içinde.
- **S6'nın asıl nedeni botun ileriye bakmaması.** GDD'deki açgözlü bot yalnızca anlık puana bakıyor; S6 aralığı ise daha güçlü bir oyun varsayıyor. Bilgi amaçlı iki ek bot koşuldu:
  - "Sezgisel" bot: 81,6 hamle.
  - Tepsinin tamamını arayan "planlayıcı" bot: 184,1 hamle (medyan 137).
  - Yani S6 aralığına yalnızca tepsi düzeyinde plan yapan oyuncu ulaşıyor.
- **Görünmez zorluk ayarı en çok zorlanan oyuncuya yardım ediyor** (İ5 ile uyumlu).
  - RAHATLATMA tepsilerinin payı: rastgele botta %56,5, açgözlüde %23,1, planlayıcıda %3,3.
  - Zorluk açık/kapalı hamle oranı (S13): açgözlüde 1,27, planlayıcıda 1,23.
- **Ayar önerisi (bkz. §6):** önce `NORMAL` B çarpanı 10 → 7.
  - Mod paylarını değiştirmiyor.
  - Becerili oyunu belirgin uzatıyor: planlayıcı +%39, açgözlü +%6.
  - RAHATLATMA eşiği 32 → 28 açgözlüyü daha çok uzatıyor (+%20), ama RAHATLATMA payını %42'ye çıkarıp S10'u bozuyor.

## 1. Yöntem

**Koşular.**
- Tohumlar 1..1000. Her bot × zorluk için 1.000 oyun. Hamle tavanı 5.000; tavana takılan oyun "bitmedi" sayılır.
- **Zorluk AÇIK:** normal `GameState` (`NewGame` → `LegalMoves(list)` → `TryPlace`).
- **Zorluk KAPALI:** Core'da böyle bir seçenek yok. Araç içinde ince bir döngü (`ThinGame`) yazıldı:
  - Bileşenler: `Grid`, `Generator.NextTray(grid, Difficulty.Profile(Normal))`, `ScoreKeeper` ve `DifficultyState`.
  - Kurallar `GameState.TryPlace` ile aynı sırada uygulanıyor.
  - Her tepsi NORMAL profille üretiliyor (asgari sığan 1).

**Botlar.**
- **Rastgele (GDD):** `LegalMoves` içinden tekdüze seçim yapar. Kendi `Pcg32(seed + 1_000_000)` örneğini kullanır; oyun RNG'sine dokunmaz.
- **Açgözlü (GDD):** En yüksek anlık `ScoreBreakdown.Total`'ı seçer. Eşitlikte hamle sonrası dolu hücresi az olanı, sonra küçük (yuva, y, x) olanı seçer. Puanı bit tahtası üzerinde tahmin eder. Bu tahmin her hamlede gerçek sonuçla karşılaştırıldı: 0 fark.
- **Sezgisel (bilgi amaçlı, S kriterlerine girmez):** Değeri şöyle hesaplar: anlık puan + hareketlilik − 8 × tek hücrelik delik − 500 × tepside kalıp artık sığmayan parça. Hareketlilik = hamle sonrası tahtaya sığan katalog parçalarının taban ağırlık toplamı (0..124).
- **Planlayıcı (bilgi amaçlı, S kriterlerine girmez):** Tepside kalan parçaların tüm sıra ve çapa kombinasyonlarını kombo dahil puanla arar. Yaprak değeri sezgisel botla aynıdır. Yerleştirilemeyen her parça için −1000 uygulanır. Arama belleklidir ve deterministiktir.

**Doğrulamalar.**
- **Gölge karşılaştırma (V1):** Zorluk AÇIK her oyunda yanında bir gölge `ThinGame` (zorluk açık) aynı hamleleri oynadı. Her hamleden sonra şunlar karşılaştırıldı: tahta bitleri, tepsi (kimlik + renk), skor/kombo/T, istatistikler, oyun sonu. Tepsi yenilemelerinde ve oyun sonunda RNG durumu da karşılaştırıldı. Sonuç: 4.000/4.000 oyun birebir.
- **Bağımsız ince döngü koşusu:** İlk 100 tohumda bağımsız ince döngü koşusu `GameState` ile hamle dizisi özeti (hash) ve skor bakımından aynı çıktı: 400/400.
- **Tarama ile Core'un aynılığı:** Taramadaki parametreli kopya (`SimDifficulty.Baseline` / `Off`) Core koşularıyla 6.000/6.000 oyunda aynı.
- **Gölgenin ek işlevi:** Gölge, `Generator`'a sayaçlı bir `IRandom` sarmalayıcısı verdi; çekilen sayı dizisi değişmiyor. Bu sayede onarım çekimleri ayırt edildi (S14) ve `NextTray` süresi ölçüldü (S15).
- **S3 (determinizm):**
  - İlk 100 tohum her koşuda düz olarak yeniden oynandı.
  - Zorluk AÇIK her oyun ayrıca ikinci kez oynandı. Bu oynayışın ortasında `ToSnapshot` → `System.Text.Json` → `FromSnapshot` ile devam edildi.
  - Hamle/puan dizisinin özeti karşılaştırıldı.
- **S4 (puan muhasebesi):**
  - Σ hamle puanı = son skor.
  - Her patlatmada `CellsCleared = 8r + 8c − r·c`.
  - `Stats.Moves` ve `TraysDealt` (= 1 + hamle/3) tutarlı.
  - Oyun bitmemişken yasal hamle sayısı hiç 0 olmadı.
  - Açgözlü botun puan tahmini gerçek sonuçla aynı.
- **S15 (süre):** Ayrıca "saf koşu" yapıldı: GameState + rastgele/açgözlü bot, 2.000 oyun, doğrulama ve ölçüm yok.

**Ortam.** .NET 8.0.131, Linux, 4 çekirdek, Release derleme. Tam koşu (4 bot × 2 zorluk + doğrulamalar + tarama) yaklaşık 15 dakika sürdü (planlayıcı taramasıyla eşzamanlı); bunun neredeyse tamamı planlayıcı bot. GDD botlarıyla 2.000 oyun 0,34 s sürdü.

## 2. Ana sonuçlar (zorluk AÇIK, 1.000 oyun)

| Metrik | Rastgele | Açgözlü | Sezgisel* | Planlayıcı* |
|---|---|---|---|---|
| Hamle — ort / medyan | 16,9 / 14 | **54,4 / 47** | 81,6 / 65 | 184,1 / 137 |
| Hamle — p10 / p90 / p99 | 11 / 26 / 41 | 23 / 92 / 173 | 29 / 158 / 302 | 47 / 379 / 683 |
| Skor — ort / medyan | 94,4 / 71 | **518,6 / 451** | 832,1 / 633 | 2.160,8 / 1.562 |
| Skor — p10 / p90 / p99 | 38 / 190 / 328 | 184 / 953 / 1.718 | 233 / 1.668 / 3.341 | 490 / 4.577 / 8.102 |
| Tepsi sayısı — ort / medyan / p90 | 6,0 / 5 / 9 | 18,5 / 16 / 31 | 27,6 / 22 / 53 | 61,7 / 46 / 127 |
| Patlatılan çizgi — ort / medyan / p90 | 2,5 / 2 / 6 | 21,0 / 18 / 39 | 33,7 / 26 / 68 | 86,9 / 64 / 184 |
| Patlatan hamle oranı | %12,9 | %37,1 | %38,9 | %38,4 |
| En yüksek kombo — medyan / p90 / p99 / maks | 1 / 2 / 3 / 4 | 3 / 4 / 5 / 6 | 3 / 4 / 6 / 6 | 3 / 4 / 6 / 7 |
| Tam temizlik — oyunların %'si | %0 | %2,6 | %2,6 | %3,8 |
| ≤ 9 hamlede biten oyun | %7,5 | %0,1 | %0 | %0 |
| Hamle tavanına takılan | %0 | %0 | %0 | %0 |
| Oyun sonunda tepside kalan parça (1 / 2 / 3) | 873 / 127 / 0 | 898 / 102 / 0 | 935 / 65 / 0 | 917 / 83 / 0 |

\* Bilgi amaçlı, GDD dışı botlar.

**Çizgi sayısı dağılımı (patlatan hamleler içinde).**

| n | Rastgele | Açgözlü | Sezgisel | Planlayıcı |
|---|---|---|---|---|
| 1 | %87,4 | %96,1 | %94,2 | %77,9 |
| 2 | %11,9 | %3,8 | %5,5 | %21,3 |
| 3 | %0,6 | %0,11 | %0,23 | %0,77 |
| 4+ | %0,05 | %0 | %0,01 | %0,03 |
| Aynı hamlede satır + sütun | %8,1 | %1,2 | %1,9 | %13,2 |

**En yüksek kombo histogramı (oyun sayısı).**

| maks K | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|---|
| Rastgele | 260 | 526 | 178 | 33 | 3 | – | – | – |
| Açgözlü | – | 49 | 358 | 403 | 150 | 37 | 3 | – |
| Planlayıcı | – | 21 | 194 | 442 | 264 | 68 | 10 | 1 |

**Zorluk modu dağılımı (tepsi payı; parantez içinde üretim anındaki ortalama dolu hücre).**

| Mod | Rastgele | Açgözlü | Sezgisel | Planlayıcı |
|---|---|---|---|---|
| AÇIK | %16,9 (0,2) | %15,0 (7,1) | %11,1 (7,6) | %26,3 (11,3) |
| NORMAL | %26,6 (17,8) | %61,9 (23,5) | %64,2 (23,9) | %70,3 (21,3) |
| RAHATLATMA | %56,5 (33,2) | %23,1 (35,1) | %24,8 (35,2) | %3,3 (35,0) |

RAHATLATMA'yı neyin tetiklediği:
- Açgözlüde %91 yalnızca `dolu ≥ 32`, %6 yalnızca `T ≥ 2`, %3 ikisi birden.
- Rastgelede %44 yalnızca doluluk, %34 yalnızca T, %22 ikisi birden.
- Planlayıcıda neredeyse tamamı doluluk.

**Zorluk KAPALI karşılaştırma (her tepsi NORMAL, ince döngü).**

| | Rastgele | Açgözlü | Sezgisel | Planlayıcı |
|---|---|---|---|---|
| Hamle ort (kapalı) | 13,1 | 42,9 | 55,0 | 150,2 |
| Hamle ort (açık) | 16,9 | 54,4 | 81,6 | 184,1 |
| Açık / kapalı | 1,29 | **1,27** | 1,48 | 1,23 |
| ≤ 9 hamlede biten (kapalı) | %16,2 | %0,2 | %0,1 | %0 |
| Skor ort (kapalı) | 68,7 | 401,5 | 548,4 | 1.698,0 |
| Yeni tepside sığan 1 / 2 / 3 (kapalı) | 221 / 510 / 4.067 | 262 / 902 / 13.532 | 305 / 1.070 / 17.374 | 171 / 1.016 / 49.268 |

**Tepsi üretimi (S15).** `NextTray` 202.508 çağrıda ortalama 1,85 µs sürdü; koşu başına medyan 1,1–2,2 µs, p99 2,2–9,5 µs. En büyük tekil değer 452 µs; bu bir GC/zamanlayıcı aykırı değeri ve planlayıcı taramasıyla eşzamanlı koşuda ölçüldü. Saf koşuda 2.000 oyun 0,34 s sürdü.

**Parça frekansı (S14).** NORMAL tepsilerde onarım çekimleri hariç 488.350 yuva sayıldı. `taban/124`'ten en büyük sapma 0,106 yüzde puanı (I4V). Koşu bazında en kötü değer 0,48 yp (rastgele-açık, I4H; küçük örneklem). Parça bazındaki tüm sapmalar JSON'da `s14_normalFreqNoRepair` alanında.

**Onarım sıklığı.** Onarım nadir:
- NORMAL'de 1.000 oyunda 0–47 tepside onarım oldu.
- RAHATLATMA'da tepsilerin %1,4–2,3'ünde onarım oldu.
- Yeni RAHATLATMA tepsilerinin %9–14’ünde tam 2 parça sığıyor; bu asgari sınırın kendisi.

## 3. S kriterleri

| # | Metrik | Hedef | Ölçülen | Tür | Sonuç |
|---|---|---|---|---|---|
| S1 | Yeni tepside hiç sığan yok | = 0 | 0 / 202.508 tepsi (tüm koşular) | Kesin | **GEÇTİ** |
| S2 | RAHATLATMA'da < 2 sığan | = 0 | 0 / 16.573 | Kesin | **GEÇTİ** |
| S3 | Determinizm + anlık görüntüden devam | %100 | düz tekrar 800/800; ortadan devam 4.000/4.000 | Kesin | **GEÇTİ** |
| S4 | Puan muhasebesi, kesişim formülü | %100 | 0 hata / 598.329 hamle | Kesin | **GEÇTİ** |
| S5 | Rastgele ort. hamle | 20–60 | 16,9 | Tahmin | **KALDI** |
| S6 | Açgözlü ort. hamle | 150–800 | 54,4 | Tahmin | **KALDI** |
| S7 | Açgözlü/rastgele skor oranı | ≥ 5 | 5,50 | Tahmin | GEÇTİ (sınıra yakın) |
| S8 | Açgözlü ≤ 9 hamlede biten | ≤ %2 | %0,1 | Tahmin | GEÇTİ |
| S9 | Açgözlü bitmedi | ≤ %1 | %0 | Tahmin | GEÇTİ |
| S10 | Açgözlü mod payları | RAHAT %10–35, AÇIK %10–40 | RAHAT %23,1, AÇIK %15,0 (NORMAL %61,9) | Tahmin | GEÇTİ |
| S11 | Açgözlü patlatan hamle | %20–45 | %37,1 | Tahmin | GEÇTİ |
| S12 | Açgözlü maks kombo medyanı | ≥ 3 | 3 | Tahmin | GEÇTİ (sınırda) |
| S13 | Zorluk açık/kapalı açgözlü hamle | 1,15–2,0 | 1,27 | Tahmin | GEÇTİ |
| S14 | NORMAL parça frekansı − taban/124 | ≤ 1,5 yp | en büyük 0,106 yp | Kesin | **GEÇTİ** |
| S15 | Tepsi üretimi; 2.000 oyun | < 0,1 ms; < 60 s | ort. 1,85 µs; 0,34 s | Kesin | **GEÇTİ** |
| V1 | İnce döngü = GameState (araç doğrulaması) | %100 | gölge 4.000/4.000; bağımsız 400/400; tarama 6.000/6.000 | Doğrulama | GEÇTİ |
| V2 | Tepsi kuralları (aynı kimlik ≤ 2, RAHATLATMA ≤ 1 B) | = 0 ihlal | 0 / 202.508 | Doğrulama | GEÇTİ |

## 4. Duyarlılık taraması (Core değiştirilmeden)

Core kuralları araç içinde parametreli olarak kopyalandı (`SimDifficulty`). Bu kopya, baseline/off varyantlarında Core koşularıyla oyun oyun aynı sonucu verdi. Varyantlar `Generator.NextTray(grid, WeightProfile)` ile uygulandı. Rastgele, açgözlü ve sezgisel botlar 1.000'er oyun, planlayıcı 300 oyun oynadı. Tablodaki değerler ortalama hamle; parantez içinde RAHATLATMA tepsi payı var.

| Varyant | Açıklama | Rastgele | Açgözlü | Sezgisel | Planlayıcı (300) |
|---|---|---|---|---|---|
| baseline | GDD v0.1 | 16,9 (%56,5) | 54,4 (%23,1) | 81,6 (%24,8) | 189,5 (%3,5) |
| off | Zorluk kapalı | 13,1 | 42,9 | 55,0 | 151,0 |
| relief28 | RAHATLATMA `dolu ≥ 28` | 17,5 (%63,1) | 65,2 (**%42,4**) | 111,9 (%45,1) | 242,9 (%8,9) |
| relief24 | RAHATLATMA `dolu ≥ 24` | 17,7 (%66,4) | 75,7 (**%62,0**) | 137,5 (%65,9) | 343,3 (%22,1) |
| reliefT1 | RAHATLATMA `T ≥ 1` | 16,7 (%74,7) | 57,3 (%34,5) | 85,8 (%34,5) | 193,5 (%5,3) |
| reliefFit2 | RAHATLATMA sığma ×3 → ×2 | 16,0 | 51,2 | 77,4 | 183,9 |
| reliefFit5 | RAHATLATMA sığma ×3 → ×5 | 17,0 | 53,5 | 89,9 | 199,9 |
| openL15 | AÇIK B ×1,3 → ×1,5 | 16,3 | 54,1 | 84,6 | 180,1 |
| normalL7 | NORMAL B ×1,0 → ×0,7 | 16,4 (%56,8) | 57,6 (%23,0) | 91,8 (%24,5) | **262,6** (%2,8) |
| allL7 | NORMAL B ×0,7 + AÇIK B ×1,3 → ×1,0 | 16,8 | 58,4 | 97,1 | 244,1 (%3,0) |
| combo | relief28 + NORMAL B ×0,7 + RAHATLATMA sığma ×4 | 17,5 (%62,9) | 71,9 (%43,2) | 127,9 (%45,3) | 326,6 (%7,8) |

Planlayıcıda `allL7` (244) `normalL7`'den (263) düşük çıktı; 300 oyunda standart hata yaklaşık ±12 olduğu için bu fark kısmen gürültü. AÇIK B çarpanını düşürmenin ek bir kazancı görülmedi. `combo` varyantı planlayıcıyı +%72 uzatırken RAHATLATMA payını açgözlüde %43'e çıkarıyor (S10 dışı).

Hiçbir varyantta S1/S2 ihlali, muhasebe hatası ya da tavana takılan oyun olmadı. Planlayıcının baseline'ı 300 oyunda 189,5; 1.000 oyunda 184,1. Fark örneklem gürültüsü (±~10).

## 5. Gözlemler

1. **Garanti ve kurallar sağlam.**
   - Yenilemeden hemen sonra hiçbir oyun bitmedi.
   - Oyunlar her zaman tepside 1 ya da 2 parça kalmışken bitti; 3 parça kalmışken biten oyun yok.
   - RAHATLATMA'da ≥ 2 sığan ve ≤ 1 B kuralları istisnasız tuttu.
2. **Oyun, GDD'nin tahmin ettiğinden daha zor ve kısa.**
   - Tepsi başına yaklaşık 11,6 hücre geliyor. Dengede kalmak için tepsi başına yaklaşık 1,45 çizgi patlatmak gerekiyor.
   - Açgözlü bot tepsi başına 1,14 çizgi patlatıyor, planlayıcı 1,41. Bu yüzden tahta yavaşça doluyor.
   - Açgözlü botun patlatmaları %96 tek çizgi. Çoklu çizgi kurmak için tepsiyi planlamak gerekiyor: planlayıcıda 2+ çizgi payı %22.
3. **Zorluk ayarının etkisi zayıf oyuncuda büyük, güçlü oyuncuda küçük.**
   - Rastgele botta "≤ 9 hamlede biten oyun" oranı ayar kapalıyken %16,2, açıkken %7,5.
   - Planlayıcı RAHATLATMA'yı nadiren görüyor (%3,3).
   - İ5'in amacıyla uyumlu: zorlanana yardım ediyor, becerili oyunu bozmuyor.
   - Rastgele botta RAHATLATMA payı %56,5. Yeni başlayan biri tepsilerin yarısından fazlasını kolaylaştırılmış alacak. Bu görünmez olmalı; M1'de oyuncu testinde "parçalar çok kolay" algısı izlenmeli.
4. **AÇIK mod pek tetiklenmiyor (açgözlüde %15).** Koşul `dolu ≤ 15 ve T = 0`; açgözlü bot yeni tepsiyi aldığında tahtada ortalama 23 dolu hücre oluyor. Boş tahta + meydan okuma durumu çoğunlukla yalnızca oyun başında ve iyi oyunda oluşuyor.
5. **Kombo ve tam temizlik.**
   - Maks kombo p99 = 5–6, maksimum 7. Puan patlaması riski yok; M0'da üst sınıra gerek yok.
   - Tam temizlik oyunların %2,6–3,8'inde görülüyor, yani "en nadir an" (İ2) hedefiyle uyumlu.
   - Açgözlü botta maks kombo medyanı tam 3 (S12 sınırda). Zorluk kapalıyken 2'ye düşüyor.
6. **Bazı düğmelerin etkisi zayıf.**
   - RAHATLATMA sığma çarpanı (×2 / ×5) ve AÇIK B çarpanı (×1,5) ortalama hamleyi yalnızca ±%5 oynatıyor.
   - GDD §10'un "S6/S9 yüksekse" yönergesindeki bu iki düğme, ileride gerekirse oyunu kısaltmak için de zayıf kalacak.
   - Etkili düğmeler: RAHATLATMA doluluk eşiği ve NORMAL B ağırlığı.
7. **Performans bütçesinin çok altında.** Tepsi üretimi hedefin yaklaşık 1/50'si (1,85 µs, hedef < 100 µs).

## 6. Öneriler (GDD §10 ayar yönergelerine göre; karar game-designer'ın, GDD değiştirilmedi)

1. **S5 ve S6 hedeflerini yeniden kalibre et (öncelikli).** GDD §10 "tahmini aralıklar ilk simülasyondan sonra güncellenir" diyor. GDD'nin tanımladığı açgözlü bot ileriye bakmıyor; bu yüzden 150–800 aralığı ona göre değil, tepsiyi planlayan bir oyuncuya göre. Öneri:
   - S5 (rastgele) 12–30 olsun.
   - S6 (açgözlü) 45–120 olsun.
   - Beceri tavanı için yeni bir kriter eklensin: "planlayıcı bot ortalama hamle 150–400". Bu kriter, gerçek oyuncunun yaşayacağı oyun uzunluğuna daha yakın.
   - S7 sınıra yakın (5,5). Değişikliklerden sonra izlenmeli.
2. **Oyunu uzatmak istenirse ilk adım `NormalLargeMul` 10 → 7 (B ağırlığını düşür; §10 "S6 düşükse" yönü).**
   - Ortalama hamle: açgözlü 54 → 58 (+%6), sezgisel 82 → 92 (+%12), planlayıcı 190 → 263 (+%39).
   - Mod payları değişmiyor (S10 korunur). S1/S2 sağlam.
   - Etkisi becerili oyunda büyük, yani beceriyi ödüllendiriyor (İ1).
   - Aynı yönde `allL7` (AÇIK B 13 → 10 ek olarak) küçük bir ek kazanç veriyor. Ancak AÇIK modun "meydan okuma" amacını zayıflattığı için önerilmiyor.
3. **RAHATLATMA eşiği 32 → 28 (§10'daki diğer "S6 düşükse" yönü) dikkatle değerlendirilmeli.**
   - Açgözlü botu +%20, sezgiseli +%37 uzatıyor.
   - Ama açgözlüde RAHATLATMA payını %23 → %42 çıkarıyor; S10 üst sınırı %35 olduğu için S10 KALIR.
   - Uygulanacaksa S10 aralığı da birlikte güncellenmeli. 24'e indirmek RAHATLATMA'yı baskın mod yapıyor (%62); önerilmez.
4. **§10 "S6/S9 yüksekse" yönergeleri şu an uygulanmamalı** (RAHATLATMA sığma 3 → 2, AÇIK B 13 → 15). S6 düşük, S9 = %0. Ayrıca bu iki düğmenin etkisi ±%5 ile sınırlı.
5. **`T ≥ 2` koşulu korunmalı.** `T ≥ 1` RAHATLATMA payını rastgele botta %75'e çıkarıyor ve kazancı küçük (+%5).
6. **Her değişiklikten sonra aynı komutla yeniden ölçüm yapılmalı** ve sonuç `decisions.md`'ye eklenmeli. `CoreConfig` değeri değişirse eski tohumların oyunları değişir; bu, karar kaydı gerektirir.

## 7. Kenar durumları ve bulunan hatalar

Önem sırasıyla. Core düzeltilmedi; yeniden üretme adımları aşağıda.

**B1 — Orta (gizli, yapılandırmaya bağlı): `Generator.NextTray` belirli geçerli profillerde istisna atıyor; `TryPlace` içinde olursa oyun durumu yarım kalır.**
- `WeightProfile` kurucusu `minFitting = 3` değerini ve 0 sınıf çarpanını kabul ediyor.
- Ama `M1` bir tepside en fazla 2 kez gelebiliyor. Yalnızca `M1`'in sığdığı bir tahtada (ör. dama tahtası) onarım için aday kalmıyor ve `InvalidOperationException("Uretimde aday parca kalmadi")` atılıyor.
- Aynı durum `SmallMul = 0` iken asgari 1 sığanla da oluşuyor.
- Mevcut `CoreConfig` değerleri (asgari 1/2, K çarpanı > 0) güvenli. Ancak denge ayarında `ReliefMinFitting = 3` ya da bir K çarpanı = 0 denenirse oyun çöker.
- `GameState.TryPlace` üreticiyi parça yerleştirilip puanlandıktan **sonra** çağırıyor. İstisna olursa tepsi boş kalır ve `IsGameOver == false` olur; durum tutarsızlaşır.

```csharp
var g = Grid.FromBits(Checkerboard());                  // yalniz M1 sigar
new Generator(PieceCatalog.All, new Pcg32(1))
    .NextTray(g, new WeightProfile(10, 10, 10, 1, 3, 3)); // InvalidOperationException
new Generator(PieceCatalog.All, new Pcg32(1))
    .NextTray(g, new WeightProfile(0, 10, 10, 1, 1, 3));  // InvalidOperationException
```

Önerilen düzeltme:
- `WeightProfile` kurucusunda `minFitting ≤ CoreConfig.MaxSamePiecePerTray` ve `smallMul ≥ 1` şartlarını koy.
- Ya da onarım çekiminde sığan adaylara en az 1 ağırlık ver.
- Buna bir birim testi ekle.

**B2 — Düşük: `GameState.FromSnapshot` tutarsız anlık görüntüleri kabul ediyor.** Bozuk ya da kurcalanmış kayıtta "hayalet" puan oluşabilir.
- Tam dolu satırı olan tahta kabul ediliyor (`Bits = 0xFF`, tepsi `M1`). Ardından ilgisiz bir yere `M1` konunca (4,5) 1 çizgi patlıyor ve hamle 11 puan getiriyor.
- `Stats.MaxCombo` (0) ile `MaxCombo` (5) arasındaki tutarsızlık kabul ediliyor.
- Tepsi doluyken `ClearedInCurrentTray = true` kabul ediliyor.
- Önerilen düzeltme: `FromSnapshot` içinde `FullRows/FullCols != 0` ise reddet. `Grid.FromRaw` testler için serbest kalabilir. `Stats` ile skor alanlarının tutarlılığını da denetle.

**B3 — Düşük: Tam dolu tahtada `NextTray` yanıltıcı mesajla istisna atıyor.**
- Yeniden üretme: `NextTray(Grid.FromBits(ulong.MaxValue), Normal)` → "katalog/profil gecersiz".
- Yalnızca doğrudan API ile erişilebiliyor; oyunda olamaz.
- Öneri: anlamlı bir `ArgumentException` at.

**N1 — Bilgi:** `GameSnapshot` alanları public field. `System.Text.Json` varsayılan ayarlarla `{}` üretiyor ve hata vermiyor. `IncludeFields = true` gerekli. Unity `JsonUtility` etkilenmez; ama araç ya da sunucu tarafında kullanılacaksa `docs/dev.md`'ye not düşülmeli.

**N2 — Bilgi:** `GameState.LegalMoves()` (`IEnumerable` sürümü) tembel çalışıyor. Numaralandırma sırasında `TryPlace` çağrılırsa hata vermeden yeni duruma göre hamle üretmeye devam ediyor. Belgelenmeli ya da botlarda liste sürümü kullanılmalı (simülasyon liste sürümünü kullanıyor).

**Denenip sorun bulunmayan kenar durumlar:**
- Aynı hamlede satır + sütun + tam temizlik: 341 puan, 15 hücre.
- Tek boş hücreye `M1` ile 16 çizgi: 2.861 puan, ardından AÇIK modda yeni tepsi.
- Yalnızca `M1`'in sığdığı tahtada RAHATLATMA: tepside tam iki `M1`.
- Hiçbir parçanın sığmadığı durumda oyun sonu.
- 598.329 hamlede yasal hamle listesi ile oyun sonu tutarlılığı.
- Bunlar için `tests/Blockbloom.Core.Tests/EdgeCaseTests.cs` eklendi (4 test).

**Test edilemeyenler:** Çoklu dokunma (GDD §8) ve uygulamanın arka plana alınıp rekorun yazılması (§7.3) Unity katmanına ait; o katman henüz yok. Unity tarafı yazıldığında Unity Test Framework ile test edilmeli:
- EditMode/PlayMode: ikinci dokunuş yok sayılıyor mu.
- `OnApplicationPause(true)` anında rekor yazılıyor mu.
- `JsonUtility` ile `GameSnapshot` gidiş-dönüşü.

## 8. Yeniden çalıştırma

```bash
# Ana koşu + tarama (yaklaşık 15 dk; planlayıcı olmadan yaklaşık 15 s)
dotnet run -c Release --project tools/Blockbloom.Sim -- --games 1000 --seed-start 1 \
  --bot random,greedy,smart,planner --difficulty all --sweep --out docs/reports/m0-balance.json
# Yalnız GDD botları (hızlı)
dotnet run -c Release --project tools/Blockbloom.Sim -- --games 1000 --bot all --difficulty all
# Planlayıcı taraması (300 oyun)
dotnet run -c Release --project tools/Blockbloom.Sim -- --games 300 --bot greedy --difficulty on \
  --no-verify --no-timing --sweep --sweep-bots planner --out docs/reports/m0-balance-planner-sweep.json
```

Çıkış kodu 0 = kesin kriterler geçti, 1 = bir kesin kriter kaldı.
