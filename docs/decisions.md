# Karar Kayıtları

Her karar: tarih, karar, gerekçe, sonuçlar. En yenisi en altta.

## 2026-10-12 — Motor: Unity (Phaser yerine)

- **Karar:** Oyun Unity 6 LTS + C# ile yapılacak. Başlangıçtaki varsayılan TypeScript + Phaser 3 + Capacitor yığını bırakıldı.
- **Gerekçe:** Proje sahibinin tercihi. iOS/Android derlemesi, dokunmatik girdi, titreşim ve reklam SDK entegrasyonları Unity'de yerleşik ve yaygın.
- **Sonuçlar:**
  - Oyun mantığı Unity'den bağımsız saf C# (`Blockbloom.Core`, `noEngineReferences: true`, C# 9) olarak yazılır ve `dotnet test` ile terminalden test edilir. Böylece "terminalden test edilebilir çekirdek" ilkesi korunur.
  - Bulut oturumunda Unity editörü yok; sahne, prefab, derleme ve cihaz testi yerelde yapılır. Kodla kurulum tercih edilir.
  - Tarayıcıda deneme ancak WebGL derlemesiyle mümkün; günlük deneme Unity editöründe Play ile yapılır.

## 2026-10-12 — Parçalar döndürülemez; 37 yön-tanımlı parça kataloğu

- **Karar:** Döndürme/aynalama yok. Her yön ayrı `PieceDef` (37 parça, 1–9 hücre, tamsayı taban ağırlık, toplam 124). Ayrıntı: `docs/GDD.md` §3.3, §4.
- **Gerekçe:** İ1 (basit çekirdek: tek hareket = tek karar; derinlik sıralama ve konumdan gelir). Yön başına ağırlık, zorluk ayarının (İ5) yön düzeyinde yapılmasını sağlar.
- **Sonuçlar:** Katalog kimlikleri kalıcıdır (anlık görüntü bunlara bağlı). Ağırlıklar denge simülasyonuyla ayarlanır.

## 2026-10-12 — Kombo ve tam temizlik kuralları

- **Karar:** `K` 0'dan başlar; patlatan hamlede önce `K+1`, sonra `10 × n² × K` (ilk patlatma K=1). Patlatmayan hamlede `K=0`. Tepsi yenilemesi kombo'yu sıfırlamaz. Satır+sütun aynı anda değerlendirilir, kesişim hücresi bir kez boşaltılır. Tahta patlatmayla tamamen boşalırsa +300 (kombo ile çarpılmaz). Ayrıntı: GDD §6.
- **Gerekçe:** İ2 (kademeli geri bildirim: tek çizgi < çok çizgi < kombo < tam temizlik), İ1 (tepsiler arası planlama ödüllenir).
- **Sonuçlar:** Kombo üst sınırı yok; simülasyonda p99 izlenir.

## 2026-10-12 — Parça üretimi: sığma garantisi ve 3 modlu görünmez zorluk

- **Karar:** Her yeni tepside en az 1 parça tahtaya sığar (onarım adımıyla). Zorluk modu her tepside hesaplanır: `RAHATLATMA` (dolu ≥ 32 veya art arda 2 patlatmasız tepsi; küçük ×1,5, büyük ×0,5, sığan ×3, ≥2 sığan, ≤1 büyük), `AÇIK` (dolu ≤ 15 ve son tepside patlatma var/ilk tepsi; büyük ×1,3, küçük ×0,8), aksi `NORMAL`. Mod oyuncuya gösterilmez. Ayrıntı: GDD §5.
- **Gerekçe:** İ5 (Ascarza ve ark. 2025). `AÇIK` mod meydan okuma arayan oyuncu tipini korur. `M1` (1 hücre) her zaman sığdığı için garanti matematiksel olarak her durumda sağlanabilir.
- **Sonuçlar:** Eşik ve çarpanlar tahmindir; GDD §10 S10/S13 hedefleriyle ayarlanır.

## 2026-10-12 — Deterministik rastgelelik: PCG32 + tamsayı ağırlıklar

- **Karar:** Core kendi PCG32 üretecini kullanır (`System.Random`/`UnityEngine.Random` yok); ağırlıklı seçim yalnız tamsayıyla yapılır; RNG tüketim sırası sabittir (yuva 0–2 parça → onarım → renkler). RNG durumu anlık görüntüye yazılır.
- **Gerekçe:** Testler, 1.000 oyunluk simülasyon ve M2 günlük bulmaca için .NET 8 ile Unity arasında bit düzeyinde aynı sonuç gerekir.
- **Sonuçlar:** Tüketim sırasını değiştirmek eski tohumların oyunlarını değiştirir; yeni karar kaydı gerektirir.

## 2026-10-12 — M0 denge kabul kriterleri

- **Karar:** qa-tester 1.000 tohum × (rastgele, açgözlü) bot simülasyonu koşar. Kesin kriterler: sığmayan tepsi oranı = 0, determinizm %100, puan muhasebesi %100, üretim < 0,1 ms. Tahmini hedefler (ör. açgözlü ortalama 150–800 hamle, ≤ %1 bitmeyen oyun) ilk koşudan sonra güncellenir. Ayrıntı: GDD §10.
- **Gerekçe:** Denge kararları ölçülebilir olmalı; zorluk ayarının hem etkili hem oyunu sonsuzlaştırmayan olduğu doğrulanmalı (İ5).
- **Sonuçlar:** Hedef dışı sonuçlar GDD §10'daki ayar yönleriyle düzeltilir ve buraya kaydedilir.

## 2026-10-12 — Oyunun adı: Blockbloom

- **Karar:** Oyunun adı **Blockbloom** (çalışma adı "Çini Blok" bırakıldı). Kod ad alanı ve assembly: `Blockbloom.Core`.
- **Gerekçe:** Proje sahibinin kararı.
- **Sonuçlar:** İznik çinisinden ilham alan görsel kimlik değişmedi. GDD v0.1 açık soruları belgede yazılı varsayılanlarla onaylandı.

## 2026-10-12 — Görsel kimlik: logo tarzı (İznik bırakıldı)

- **Karar:** Görsel kimliğin referansı Blockbloom logosu (`Assets/_Project/Art/Branding/logo.png`): koyu mor zemin, parlak şeker karolar, patlatmada açan çiçekler. İznik çinisi teması bırakıldı. Palet `CLAUDE.md`'de.
- **Gerekçe:** Proje sahibinin logo ve tarz tercihi; "bloom" adıyla tutarlı.
- **Sonuçlar:** Altı karo rengi logodan ölçüldü (GDD renk kimlikleri 1–6 bu sırayla eşlenir). Çiçek efektleri yalnızca patlatmaya bağlı (ilke 2). Logo Unity için PNG'ye çevrildi (Unity WebP içe aktarmaz).

## 2026-10-12 — M0 denge hedefleri ölçüme göre güncellendi; uzatma ayarı ertelendi

- **Karar:** GDD v0.2 §10: S5 (rastgele bot ort. hamle) 20–60 → **12–30**; S6 (açgözlü bot) 150–800 → **45–120**. Yeni **S16**: tepsi planlayıcı bot (tepsinin 3 parçasını birlikte planlar) ort. hamle **150–400** (tahmin; ölçülen 184,1). Ayar yönergeleri tarama sonuçlarına göre düzeltildi: "S6/S9 yüksekse RAHATLATMA sığma 3 → 2, AÇIK B 13 → 15" kaldırıldı (etkisi ±%5, yön tutarsız); etkili düğmeler `NORMAL` B çarpanı ve RAHATLATMA doluluk eşiği. Denge değerleri (`CoreConfig` çarpanları ve eşikleri) **değişmedi**.
- **Ertelenen ayar:** Oyunu uzatma (ilk aday `NormalLargeMul` 10 → 7: açgözlü +%6, planlayıcı +%39, mod payları aynı) gerçek oyuncu verisi gelene kadar ertelendi. Yeniden açma ölçütü (tahmin): gerçek oyuncu medyan oyun uzunluğu < 40 hamle veya oyuncu testinde "çok çabuk bitiyor" geri bildirimi.
- **Gerekçe:** `docs/reports/m0-balance.md` (qa-tester): kesin kriterlerin hepsi geçti; S5 (16,9) ve S6 (54,4) kaldı, çünkü GDD'nin açgözlü botu ileriye bakmıyor ve v0.1 aralığı tepsi planlayan oyuncuyu varsayıyordu. Botlar gerçek oyuncunun yalnız alt/üst sınırını verir; hedef uzunluk oyuncu verisiyle belirlenmeli (İ1, İ5). §6.4 puan örnekleri (4/13/84/422/1) gerçekçi tahta durumlarıyla yeniden yazıldı; puanlar değişmedi.
- **Sonuçlar:** Yeni sürümde S5/S6 geçer, S16 geçer (184,1). S7 (5,50) ve S12 (medyan 3) sınırda, her değişiklikte izlenir. Eski tohumların oyunları değişmez. Ayar yapılırsa aynı simülasyon komutuyla ölçülüp buraya yazılır.

## 2026-10-12 — Core sağlamlık şartları (WeightProfile, anlık görüntü)

- **Karar:** `WeightProfile` kurucusu `SmallMul ≥ 1` ve `MinFitting ≤ MaxSamePiecePerTray (2)` şartlarını zorlar. `GameSnapshot`'a `ClearedInCurrentTray` eklendi. `TraysDealt` ilk tepsiyi sayar (`= 1 + ⌊Moves/3⌋`). `FromSnapshot` tam dolu satır/sütunlu tahtayı ve tutarsız skor/istatistik alanlarını `ArgumentException` ile reddeder. Ayrıntı: GDD §9.
- **Gerekçe:** Simülasyon hatası B1 (geçersiz profilde onarım adayı kalmıyor, `TryPlace` durumu yarım bırakıyor) ve B2 (bozuk kayıtta "hayalet" puan). Tepsi ortasında alınan kayıtta `T`'nin doğru hesaplanması için bayrak gerekli (S3).
- **Sonuçlar:** §5.4 sığma garantisi her geçerli yapılandırmada sağlanır. Anlık görüntü sürümü/alan değişikliği Unity kayıt biçimini etkiler.

## 2026-10-12 — "Yeni rekor!" kuralı

- **Karar:** "Yeni rekor!" etiketi, oyun başlarken okunan rekora (`oyunBaşıRekor`) göre belirlenir: `sonSkor > oyunBaşıRekor`. Oyun sırasında veya arka plana alınırken rekor depoya yazılsa bile bu değer oyun boyunca değişmez. Sürdürülen oyunda Unity katmanı `oyunBaşıRekor`'u anlık görüntüyle birlikte saklar. HUD ve panel rekor alanı `max(oyunBaşıRekor, skor)` gösterir. Ayrıntı: GDD §7.2–7.3.
- **Gerekçe:** Arka plana alma yazımı (GDD §7.3) rekoru oyun bitmeden günceller; depodaki güncel değerle karşılaştırılırsa oyuncu hak ettiği "Yeni rekor!" anını kaybeder (İ2: başarıya bağlı geri bildirim).
- **Sonuçlar:** Core değişmez; kural Unity katmanında (UI/kayıt) uygulanır ve Unity Test Framework ile test edilir.

## 2026-10-12 — M0 Unity katmanı ve Core sağlamlaştırma

- **Karar:** Unity M0 sahneye bağımlı değil; `Bootstrap` (`RuntimeInitializeOnLoadMethod`) her şeyi kodla kurar, karolar çalışma anında prosedürel üretilir. M0'da yerleşik renderer (URP M1'de art-ui ile), metin için legacy uGUI `Text` (TMP yok), düğmeler için EventSystem yerine kendi isabet testi. Girdi: Input System, yalnız eski sistem açıksa eski `Input`.
- **Core:** `WeightProfile` geçersiz değerleri reddeder (`minFitting = 0` artık geçersiz); `FromSnapshot` tutarsız kayıtları reddeder (`Stats.MaxCombo == MaxCombo` dahil); `NextTray` hiçbir parçanın sığmadığı tahtada RNG'ye dokunmadan atar. Bırakma çapası hesabı testlenebilsin diye Core'a taşındı (`DropAnchor`). Geçerli girdilerde üretim dizisi değişmedi (simülasyon V1 doğrulaması geçti).
- **Kayıt:** Yarım oyun `SavedGameDto` v1 (PlayerPrefs `bb.savedGame.v1`, `bestAtStart` dahil), rekorlar `bb.records.*`.
- **Doğrulama:** Unity editörü olmadığından Game kodu `Unity3D.SDK` (NuGet) + imza taklitleriyle iki girdi kipinde derleme kontrolünden geçirilir (`tests/Blockbloom.Game.CompileCheck*`, çözüm dışında). Unity 6'ya özgü API farklarını yakalamaz; yerel Unity doğrulaması şart.
- **Gerekçe:** Bulut ortamında Unity editörü yok; sahne/prefab YAML'ı elle yazmak kırılgan. Ek paket ve içe aktarma adımı gerektirmeyen en kısa yol.
