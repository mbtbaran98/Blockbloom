# Çini Blok — Proje Rehberi

Bu dosya projenin ortak hafızasıdır. Ana oturum (koordinatör) ve tüm alt ajanlar işe başlamadan önce bunu okur. Karar değiştiğinde önce bu dosya güncellenir.

## Vizyon

8×8 ızgaralı, üç parçalı, satır/sütun patlatmalı bir blok bulmaca oyunu. Hedef: türün en "tatmin edici" hissettiren oyunu olmak. Çalışma adı **Çini Blok**; görsel kimlik İznik çinisinden ilham alır (kobalt, turkuaz, mercan kırmızısı, safran, yeşil, mor). Başka bir oyunun adı, logosu, görselleri veya marka unsurları kullanılmaz.

## Tasarım ilkeleri (araştırmadan türetildi — kaynaklar: `docs/research.md`)

1. **Basit çekirdek, derin planlama.** Aynı anda üç parça; zaman sınırı yok; ceza yok; çevrimdışı oynanır.
2. **Hamleye bağlı, kademeli geri bildirim.** Ses + titreşim + görsel efekt; tek satır < çoklu satır < kombo şeklinde büyür. Rastgele süs efekti yok (Kao ve ark. 2024: hamleden bağımsız büyütme motivasyonu düşürdü).
3. **Titreşim görüntüyle tam senkron.** Az da olsa titreşim deneyimi belirgin iyileştirir (Waterloo, CHI 2021).
4. **Merak motoru.** Açılan resimli macera modu, haftalık içerik, günlük bulmaca. Merak oyun süresinin en güçlü öngörücüsü.
5. **Görünmez zorluk ayarı.** Zorlanan oyuncuya yerleşebilir parça olasılığı artar; her tepside en az bir parça mutlaka yerleşebilir (Ascarza ve ark. 2025: elde tutmayı artırdı, harcamayı düşürmedi).
6. **Saygılı reklam.** Ödüllü reklam öncelikli; oyun ortasında ve ödül ekranında geçiş reklamı yok (Deloitte & AdMob 2025: ödül ekranındaki rahatsız edici reklam bırakmayı ~3 kat artırıyor).
7. **Dürüst pazarlama.** Reklamlar gerçek oynanışı gösterir. Yanıltıcı reklam en hızlı kopan oyuncuyu getirir.
8. **Hafif ve hızlı.** Düşük donanımlı cihazda 60 fps, küçük indirme boyutu.

## Teknik yığın (varsayılan — değiştirmek için bu bölümü güncelle)

- **TypeScript + Phaser 3** (oyun), **Vite** (derleme), **Capacitor** (iOS/Android paketleme).
- Neden: tamamen terminalden derlenir, test edilir ve tarayıcıda denenir; editör arayüzü gerektirmez. Alternatifler: Godot 4 (GDScript), Unity (C#).
- Oyun mantığı (ızgara, yerleştirme, puan, parça üretimi) **saf TypeScript modüllerinde**, Phaser'dan bağımsız ve birim testli (Vitest).

## Klasör yapısı (hedef)

```
src/core/      # saf oyun mantığı: grid, pieces, scoring, generator, difficulty
src/scenes/    # Phaser sahneleri: Boot, Menu, Game, GameOver
src/fx/        # efekt, ses, titreşim (juice) katmanı
src/ui/        # HUD, menüler
assets/        # görsel ve ses
tests/         # Vitest birim testleri
docs/          # GDD, araştırma, karar kayıtları
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
- `src/core` değişikliği testsiz birleştirilmez.
- Başka oyunlardan kod, görsel, isim veya ses kopyalanmaz.

## Yol haritası

1. **M0 – Çekirdek:** ızgara, üç parçalı tepsi, sürükle-bırak, patlatma, puan, oyun sonu. Birim testleri.
2. **M1 – His:** efektler, ses, titreşim, kombo geri bildirimi. Telefonda test.
3. **M2 – İçerik:** macera modu (açılan resim), günlük bulmaca, zorluk ayarı.
4. **M3 – Gelir ve analitik:** ödüllü reklam, reklamsız satın alma, olay takibi.
5. **M4 – Yayın:** Capacitor build, mağaza sayfası, soft launch.
