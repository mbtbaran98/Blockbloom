# Geliştirici notu

## Yapı

| Yol | İçerik |
|---|---|
| `Assets/_Project/Scripts/Core/` | Saf C# oyun mantığı (`Blockbloom.Core`, `noEngineReferences: true`). GDD §9 API'si. |
| `Assets/_Project/Scripts/Core/CoreConfig.cs` | Tüm ayarlanabilir sabitler (puan, zorluk eşikleri/çarpanları). Denge ayarı buradan yapılır. |
| `tests/Blockbloom.Core.Tests/` | NUnit testleri (net8.0, C# 9). Core kaynaklarını doğrudan derler. |
| `tests/Blockbloom.Core.Compat/` | Uyumluluk denetimi: aynı Core kaynaklarını **netstandard2.1 + C# 9** ile derler (Unity 6'nın API yüzeyi). Çıktısı kullanılmaz; uyarılar hata sayılır. |
| `Assets/_Project/Scripts/Game/` | Unity katmani (`Blockbloom.Game.asmdef`): `Bootstrap` sahneyi kodla kurar, `GameApp` denetleyici, gorunumler, girdi, kayit. Kural icermez; oyun yalniz `GameState.TryPlace` ile ilerler. |
| `Assets/_Project/Tests/EditMode/` | Unity Test Framework EditMode testleri (PCG32 referans, PlayerPrefs/JsonUtility kayit gidis-donusu, palet, yerlesim). |
| `tests/Blockbloom.Game.CompileCheck/` (+ `.Legacy`) | Unity editoru olmadan Game + EditMode kodunun **yalniz derleme** kontrolu (yeni Input System ve eski Input Manager kipleri). Cozum dosyasinda degil; ayri calistirilir. |
| `tools/Blockbloom.Sim/` | Denge simülasyonu (net8.0 konsol, GDD §10). Core kaynaklarını doğrudan derler; Core'u değiştirmez. Rapor: `docs/reports/m0-balance.md`. |
| `Blockbloom.Core.sln` | Yukarıdaki projeler. Unity kökte kendi `Blockbloom.sln` dosyasını ürettiği için ad bilerek farklı. |

## Testleri çalıştırma

```bash
dotnet test tests/Blockbloom.Core.Tests     # ya da: dotnet test Blockbloom.Core.sln
```

- `dotnet test` önce `Blockbloom.Core.Compat` projesini derler. Core'a C# 10+ özelliği (file-scoped namespace, record struct, `required`, raw string…) ya da .NET Standard 2.1'de olmayan bir API (ör. `System.Numerics.BitOperations`) girerse derleme burada kırılır.
- Gereken: .NET 8 SDK. NuGet: NUnit 4, NUnit3TestAdapter, Microsoft.NET.Test.Sdk.
- Tek test: `dotnet test tests/Blockbloom.Core.Tests --filter "FullyQualifiedName~Gdd_6_4"`.
- Performans çıktısı: `--logger "console;verbosity=detailed"` ile `Ortalama tepsi uretimi` satırı görünür.

## Unity kodunun derleme kontrolü (editörsüz)

```bash
dotnet build tests/Blockbloom.Game.CompileCheck          # ENABLE_INPUT_SYSTEM
dotnet build tests/Blockbloom.Game.CompileCheck.Legacy   # ENABLE_LEGACY_INPUT_MANAGER
```

- `UnityEngine.dll` NuGet `Unity3D.SDK 2021.1.14.1` paketinden gelir (Unity 2021.1 API'si). uGUI (`Text`), Input System ve `LogAssert` icin `tests/Blockbloom.Game.CompileCheck/Stubs/` altinda elle yazilmis imza taklitleri vardir.
- Uyarilar hata sayilir. Ad cakismalari (ornegin `Blockbloom.Core.Grid` ile `UnityEngine.Grid`) burada yakalanir; Game kodunda `using Grid = Blockbloom.Core.Grid;` kullanin.
- Sinirlar: Unity 6'ya ozgu ya da Unity 6'da kaldirilmis API burada yakalanmaz; stub'lar gercek paketle birebir degildir. Son dogrulama Unity editorunde yapilir.

## Denge simülasyonu

```bash
dotnet run -c Release --project tools/Blockbloom.Sim -- --games 1000 --seed-start 1 --bot random,greedy,smart,planner --difficulty all --sweep
dotnet run -c Release --project tools/Blockbloom.Sim -- --help   # tüm seçenekler
```

- Varsayılan çıktı `docs/reports/m0-balance.json`. Çıkış kodu 1 = GDD §10'daki kesin bir kriter kaldı.
- `random`/`greedy` GDD §10 botlarıdır; `smart`/`planner` yalnız bilgi amaçlıdır (S kriterlerine girmez; `planner` yavaştır, 1.000 oyun birkaç dakika).
- `--difficulty off` ve `--sweep` ince bir döngüyle (`Grid` + `Generator.NextTray(grid, profil)` + `ScoreKeeper`) çalışır; bu döngünün zorluk açıkken `GameState` ile birebir aynı olduğu her koşuda denetlenir.

## Core kuralları

- `UnityEngine`, `System.Random`, saat/zaman okuma yok. Tüm rastgelelik `IRandom` (`Pcg32`) üzerinden.
- C# 9 sınırı; `record` kullanılmaz. Nullable açıklamaları (`string?`) kullanılmaz (Unity'de nullable bağlamı kapalı).
- Sıcak yollar (`Grid.CanPlace`, `FitsAnywhere`, `PreviewLines`, `GameState.LegalMoves(List<>)`, `SlotFitsAnywhere`) bellek ayırmaz; testle denetlenir (`PerformanceTests.HotPaths_DoNotAllocate`). LINQ bu yollarda yasak.
- `Generator`'ın RNG tüketim sırası ve `PieceCatalog` sırası/kimlikleri kalıcıdır; değişirse eski tohumlar başka oyun üretir → `docs/decisions.md` kaydı gerekir.
- Her Core değişikliği testle gelir; `dotnet test` yeşil olmadan commit yok.

## Unity'de açma ve oynama (M0)

1. **Unity 6 LTS** kurun (Unity Hub → Installs; `ProjectSettings/ProjectVersion.txt` = `6000.0.23f1`; herhangi bir 6000.0.x yamasi da olur, Hub surum farkini sorarsa "Continue/Change Editor version" ile 6000.0.x secin).
2. Unity Hub → **Add → Add project from disk** → depo kok klasorunu (`Assets/`, `Packages/` iceren) secin ve acin.
   - Ilk acilista Unity `Library/`, eksik `ProjectSettings/*.asset` dosyalarini ve `Packages/packages-lock.json`'u uretir; paketler (`com.unity.inputsystem`, `com.unity.ugui`, `com.unity.test-framework`) indirilir.
   - **Input System sorusu:** "This project is using the new input system package but the native platform backends for the new input system are not enabled... Do you want to enable the backends?" → **Yes** (editor yeniden baslar). "No" derseniz oyun eski Input Manager ile de calisir (kod iki kipte de derlenir). Player Settings → Active Input Handling: `Input System Package (New)` ya da `Both` onerilir.
   - Render: yerlesik (Built-in) renderer. URP M0'da kullanilmiyor; URP kurulumu M1'de art-ui ile yapilacak.
3. Herhangi bir sahneyi acin (bos sahne de olur: File → New Scene) ve **Play**'e basin. `Bootstrap` (`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`) kamerayi, tahtayi, tepsiyi, HUD'u ve oyun sonu panelini kodla kurar; sahnede `Main Camera` varsa onu kullanir.
   - Fare ile surukle-birak calisir. Telefon deneyimi icin Game gorunumunu **Simulator**'a alin ya da cihaza derleyin.
   - Yarim kalan oyun Play durdurulunca (`OnApplicationQuit`) / uygulama arka plana alininca (`OnApplicationPause`) PlayerPrefs'e kaydedilir ve sonraki Play'de devam eder. Sifirlamak icin: Edit → Clear All PlayerPrefs.
4. Testler: Window → General → **Test Runner** → EditMode → Run All.
5. **`.meta` dosyalari:** Unity her varlik icin `.meta` (GUID) uretir. Bunlari elle yazmayin; Unity'nin urettiklerini **commit edin** (asmdef referanslari ve ileride sahne/prefab baglantilari bu GUID'lere dayanir). `Library/`, `Temp/`, `Logs/`, `UserSettings/` ve Unity'nin urettigi kok `*.csproj`/`*.sln` commit edilmez (`.gitignore`).

## Unity'de kullanım

1. Acma adimlari icin yukaridaki bolume bakin.
2. `Assets/_Project/Scripts/Core/Blockbloom.Core.asmdef` sayesinde Core ayrı bir assembly olarak otomatik derlenir.
3. `autoReferenced: true` olduğu için varsayılan `Assembly-CSharp` Core'u görür. Oyun kodu kendi asmdef'ini kullanacaksa (`Blockbloom.Game` vb.) `references` listesine `Blockbloom.Core` eklenmelidir.
4. Unity tarafından tipik kullanım:

```csharp
var game = GameState.NewGame((ulong)System.DateTime.UtcNow.Ticks); // tohum Unity katmanında üretilir
bool fits = game.SlotFitsAnywhere(slot);                             // soluk gösterim
LinePreview pv = game.Grid.PreviewLines(game.Tray[slot].Value.Def, ax, ay); // sürüklerken, çapa değişince
MoveResult r = game.TryPlace(slot, ax, ay);                          // bırakınca
```

5. Kayıt: `PlayerPrefsRecordStore` (`IRecordStore`). Yarıda kalan oyun: `SnapshotStore` → `GameState.ToSnapshot()` + `oyunBaşıRekor` (GDD §7.2) → `SavedGameDto` (ulong'lar ondalık dizgi, renkler onaltılık dizgi, boş yuva `""`) → `JsonUtility` → PlayerPrefs. Okunamayan/tutarsız kayıt silinir ve yeni oyun başlar (`FromSnapshot` tutarsız durumu `ArgumentException` ile reddeder).
6. Not: `GameSnapshot` alanları public field'dır; `System.Text.Json` ile serileştirmede `IncludeFields = true` gerekir (aksi halde `{}` yazar).

## Unity'de yerelde doğrulanması gerekenler

- Projenin Unity 6000.0'da hatasız derlenmesi, paket sürümlerinin çözülmesi, asmdef'lerin tanınması (bulut ortamında Unity yok; yalnız `tests/Blockbloom.Game.CompileCheck*` derleme kontrolü yapıldı).
- EditMode testleri: `SnapshotStoreTests` (gerçek `JsonUtility` + PlayerPrefs), `Pcg32ReferenceTests`, `PaletteAndLayoutTests`.
- IL2CPP (iOS/Android) derlemesinde PCG32 referans değerleri (aynı test cihazda/PlayMode'da).
- Oynanış: GDD §8 tablosu (dokunma alanı, 80 ms kaldırma, 1,2 hücre ofset, 0,75 hücre çapa eşiği, %45 önizleme, çizgi vurgusu, 150 ms dönüş, %40 soluk parça, tek parmak), GDD §7.1 oyun sonu akışı, portre/yatay ve çentikli ekranda yerleşim.
- Arka plana alma (`OnApplicationPause`) → rekor yazımı ve kaldığı yerden devam.
