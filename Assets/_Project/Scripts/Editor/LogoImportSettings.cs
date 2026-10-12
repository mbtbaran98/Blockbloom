using UnityEditor;

namespace Blockbloom.EditorTools
{
    /// <summary>
    /// Logo icin iceri aktarma ayari (elle .meta yazmak yerine kodla). Unity'nin "Default" doku turu varsayilan olarak
    /// iki-kuvveti olmayan dokulari en yakin 2^n boyuta olcekler (1024x572 -> 1024x512) ve logonun en-boy orani bozulur.
    /// Burada yalniz olcekleme kapatilir; doku turu (Default ya da Sprite) kullanicinin secimine birakilir:
    /// MainMenuView logoyu her iki turde de Texture2D olarak yukleyip Sprite'i kodla olusturur.
    /// GetVersion degisirse Unity etkilenen varliklari yeniden iceri aktarir.
    /// </summary>
    public sealed class LogoImportSettings : AssetPostprocessor
    {
        public const string LogoAssetPath = "Assets/_Project/Resources/Branding/logo.png";

        public override uint GetVersion() => 1;

        private void OnPreprocessTexture()
        {
            if (assetPath != LogoAssetPath) return;
            var importer = (TextureImporter)assetImporter;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
