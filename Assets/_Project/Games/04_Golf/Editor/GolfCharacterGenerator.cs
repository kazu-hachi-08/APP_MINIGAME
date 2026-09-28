using System.IO;
using UnityEditor;
using UnityEngine;

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// キャラのデータアセット・カタログ・正面の立ち絵を生成するエディタユーティリティ。
    /// 既にあるアセットの数値は上書きしない。Inspector で調整した倍率や色を消さないため。
    /// 立ち絵はアセットの帽子・髪の色から毎回描き直すので、色を調整したらメニューを実行し直せば絵も揃う。
    /// </summary>
    public static class GolfCharacterGenerator
    {
        private const string DataDirectory = "Assets/_Project/Games/04_Golf/Data/Characters";
        private const string CatalogPath = DataDirectory + "/GolfCharacterCatalog.asset";
        private const string SpriteDirectory = "Assets/_Project/Games/04_Golf/Sprites/Characters";

        /// <summary>先頭のバランス型が初期選択になる。生成後はアセットの数値を直接調整する</summary>
        private static readonly (string Id, string Name, float Distance, float Straightness, Color Cap, Color Hair)[] Defaults =
        {
            ("Balance", "バランス型", 1f, 1f, new Color(0.95f, 0.95f, 0.95f), new Color(0.1f, 0.1f, 0.1f)),
            ("Power", "パワー型", 1.15f, 0.75f, new Color(0.85f, 0.15f, 0.15f), new Color(0.4f, 0.25f, 0.12f)),
            ("Technique", "テクニック型", 0.9f, 1.35f, new Color(0.12f, 0.18f, 0.4f), new Color(0.9f, 0.75f, 0.3f)),
        };

        // 立ち絵は 16×24 ピクセルのドット絵（モルックのキャラと同じ大きさ）
        private const int SpriteWidth = 16;
        private const int SpriteHeight = 24;
        private const int PixelsPerUnit = 32;
        private static readonly Color32 Skin = new Color32(246, 206, 168, 255);
        private static readonly Color32 Shirt = new Color32(235, 235, 240, 255);
        private static readonly Color32 Pants = new Color32(60, 64, 88, 255);
        private static readonly Color32 Shoes = new Color32(40, 34, 30, 255);
        private static readonly Color32 Outline = new Color32(34, 30, 38, 255);

        /// <summary>データを確保したうえで立ち絵を描き直す（Rebuild Golf から呼ばれる）</summary>
        public static void Generate()
        {
            GolfCharacterCatalog catalog = EnsureGenerated();
            for (int i = 0; i < catalog.Count; i++)
            {
                SaveFrontSprite(catalog.Get(i));
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GolfCharacterGenerator] キャラのデータを確認しました: {DataDirectory}");
        }

        /// <summary>カタログと各キャラが無ければ作り、カタログを返す（シーンビルダーからも呼べるようにする）</summary>
        public static GolfCharacterCatalog EnsureGenerated()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GolfCharacterCatalog>(CatalogPath);
            if (existing != null) return existing;

            Directory.CreateDirectory(DataDirectory);

            var characters = new GolfCharacterData[Defaults.Length];
            for (int i = 0; i < Defaults.Length; i++)
            {
                characters[i] = EnsureCharacter(Defaults[i]);
                if (characters[i].FrontSprite == null) SaveFrontSprite(characters[i]);
            }

            var catalog = ScriptableObject.CreateInstance<GolfCharacterCatalog>();
            GolfSceneBuilder.SetArray(catalog, "_characters", characters);

            AssetDatabase.CreateAsset(catalog, CatalogPath);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static GolfCharacterData EnsureCharacter(
            (string Id, string Name, float Distance, float Straightness, Color Cap, Color Hair) values)
        {
            string path = $"{DataDirectory}/GolfChar_{values.Id}.asset";
            var character = AssetDatabase.LoadAssetAtPath<GolfCharacterData>(path);
            if (character != null) return character;

            character = ScriptableObject.CreateInstance<GolfCharacterData>();
            var so = new SerializedObject(character);
            so.FindProperty("_displayName").stringValue = values.Name;
            so.FindProperty("_capColor").colorValue = values.Cap;
            so.FindProperty("_hairColor").colorValue = values.Hair;
            so.FindProperty("_distanceMultiplier").floatValue = values.Distance;
            so.FindProperty("_straightnessMultiplier").floatValue = values.Straightness;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(character, path);
            return character;
        }

        /// <summary>アセットの帽子・髪の色で立ち絵を描き、PNG に保存してアセットに設定する</summary>
        private static void SaveFrontSprite(GolfCharacterData character)
        {
            Directory.CreateDirectory(SpriteDirectory);
            string path = $"{SpriteDirectory}/{character.name}_Front.png";

            var texture = new Texture2D(SpriteWidth, SpriteHeight, TextureFormat.RGBA32, false);
            texture.SetPixels32(DrawFront(character.CapColor, character.HairColor));
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path);

            var so = new SerializedObject(character);
            so.FindProperty("_frontSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(character);
        }

        /// <summary>
        /// 正面を向いたゴルファー。キャラの違いは帽子と髪の色だけで見せる（背後視点の GolferRig と揃えるため）。
        /// シャツは席の色にしたいので白っぽくしておき、選択画面側で色を掛けられるようにする
        /// </summary>
        private static Color32[] DrawFront(Color cap, Color hair)
        {
            var pixels = new Color32[SpriteWidth * SpriteHeight];
            const int center = SpriteWidth / 2;

            // 脚と靴
            FillRect(pixels, center - 3, 17, 2, 6, Pants);
            FillRect(pixels, center + 1, 17, 2, 6, Pants);
            FillRect(pixels, center - 3, 22, 2, 1, Shoes);
            FillRect(pixels, center + 1, 22, 2, 1, Shoes);

            // 腕と胴
            FillRect(pixels, center - 5, 10, 1, 6, Skin);
            FillRect(pixels, center + 4, 10, 1, 6, Skin);
            FillRect(pixels, center - 4, 9, 8, 8, Shirt);

            // 顔の横に髪を見せ、その上に帽子とつばを重ねる
            FillRect(pixels, center - 4, 3, 8, 5, hair);
            FillRect(pixels, center - 3, 4, 6, 5, Skin);
            FillRect(pixels, center - 4, 1, 8, 3, cap);
            FillRect(pixels, center - 5, 3, 10, 1, cap);
            SetPixel(pixels, center - 2, 6, Outline);
            SetPixel(pixels, center + 1, 6, Outline);

            AddOutline(pixels);
            return pixels;
        }

        /// <summary>塗った部分を1ピクセルの縁で囲む。白い帽子やシャツが明るい背景に埋もれないようにする</summary>
        private static void AddOutline(Color32[] pixels)
        {
            var source = (Color32[])pixels.Clone();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (source[i].a != 0) continue;

                int x = i % SpriteWidth;
                int y = i / SpriteWidth;
                if (IsFilled(source, x - 1, y) || IsFilled(source, x + 1, y) ||
                    IsFilled(source, x, y - 1) || IsFilled(source, x, y + 1))
                {
                    pixels[i] = Outline;
                }
            }
        }

        private static bool IsFilled(Color32[] pixels, int x, int y)
        {
            if (x < 0 || x >= SpriteWidth || y < 0 || y >= SpriteHeight) return false;

            return pixels[y * SpriteWidth + x].a != 0;
        }

        private static void FillRect(Color32[] pixels, int x0, int y0, int w, int h, Color32 color)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    SetPixel(pixels, x, y, color);
                }
            }
        }

        /// <summary>x,y は左上原点。Texture2D は左下原点なので上下を反転して書く</summary>
        private static void SetPixel(Color32[] pixels, int x, int y, Color32 color)
        {
            if (x < 0 || x >= SpriteWidth || y < 0 || y >= SpriteHeight) return;

            pixels[(SpriteHeight - 1 - y) * SpriteWidth + x] = color;
        }

        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point; // ドット絵をぼかさない
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }
}
