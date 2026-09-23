#nullable enable
using UnityEditor;

namespace CardGame.Unity.Editor
{
    /// <summary>
    /// Assets/Resources/ 配下の画像素材を自動設定する(エディタ GUI で 1 枚ずつ設定しなくて済むように)。
    /// 解像度は「画面に出る最大の大きさ」に合わせて絞る(2026-09-23 の容量削減):
    /// - CardArt/   カードイラスト 512×1024 → 256×512。拡大表示でも絵の窓は横 220px 程度
    /// - Leaders/   リーダーの肖像 512×512 → 256×256(表示は 104px)
    /// - HsParts/   カードの部品。台紙(body_*)は不透明なので DXT1、ほかは透過の DXT5。どちらも crunch、最大 512
    /// - Field/     机の背景 2048×1024
    /// - Emblems/ Keywords/  紋章・アイコン(透過、元から小さい)
    /// DXT / crunch は縦横が 4 の倍数でないと効かない(無圧縮になって重くなる)ので、素材は 4 の倍数の大きさで作る。
    /// </summary>
    public sealed class CardArtImporter : AssetPostprocessor
    {
        // 設定を変えたらこの番号を上げる(該当画像が再インポートされる)
        public override uint GetVersion() => 12;

        private void OnPreprocessTexture()
        {
            var path = assetPath.Replace('\\', '/');
            bool cardArt = path.Contains("/Resources/CardArt/");
            bool leader = path.Contains("/Resources/Leaders/");
            bool field = path.Contains("/Resources/Field/");
            bool parts = path.Contains("/Resources/HsParts/");
            bool body = parts && path.Contains("/body_");
            bool icon = path.Contains("/Resources/Emblems/") || path.Contains("/Resources/Keywords/");
            if (!cardArt && !leader && !field && !parts && !icon) return;

            bool transparent = icon || (parts && !body);
            bool crunch = !icon;
            int maxSize = field ? 2048 : leader ? 256 : icon ? 256 : 512;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;   // 2 のべき乗でない画像にミップマップを付けると DXT が効かず無圧縮になる(2026-09-23 に判明)
            importer.npotScale = TextureImporterNPOTScale.None;   // Sprite は丸められない。素材側を 4 の倍数の大きさで作っておく(DXT / crunch の条件)
            importer.alphaIsTransparency = transparent;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = crunch;
            importer.compressionQuality = 60;
            importer.sRGBTexture = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;

            // 既定設定だけでは WebGL で圧縮されないケースがあったため、主要プラットフォームに明示的に指定する
            var format = transparent
                ? (crunch ? TextureImporterFormat.DXT5Crunched : TextureImporterFormat.DXT5)
                : TextureImporterFormat.DXT1Crunched;
            foreach (var platform in new[] { "WebGL", "Android", "iPhone", "Standalone" })
            {
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform,
                    overridden = true,
                    maxTextureSize = maxSize,
                    format = format,
                    compressionQuality = 60,
                    textureCompression = TextureImporterCompression.Compressed,
                    crunchedCompression = crunch,
                });
            }
        }
    }
}
