# Blockbloom — Proje Rehberi

Bu dosya projenin ortak hafızasıdır. Ana oturum (koordinatör) ve tüm alt ajanlar işe başlamadan önce bunu okur. Karar değiştiğinde önce bu dosya güncellenir.

## Vizyon

8×8 ızgaralı, üç parçalı, satır/sütun patlatmalı bir blok bulmaca oyunu. Hedef: türün en "tatmin edici" hissettiren oyunu olmak. Oyunun adı **Blockbloom**. Başka bir oyunun adı, logosu, görselleri veya marka unsurları kullanılmaz.

## Görsel kimlik (kaynak: `Assets/_Project/Art/Branding/logo.png`)

Logo görsel kimliğin referansıdır: koyu mor-lacivert gece zemini, parlak ve hafif hacimli (üstten ışık, alt gölge) "şeker" karolar, "bloom" teması — patlatmada açan çiçekler, yapraklar, küçük parıltılar. Koyu kontur, yuvarlatılmış köşeler, neşeli ama sakin ton.

Palet (logodan ölçülmüş başlangıç değerleri; art-ui netleştirir):

| Rol | Renk |
|---|---|
| Zemin | `#18114C` |
| Karo 1 mavi | `#199FEF` |
| Karo 2 kırmızı | `#F2433B` |
| Karo 3 sarı | `#FDDA23` |
| Karo 4 yeşil | `#76CF36` |
| Karo 5 pembe | `#EA56A7` |
| Karo 6 mor | `#AD48E3` |
| Vurgu turkuaz | `#1BD3F8` |

Çiçek/parıltı efektleri ilke 2'ye bağlıdır: yalnızca oyuncunun patlatmasında ve patlatmanın büyüklüğüyle orantılı çıkar; boşta süs animasyonu yok.

## Tasarım ilkeleri (araştırmadan türetildi — kaynaklar: `docs/research.md`)

1. **Basit çekirdek, derin planlama.** Aynı anda üç parça; zaman sınırı yok; ceza yok; çevrimdışı oynanır.
2. **Hamleye bağlı, kademeli geri bildirim.** Ses + titreşim + görsel efekt; tek satır < çoklu satır < kombo şeklinde büyür. Rastgele süs efekti yok (Kao ve ark. 2024: hamleden bağımsız büyütme motivasyonu düşürdü).
3. **Titreşim görüntüyle tam senkron.** Az da olsa titreşim deneyimi belirgin iyileştirir (Waterloo, CHI 2021).
4. **Merak motoru.** Açılan resimli macera modu, haftalık içerik, günlük bulmaca. Merak oyun süresinin en güçlü öngörücüsü.
5. **Görünmez zorluk ayarı.** Zorlanan oyuncuya yerleşebilir parça olasılığı artar; her tepside en az bir parça mutlaka yerleşebilir (Ascarza ve ark. 2025: elde tutmayı artırdı, harcamayı düşürmedi).
6. **Saygılı reklam.** Ödüllü reklam öncelikli; oyun ortasında ve ödül ekranında geçiş reklamı yok (Deloitte & AdMob 2025: ödül ekranındaki rahatsız edici reklam bırakmayı ~3 kat artırıyor).
7. **Dürüst pazarlama.** Reklamlar gerçek oynanışı gösterir. Yanıltıcı reklam en hızlı kopan oyuncuyu getirir.
8. **Hafif ve hızlı.** Düşük donanımlı cihazda 60 fps, küçük indirme boyutu.

## Teknik yığın (karar: 2026-10-12, bkz. `docs/decisions.md`)

- **Unity 6 LTS (6000.0.x) + C#**, 2D URP. iOS/Android derlemesi doğrudan Unity'den; ayrı paketleyici yok.
- Oyun mantığı (ızgara, yerleştirme, puan, parça üretimi, zorluk) **saf C#**: `Blockbloom.Core` assembly'si, `noEngineReferences: true`. `UnityEngine` kullanmaz; C# 9 ile sınırlı (Unity uyumu).
- Core, Unity editörü olmadan terminalden test edilir: `dotnet test tests/Blockbloom.Core.Tests` (NUnit, .NET 8). Test projesi Core kaynak dosyalarını doğrudan derler; aynı dosyalar Unity'de de kullanılır.
- Bağımlılık yönü tek: Unity kodu (sahneler, girdi, efekt, UI) Core'u kullanır, Core Unity'yi bilmez. Unity tarafı testleri: Unity Test Framework (EditMode/PlayMode).
- Ortam sınırı: bulut oturumunda Unity editörü ve lisansı yok. Sahne, prefab ve derleme yerel makinede Unity ile doğrulanır. Ajanlar sahne/prefab YAML'ını elle yazmak yerine kodla kurulumu (ör. sahneyi çalışma anında kuran `Bootstrap` bileşeni, editör menü komutu) tercih eder.
- Hızlı deneme: Unity editöründe Play; paylaşım için WebGL derlemesi.

## Klasör yapısı (hedef)

```
Assets/_Project/Scripts/Core/    # saf C# oyun mantığı (Blockbloom.Core.asmdef): grid, pieces, scoring, generator, difficulty
Assets/_Project/Scripts/Game/    # Unity bileşenleri: girdi, sürükle-bırak, tahta görünümü
Assets/_Project/Scripts/Fx/      # efekt, ses, titreşim (juice) katmanı
Assets/_Project/Scripts/UI/      # HUD, menüler
Assets/_Project/Scenes/          # Boot, Menu, Game
Assets/_Project/Art/, Audio/     # görsel ve ses
Assets/_Project/Tests/           # Unity Test Framework (EditMode/PlayMode)
tests/Blockbloom.Core.Tests/       # Core terminal testleri (dotnet test, NUnit)
tools/                           # denge simülasyonu vb. (dotnet konsol)
docs/                            # GDD, araştırma, karar kayıtları
```

## Puanlama (başlangıç değerleri — denge testleriyle ayarlanır)

- Yerleştirilen her hücre: +1
- Patlatma: `10 × çizgi_sayısı² × kombo`
- Kombo: art arda patlatan her yerleştirmede +1, patlatmayan yerleştirmede sıfırlanır.

## Ekip ve iş akışı

Koordinatör (ana oturum) işi böler, ilgili alt ajana verir, çıktıyı kontrol eder ve birleştirir. Alt ajanlar `.claude/agents/` içinde:

| Ajan | Sorumluluk |
|---|---|
| `game-designer` | GDD, mekanik, seviye ve ilerleme tasarımı, denge |
| `game-programmer` | Kod, mimari, performans |
| `art-ui` | Görsel kimlik, UI, efekt ve ses tasarımı |
| `qa-tester` | Testler, hata avı, denge simülasyonları |
| `growth` | Pazar, reklam/ASO, monetizasyon, analitik planı |

Kurallar:
- Her alt ajan sıfırdan başlar: görev verirken ilgili dosyaları ve kabul kriterini açıkça yaz.
- Kararlar `docs/decisions.md` dosyasına tarih ve gerekçeyle eklenir.
- Core (`Assets/_Project/Scripts/Core`) değişikliği testsiz birleştirilmez; `dotnet test` geçmeden commit yok.
- Başka oyunlardan kod, görsel, isim veya ses kopyalanmaz.

## Yol haritası

1. **M0 – Çekirdek:** ızgara, üç parçalı tepsi, sürükle-bırak, patlatma, puan, oyun sonu. Core birim testleri (`dotnet test`) + Unity'de oynanabilir sahne.
2. **M1 – His:** efektler, ses, titreşim, kombo geri bildirimi. Telefonda test.
3. **M2 – İçerik:** macera modu (açılan resim), günlük bulmaca, zorluk ayarı.
4. **M3 – Gelir ve analitik:** ödüllü reklam, reklamsız satın alma, olay takibi.
5. **M4 – Yayın:** Unity iOS/Android build, mağaza sayfası, soft launch.
