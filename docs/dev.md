# Geliştirici notu

## Yapı

| Yol | İçerik |
|---|---|
| `Assets/_Project/Scripts/Core/` | Saf C# oyun mantığı (`Blockbloom.Core`, `noEngineReferences: true`). GDD §9 API'si. |
| `Assets/_Project/Scripts/Core/CoreConfig.cs` | Tüm ayarlanabilir sabitler (puan, zorluk eşikleri/çarpanları). Denge ayarı buradan yapılır. |
| `tests/Blockbloom.Core.Tests/` | NUnit testleri (net8.0, C# 9). Core kaynaklarını doğrudan derler. |
| `tests/Blockbloom.Core.Compat/` | Uyumluluk denetimi: aynı Core kaynaklarını **netstandard2.1 + C# 9** ile derler (Unity 6'nın API yüzeyi). Çıktısı kullanılmaz; uyarılar hata sayılır. |
| `Blockbloom.Core.sln` | Yukarıdaki iki proje (ileride `tools/` projeleri de buraya eklenir). Unity kökte kendi `Blockbloom.sln` dosyasını ürettiği için ad bilerek farklı. |

## Testleri çalıştırma

```bash
dotnet test tests/Blockbloom.Core.Tests     # ya da: dotnet test Blockbloom.Core.sln
```

- `dotnet test` önce `Blockbloom.Core.Compat` projesini derler. Core'a C# 10+ özelliği (file-scoped namespace, record struct, `required`, raw string…) ya da .NET Standard 2.1'de olmayan bir API (ör. `System.Numerics.BitOperations`) girerse derleme burada kırılır.
- Gereken: .NET 8 SDK. NuGet: NUnit 4, NUnit3TestAdapter, Microsoft.NET.Test.Sdk.
- Tek test: `dotnet test tests/Blockbloom.Core.Tests --filter "FullyQualifiedName~Gdd_6_4"`.
- Performans çıktısı: `--logger "console;verbosity=detailed"` ile `Ortalama tepsi uretimi` satırı görünür.

## Core kuralları

- `UnityEngine`, `System.Random`, saat/zaman okuma yok. Tüm rastgelelik `IRandom` (`Pcg32`) üzerinden.
- C# 9 sınırı; `record` kullanılmaz. Nullable açıklamaları (`string?`) kullanılmaz (Unity'de nullable bağlamı kapalı).
- Sıcak yollar (`Grid.CanPlace`, `FitsAnywhere`, `PreviewLines`, `GameState.LegalMoves(List<>)`, `SlotFitsAnywhere`) bellek ayırmaz; testle denetlenir (`PerformanceTests.HotPaths_DoNotAllocate`). LINQ bu yollarda yasak.
- `Generator`'ın RNG tüketim sırası ve `PieceCatalog` sırası/kimlikleri kalıcıdır; değişirse eski tohumlar başka oyun üretir → `docs/decisions.md` kaydı gerekir.
- Her Core değişikliği testle gelir; `dotnet test` yeşil olmadan commit yok.

## Unity'de kullanım

1. Unity 6 LTS (6000.0.x) ile depo kökünü proje olarak açın (2D URP şablonu; `Assets/`, `Packages/`, `ProjectSettings/` Unity tarafından oluşturulur/tamamlanır).
2. `Assets/_Project/Scripts/Core/Blockbloom.Core.asmdef` sayesinde Core ayrı bir assembly olarak otomatik derlenir. `.meta` dosyaları depoda yok; Unity ilk açılışta üretir — bunlar commit edilmelidir.
3. `autoReferenced: true` olduğu için varsayılan `Assembly-CSharp` Core'u görür. Oyun kodu kendi asmdef'ini kullanacaksa (`Blockbloom.Game` vb.) `references` listesine `Blockbloom.Core` eklenmelidir.
4. Unity tarafından tipik kullanım:

```csharp
var game = GameState.NewGame((ulong)System.DateTime.UtcNow.Ticks); // tohum Unity katmanında üretilir
bool fits = game.SlotFitsAnywhere(slot);                             // soluk gösterim
LinePreview pv = game.Grid.PreviewLines(game.Tray[slot].Value.Def, ax, ay); // sürüklerken, çapa değişince
MoveResult r = game.TryPlace(slot, ax, ay);                          // bırakınca
```

5. Kayıt: `IRecordStore`'u Unity katmanında uygulayın (ör. `PlayerPrefs`). Yarıda kalan oyun için `GameState.ToSnapshot()` → JSON (`GameSnapshot` alanları public, `[Serializable]`) → `GameState.FromSnapshot()`.

## Unity'de yerelde doğrulanması gerekenler

- Projenin Unity 6'da hatasız derlenmesi ve asmdef'in tanınması (bulut ortamında Unity yok).
- `GameSnapshot`'ın `JsonUtility` ile gidiş-dönüşü (`ulong` alanlar ve `string[]` içindeki `null` yuvalar; `FromSnapshot` `""` değerini de boş yuva sayar). Terminal testi yalnız `System.Text.Json` ile yapıldı.
- IL2CPP (iOS/Android) derlemesinde Core'un davranışı: PCG32 referans değerleri testi Unity Test Framework'e de taşınabilir.
