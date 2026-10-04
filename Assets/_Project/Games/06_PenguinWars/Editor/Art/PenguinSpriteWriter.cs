using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>ピクセル配列を PNG に書き出し、ドット絵用の設定で Sprite として取り込む（キャラ・戦場の生成で共通）</summary>
    public static class PenguinSpriteWriter
    {
        public const string SpriteDirectory = "Assets/_Project/Games/06_PenguinWars/Sprites";

        /// <summary>1ワールド単位あたりのピクセル数。基本ペンギン（16x19ドット）が約1x1.2単位になる</summary>
        public const int PixelsPerUnit = 16;

        public static Sprite Load(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        /// <param name="pixels">左下から右へ、下の行から上の行へ並んだ色（Texture2D.SetPixels32 と同じ順）</param>
        /// <param name="pixelsPerUnit">大型キャラは小さくして、同じドット数のまま大きく見せる</param>
        public static Sprite Save(string assetPath, Color32[] pixels, int width, int height, float pixelsPerUnit, Vector2 pivot)
        {
            EnsureDirectory(Path.GetDirectoryName(assetPath));

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(assetPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(assetPath, pixelsPerUnit, pivot);
            return Load(assetPath);
        }

        private static void ConfigureImporter(string assetPath, float pixelsPerUnit, Vector2 pivot)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point; // ドット絵をぼかさない
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // 地面・山を Tiled で敷き詰めるのに必要
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        /// <summary>ディスクに作っただけだと ImportAsset が親フォルダを見つけられないことがあるので取り込み直す</summary>
        private static void EnsureDirectory(string path)
        {
            if (Directory.Exists(path)) return;

            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}
