# Karar Kayıtları

Her karar: tarih, karar, gerekçe, sonuçlar. En yenisi en altta.

## 2026-10-12 — Motor: Unity (Phaser yerine)

- **Karar:** Oyun Unity 6 LTS + C# ile yapılacak. Başlangıçtaki varsayılan TypeScript + Phaser 3 + Capacitor yığını bırakıldı.
- **Gerekçe:** Proje sahibinin tercihi. iOS/Android derlemesi, dokunmatik girdi, titreşim ve reklam SDK entegrasyonları Unity'de yerleşik ve yaygın.
- **Sonuçlar:**
  - Oyun mantığı Unity'den bağımsız saf C# (`CiniBlok.Core`, `noEngineReferences: true`, C# 9) olarak yazılır ve `dotnet test` ile terminalden test edilir. Böylece "terminalden test edilebilir çekirdek" ilkesi korunur.
  - Bulut oturumunda Unity editörü yok; sahne, prefab, derleme ve cihaz testi yerelde yapılır. Kodla kurulum tercih edilir.
  - Tarayıcıda deneme ancak WebGL derlemesiyle mümkün; günlük deneme Unity editöründe Play ile yapılır.
