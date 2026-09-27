using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// 地面タイル・ホール1のプレハブ・GolfHoleData・GolfHoleCatalog を作る（§9、Phase 2）。
    /// どれも「無ければ作る」だけにして、Tilemap を塗り替えたホールや調整済みの値を上書きしない。
    /// </summary>
    public static class GolfHoleAssetBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/04_Golf";
        private const string TileDirectory = RootDirectory + "/Tiles";
        private const string TileSpritePath = TileDirectory + "/TileSquare.png";
        private const string HoleDirectory = RootDirectory + "/Holes/Hole_01";
        private const string HolePrefabPath = HoleDirectory + "/Hole_01.prefab";
        private const string HoleDataPath = HoleDirectory + "/Hole_01.asset";
        private const string CatalogPath = RootDirectory + "/Data/GolfHoleCatalog.asset";

        // タイル1枚の画像。単色なので小さくてよく、1枚＝1ユニットにする
        private const int TileSpriteSize = 4;

        private const int GroundSortingOrder = 0;
        private const int CupSortingOrder = 5;
        private const int FlagSortingOrder = 15;

        // §5 の色。ティーはフェアウェイと区別できるよう少し明るくする
        private static readonly Dictionary<GroundType, Color> TileColors = new Dictionary<GroundType, Color>
        {
            { GroundType.Tee, new Color(0.62f, 0.88f, 0.5f) },
            { GroundType.Fairway, new Color(0.47f, 0.78f, 0.37f) },
            { GroundType.Rough, new Color(0.24f, 0.52f, 0.22f) },
            { GroundType.Bunker, new Color(0.9f, 0.82f, 0.56f) },
            { GroundType.Green, new Color(0.74f, 0.92f, 0.52f) },
        };

        // ホール1（§9.5：パー3・まっすぐ・グリーン手前にバンカー）。横16×縦32タイル
        private const string Hole01Name = "ホール1";
        private const int Hole01Par = 3;
        private const int Hole01MinX = -8;
        private const int Hole01MaxX = 7;
        private const int Hole01Height = 32;
        private static readonly Vector2 Hole01Tee = new Vector2(0f, 2f);
        private static readonly Vector2 Hole01Cup = new Vector2(0f, 26f);
        private const float Hole01GreenRadius = 4f;
        private static readonly RectInt Hole01TeeBox = new RectInt(-2, 0, 4, 4);
        private static readonly RectInt Hole01Fairway = new RectInt(-4, 4, 8, 16);
        private static readonly RectInt Hole01Bunker = new RectInt(-3, 20, 6, 2);

        private const float CupDiameter = 0.5f;
        private static readonly Vector2 FlagSize = new Vector2(0.4f, 0.28f);
        private static readonly Vector2 FlagOffset = new Vector2(0.25f, 0.5f);
        private static readonly Color FlagColor = new Color(0.9f, 0.15f, 0.15f);

        /// <summary>ホールまわりのアセットを揃えて、カタログを返す</summary>
        public static GolfHoleCatalog EnsureAssets()
        {
            Dictionary<GroundType, GolfTerrainTile> tiles = EnsureTiles();
            HoleCourse hole01Prefab = EnsureHole01Prefab(tiles);
            GolfHoleData hole01 = EnsureHoleData(hole01Prefab);
            return EnsureCatalog(hole01);
        }

        private static Dictionary<GroundType, GolfTerrainTile> EnsureTiles()
        {
            Sprite sprite = EnsureTileSprite();
            var tiles = new Dictionary<GroundType, GolfTerrainTile>();

            foreach (KeyValuePair<GroundType, Color> pair in TileColors)
            {
                string path = $"{TileDirectory}/Tile_{pair.Key}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<GolfTerrainTile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<GolfTerrainTile>();
                    tile.sprite = sprite;
                    tile.color = pair.Value;
                    AssetDatabase.CreateAsset(tile, path);
                    SetInt(tile, "_groundType", (int)pair.Key);
                }

                tiles[pair.Key] = tile;
            }

            AssetDatabase.SaveAssets();
            return tiles;
        }

        /// <summary>タイルは色で塗り分けるので、白い正方形の画像を1枚だけ用意する</summary>
        private static Sprite EnsureTileSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
            if (sprite != null) return sprite;

            EnsureDirectory(TileDirectory);
            var texture = new Texture2D(TileSpriteSize, TileSpriteSize, TextureFormat.RGBA32, false);
            var pixels = new Color[TileSpriteSize * TileSpriteSize];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(TileSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(TileSpritePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TileSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = TileSpriteSize;
            // タイル同士の境目がにじまないようにする
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(TileSpritePath);
        }

        private static HoleCourse EnsureHole01Prefab(Dictionary<GroundType, GolfTerrainTile> tiles)
        {
            var existing = AssetDatabase.LoadAssetAtPath<HoleCourse>(HolePrefabPath);
            if (existing != null) return existing;

            EnsureDirectory(HoleDirectory);

            var root = new GameObject("Hole_01");
            var course = root.AddComponent<HoleCourse>();

            var gridObj = new GameObject("Grid", typeof(Grid));
            gridObj.transform.SetParent(root.transform);

            var terrainObj = new GameObject("Terrain", typeof(Tilemap), typeof(TilemapRenderer));
            terrainObj.transform.SetParent(gridObj.transform);
            terrainObj.GetComponent<TilemapRenderer>().sortingOrder = GroundSortingOrder;
            var terrain = terrainObj.GetComponent<Tilemap>();
            PaintHole01(terrain, tiles);

            var tee = new GameObject("Tee").transform;
            tee.SetParent(root.transform);
            tee.localPosition = Hole01Tee;

            Transform cup = CreateCup(root.transform, Hole01Cup);

            GolfSceneBuilder.SetRefs(course, ("_terrain", terrain), ("_tee", tee), ("_cup", cup));

            PrefabUtility.SaveAsPrefabAsset(root, HolePrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<HoleCourse>(HolePrefabPath);
        }

        private static void PaintHole01(Tilemap terrain, Dictionary<GroundType, GolfTerrainTile> tiles)
        {
            for (int y = 0; y < Hole01Height; y++)
            {
                for (int x = Hole01MinX; x <= Hole01MaxX; x++)
                {
                    terrain.SetTile(new Vector3Int(x, y, 0), tiles[Hole01GroundAt(x, y)]);
                }
            }
        }

        /// <summary>初期の塗り分け。以降の形の調整はプレハブの Tilemap を直接塗って行う</summary>
        private static GroundType Hole01GroundAt(int x, int y)
        {
            var cell = new Vector2Int(x, y);
            Vector2 cellCenter = new Vector2(x + 0.5f, y + 0.5f);

            if (Vector2.Distance(cellCenter, Hole01Cup) <= Hole01GreenRadius) return GroundType.Green;
            if (Hole01Bunker.Contains(cell)) return GroundType.Bunker;
            if (Hole01TeeBox.Contains(cell)) return GroundType.Tee;
            if (Hole01Fairway.Contains(cell)) return GroundType.Fairway;
            return GroundType.Rough;
        }

        private static Transform CreateCup(Transform parent, Vector2 position)
        {
            var cupObj = new GameObject("Cup");
            cupObj.transform.SetParent(parent);
            cupObj.transform.localPosition = position;

            var hole = new GameObject("Hole").AddComponent<SpriteRenderer>();
            hole.transform.SetParent(cupObj.transform, false);
            hole.transform.localScale = Vector3.one * CupDiameter;
            hole.color = Color.black;
            hole.sortingOrder = CupSortingOrder;

            var flag = new GameObject("Flag").AddComponent<SpriteRenderer>();
            flag.transform.SetParent(cupObj.transform, false);
            flag.transform.localPosition = FlagOffset;
            flag.transform.localScale = new Vector3(FlagSize.x, FlagSize.y, 1f);
            flag.color = FlagColor;
            flag.sortingOrder = FlagSortingOrder;

            GolfSceneBuilder.SetRefs(cupObj.AddComponent<CupView>(), ("_hole", hole), ("_flag", flag));
            return cupObj.transform;
        }

        private static GolfHoleData EnsureHoleData(HoleCourse prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<GolfHoleData>(HoleDataPath);
            if (data != null) return data;

            data = ScriptableObject.CreateInstance<GolfHoleData>();
            AssetDatabase.CreateAsset(data, HoleDataPath);

            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = Hole01Name;
            so.FindProperty("_par").intValue = Hole01Par;
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            return data;
        }

        private static GolfHoleCatalog EnsureCatalog(GolfHoleData hole)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GolfHoleCatalog>(CatalogPath);
            if (catalog == null)
            {
                EnsureDirectory(Path.GetDirectoryName(CatalogPath));
                catalog = ScriptableObject.CreateInstance<GolfHoleCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            foreach (GolfHoleData registered in catalog.Holes)
            {
                if (registered == hole) return catalog;
            }

            var so = new SerializedObject(catalog);
            SerializedProperty holes = so.FindProperty("_holes");
            holes.InsertArrayElementAtIndex(holes.arraySize);
            holes.GetArrayElementAtIndex(holes.arraySize - 1).objectReferenceValue = hole;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static void SetInt(Object target, string property, int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(property).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureDirectory(string directory)
        {
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }
    }
}
