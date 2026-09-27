using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace MiniGame.Golf.Editor
{
    /// <summary>
    /// 地面タイル・ホールのプレハブ・GolfHoleData・GolfHoleCatalog を作る（§9、Phase 2・4・10）。
    /// どれも「無ければ作る」だけにして、Tilemap を塗り替えたホールや調整済みの値を上書きしない。
    /// 例外はタイルの見た目で、画像（GolfTileArtBuilder）に毎回揃える。
    /// </summary>
    public static class GolfHoleAssetBuilder
    {
        private const string RootDirectory = "Assets/_Project/Games/04_Golf";
        private const string TileDirectory = RootDirectory + "/Tiles";
        private const string HoleRootDirectory = RootDirectory + "/Holes";
        private const string CatalogPath = RootDirectory + "/Data/GolfHoleCatalog.asset";

        private const int GroundSortingOrder = 0;
        private const int CupSortingOrder = 5;
        private const int FlagSortingOrder = 15;

        private const float CupDiameter = 0.5f;

        // 横は §19 の目安どおり16タイル（x = -8〜7）。縦はパーに合わせて伸ばす
        private const int HoleMinX = -8;
        private const int HoleMaxX = 7;

        /// <summary>1ホール分の初期の形。以降の形の調整はプレハブの Tilemap を直接塗って行う</summary>
        private sealed class HoleSpec
        {
            public string Id;
            public string DisplayName;
            public int Par;
            public int Height;
            public Vector2 Tee;
            public Vector2 Cup;
            public float MinWind;
            public float MaxWind;
            public Func<Vector2Int, GroundType> GroundAt;

            public string FolderPath => $"{HoleRootDirectory}/{Id}";
            public string PrefabPath => $"{FolderPath}/{Id}.prefab";
            public string DataPath => $"{FolderPath}/{Id}.asset";
        }

        // §9.5 ホール1：パー3・まっすぐ・グリーン手前にバンカー。横16×縦32タイル
        private static readonly Vector2 Hole01Cup = new Vector2(0f, 26f);
        private const float Hole01GreenRadius = 4f;
        private static readonly RectInt Hole01TeeBox = new RectInt(-2, 0, 4, 4);
        private static readonly RectInt Hole01Fairway = new RectInt(-4, 4, 8, 16);
        private static readonly RectInt Hole01Bunker = new RectInt(-3, 20, 6, 2);

        // §9.5 ホール2：パー4・左から右へ曲がるドッグレッグ・曲がり角の内側に池。横16×縦44タイル。
        // カップへまっすぐ打つとスライスで池に入り、安全に打つと左の角に残る
        private static readonly Vector2 Hole02Cup = new Vector2(3f, 38f);
        private const float Hole02GreenRadius = 4f;
        private static readonly RectInt Hole02TeeBox = new RectInt(-6, 0, 4, 4);
        private static readonly RectInt Hole02FairwayStraight = new RectInt(-7, 4, 7, 22);
        private static readonly RectInt Hole02FairwayCorner = new RectInt(-7, 24, 12, 7);
        private static readonly RectInt Hole02FairwayApproach = new RectInt(0, 31, 6, 4);
        private static readonly RectInt Hole02Water = new RectInt(1, 8, 6, 15);
        private static readonly RectInt Hole02Bunker = new RectInt(-3, 36, 2, 3);

        // §9.5 ホール3：パー5・長い・両側すぐOB。横16×縦60タイル。グリーンの傾斜は Phase 5 で塗る
        private static readonly Vector2 Hole03Cup = new Vector2(0f, 54f);
        private const float Hole03GreenRadius = 4f;
        private static readonly RectInt Hole03TeeBox = new RectInt(-2, 0, 4, 4);
        private static readonly RectInt Hole03Fairway = new RectInt(-3, 4, 6, 44);
        private static readonly RectInt Hole03FairwayBunker = new RectInt(-3, 24, 2, 2);
        private static readonly RectInt Hole03GreenBunker = new RectInt(-2, 48, 4, 2);
        private static readonly RectInt Hole03LeftOutOfBounds = new RectInt(HoleMinX, 6, 3, 44);
        private static readonly RectInt Hole03RightOutOfBounds = new RectInt(5, 6, 3, 44);

        private static readonly HoleSpec[] Holes =
        {
            new HoleSpec
            {
                Id = "Hole_01", DisplayName = "ホール1", Par = 3, Height = 32,
                Tee = new Vector2(0f, 2f), Cup = Hole01Cup, MinWind = 0f, MaxWind = 3f, GroundAt = Hole01GroundAt,
            },
            new HoleSpec
            {
                Id = "Hole_02", DisplayName = "ホール2", Par = 4, Height = 44,
                Tee = new Vector2(-4f, 2f), Cup = Hole02Cup, MinWind = 0f, MaxWind = 5f, GroundAt = Hole02GroundAt,
            },
            new HoleSpec
            {
                Id = "Hole_03", DisplayName = "ホール3", Par = 5, Height = 60,
                Tee = new Vector2(0f, 2f), Cup = Hole03Cup, MinWind = 2f, MaxWind = 5f, GroundAt = Hole03GroundAt,
            },
        };

        /// <summary>ホールまわりのアセットを揃えて、カタログを返す</summary>
        public static GolfHoleCatalog EnsureAssets()
        {
            Dictionary<GroundType, GolfTerrainTile> tiles = EnsureTiles();
            GolfHoleCatalog catalog = EnsureCatalog();

            foreach (HoleSpec spec in Holes)
            {
                HoleCourse prefab = EnsureHolePrefab(spec, tiles);
                Register(catalog, EnsureHoleData(spec, prefab));
            }

            // 手で追加したホールも含めて、登録済みの全ホールの見た目を揃える
            foreach (GolfHoleData hole in catalog.Holes)
            {
                if (hole != null && hole.Prefab != null) RefreshTiles(hole.Prefab, tiles.Values);
            }

            AssetDatabase.SaveAssets();
            return catalog;
        }

        private static Dictionary<GroundType, GolfTerrainTile> EnsureTiles()
        {
            EnsureDirectory(TileDirectory);
            var tiles = new Dictionary<GroundType, GolfTerrainTile>();

            foreach (GroundType type in (GroundType[])Enum.GetValues(typeof(GroundType)))
            {
                string path = $"{TileDirectory}/Tile_{type}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<GolfTerrainTile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<GolfTerrainTile>();
                    AssetDatabase.CreateAsset(tile, path);
                    SetInt(tile, "_groundType", (int)type);
                }

                ApplyArt(tile, type);
                tiles[type] = tile;
            }

            AssetDatabase.SaveAssets();
            return tiles;
        }

        /// <summary>
        /// 見た目は画像で決めるので、既存のタイルも毎回画像に揃える（Phase 9 までの単色タイルもここで置き換わる）。
        /// 画像そのものを差し替えたいときは Tiles/Art の PNG を上書きする。
        /// </summary>
        private static void ApplyArt(GolfTerrainTile tile, GroundType type)
        {
            tile.sprite = GolfTileArtBuilder.EnsureSprite(type);
            tile.color = Color.white;

            Sprite[] frames = type == GroundType.Water ? GolfTileArtBuilder.EnsureWaterFrames() : new Sprite[0];
            var so = new SerializedObject(tile);
            SerializedProperty framesProperty = so.FindProperty("_animationFrames");
            framesProperty.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
            {
                framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tile);
        }

        /// <summary>
        /// Tilemap は塗った時点のタイルの画像・色をセルごとに覚えているため、タイルを変えたら塗り直しが要る。
        /// 手で塗り替えたホールの形はそのまま、見た目だけを最新のタイルに揃える。
        /// </summary>
        private static void RefreshTiles(HoleCourse prefab, IEnumerable<GolfTerrainTile> terrainTiles)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                // 傾斜の Tilemap は回転したセルを持つので触らず、地面タイルを塗った Tilemap だけ塗り直す
                if (terrainTiles.Any(tilemap.ContainsTile)) tilemap.RefreshAllTiles();
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static HoleCourse EnsureHolePrefab(HoleSpec spec, Dictionary<GroundType, GolfTerrainTile> tiles)
        {
            var existing = AssetDatabase.LoadAssetAtPath<HoleCourse>(spec.PrefabPath);
            if (existing != null) return existing;

            EnsureDirectory(spec.FolderPath);

            var root = new GameObject(spec.Id);
            var course = root.AddComponent<HoleCourse>();

            var gridObj = new GameObject("Grid", typeof(Grid));
            gridObj.transform.SetParent(root.transform);

            var terrainObj = new GameObject("Terrain", typeof(Tilemap), typeof(TilemapRenderer));
            terrainObj.transform.SetParent(gridObj.transform);
            terrainObj.GetComponent<TilemapRenderer>().sortingOrder = GroundSortingOrder;
            var terrain = terrainObj.GetComponent<Tilemap>();
            Paint(terrain, spec, tiles);

            var tee = new GameObject("Tee").transform;
            tee.SetParent(root.transform);
            tee.localPosition = spec.Tee;

            Transform cup = CreateCup(root.transform, spec.Cup);

            GolfSceneBuilder.SetRefs(course, ("_terrain", terrain), ("_tee", tee), ("_cup", cup));

            PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
            Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<HoleCourse>(spec.PrefabPath);
        }

        private static void Paint(Tilemap terrain, HoleSpec spec, Dictionary<GroundType, GolfTerrainTile> tiles)
        {
            for (int y = 0; y < spec.Height; y++)
            {
                for (int x = HoleMinX; x <= HoleMaxX; x++)
                {
                    var cell = new Vector2Int(x, y);
                    terrain.SetTile(new Vector3Int(x, y, 0), tiles[spec.GroundAt(cell)]);
                }
            }
        }

        private static bool IsOnGreen(Vector2Int cell, Vector2 cup, float radius)
        {
            var cellCenter = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
            return Vector2.Distance(cellCenter, cup) <= radius;
        }

        // 各ホールの塗り分けは、上に書いたものほど優先する

        private static GroundType Hole01GroundAt(Vector2Int cell)
        {
            if (IsOnGreen(cell, Hole01Cup, Hole01GreenRadius)) return GroundType.Green;
            if (Hole01Bunker.Contains(cell)) return GroundType.Bunker;
            if (Hole01TeeBox.Contains(cell)) return GroundType.Tee;
            if (Hole01Fairway.Contains(cell)) return GroundType.Fairway;
            return GroundType.Rough;
        }

        private static GroundType Hole02GroundAt(Vector2Int cell)
        {
            if (IsOnGreen(cell, Hole02Cup, Hole02GreenRadius)) return GroundType.Green;
            if (Hole02Bunker.Contains(cell)) return GroundType.Bunker;
            if (Hole02Water.Contains(cell)) return GroundType.Water;
            if (Hole02TeeBox.Contains(cell)) return GroundType.Tee;
            if (Hole02FairwayStraight.Contains(cell) || Hole02FairwayCorner.Contains(cell)
                || Hole02FairwayApproach.Contains(cell)) return GroundType.Fairway;
            return GroundType.Rough;
        }

        private static GroundType Hole03GroundAt(Vector2Int cell)
        {
            if (IsOnGreen(cell, Hole03Cup, Hole03GreenRadius)) return GroundType.Green;
            if (Hole03LeftOutOfBounds.Contains(cell) || Hole03RightOutOfBounds.Contains(cell)) return GroundType.OutOfBounds;
            if (Hole03FairwayBunker.Contains(cell) || Hole03GreenBunker.Contains(cell)) return GroundType.Bunker;
            if (Hole03TeeBox.Contains(cell)) return GroundType.Tee;
            if (Hole03Fairway.Contains(cell)) return GroundType.Fairway;
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
            hole.sortingOrder = CupSortingOrder;

            var flag = new GameObject("Flag").AddComponent<SpriteRenderer>();
            flag.transform.SetParent(cupObj.transform, false);
            flag.sortingOrder = FlagSortingOrder;

            GolfSceneBuilder.SetRefs(cupObj.AddComponent<CupView>(), ("_hole", hole), ("_flag", flag));
            return cupObj.transform;
        }

        private static GolfHoleData EnsureHoleData(HoleSpec spec, HoleCourse prefab)
        {
            var data = AssetDatabase.LoadAssetAtPath<GolfHoleData>(spec.DataPath);
            if (data != null) return data;

            data = ScriptableObject.CreateInstance<GolfHoleData>();
            AssetDatabase.CreateAsset(data, spec.DataPath);

            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue = spec.DisplayName;
            so.FindProperty("_par").intValue = spec.Par;
            so.FindProperty("_prefab").objectReferenceValue = prefab;
            so.FindProperty("_minWindStrength").floatValue = spec.MinWind;
            so.FindProperty("_maxWindStrength").floatValue = spec.MaxWind;
            so.ApplyModifiedPropertiesWithoutUndo();

            return data;
        }

        private static GolfHoleCatalog EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GolfHoleCatalog>(CatalogPath);
            if (catalog != null) return catalog;

            EnsureDirectory(Path.GetDirectoryName(CatalogPath));
            catalog = ScriptableObject.CreateInstance<GolfHoleCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        /// <summary>未登録なら末尾に足す。並べ替えや削除をした一覧は崩さない</summary>
        private static void Register(GolfHoleCatalog catalog, GolfHoleData hole)
        {
            foreach (GolfHoleData registered in catalog.Holes)
            {
                if (registered == hole) return;
            }

            var so = new SerializedObject(catalog);
            SerializedProperty holes = so.FindProperty("_holes");
            holes.InsertArrayElementAtIndex(holes.arraySize);
            holes.GetArrayElementAtIndex(holes.arraySize - 1).objectReferenceValue = hole;
            so.ApplyModifiedPropertiesWithoutUndo();
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
