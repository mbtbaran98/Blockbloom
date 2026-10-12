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
