# Claude Code'a yazılacak ilk mesaj

Bu klasörü proje kökü yaptıktan sonra Claude Code'da şunu yaz:

---

Sen bu projenin koordinatörüsün. Önce CLAUDE.md ve docs/research.md dosyalarını oku, sonra .claude/agents altındaki ekibi tanı.

İlk hedefimiz M0 (çekirdek oyun). Şöyle ilerle:

1. game-designer ajanına docs/GDD.md dosyasının ilk sürümünü yazdır: kurallar, parça seti, parça üretme algoritması (her tepside en az bir parça yerleşebilmeli), puanlama, oyun sonu.
2. GDD'yi bana özetle ve onayımı bekle.
3. Onaydan sonra game-programmer ajanına Unity projesini kurdur (Unity 6 LTS iskeleti, Blockbloom.Core assembly'si, dotnet NUnit test projesi) ve Core modüllerini testleriyle yazdır.
4. qa-tester ajanına testleri çalıştırtıp otomatik oyuncuyla 1.000 oyunluk denge simülasyonu yaptır.
5. Sonunda bana: ne yapıldı, test sonuçları, Unity'de nasıl denerim ve açık sorular.

Kararları docs/decisions.md dosyasına kaydet.
