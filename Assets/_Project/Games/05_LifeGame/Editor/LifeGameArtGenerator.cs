using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiniGame.LifeGame.Editor
{
    /// <summary>
    /// 人生ゲームのドット絵（マス・道・アイコン・背景・乗り物・キャラの顔と立ち絵）をコードから生成する。
    /// モルックの MolkkyArtGenerator と同じく、外部素材なしでいつでも同じ絵を作り直せるようにしている。
    /// 生成物は通常のPNGなので、手描きに差し替えてもよい（テーマ・キャラのアセットの参照を差し替えるだけ）。
    /// </summary>
    public static class LifeGameArtGenerator
    {
        public const string SpriteDirectory = "Assets/_Project/Games/05_LifeGame/Sprites";

        /// <summary>1ワールド単位あたりのピクセル数。マス（16px）がちょうど1単位になる</summary>
        private const int PixelsPerUnit = 16;

        public const string CellName = "Cell";
        public const string RoadName = "Road";
        public const string FamilyFaceName = "Face_Family";

        /// <summary>LifeDataGenerator のキャラと同じ並び（カタログの番号順）</summary>
        private static readonly string[] CharacterIds = { "Balance", "Worker", "Lucky", "Saver" };

        private const int CellSize = 16;
        private const int RoadWidth = 4;
        private const int RoadHeight = 6;
        private const int IconSize = 12;
        private const int TileSize = 32;
        private const int VehicleWidth = 16;
        private const int VehicleHeight = 24;
        private const int FaceSize = 12;
        private const int PortraitWidth = 24;
        private const int PortraitHeight = 32;
        private const int LandmarkSize = 28;
        private const int DecorationSize = 16;
        private const int DecorationsPerTheme = 2;

        // マスと道は白地で描き、実行時にテーマの色を掛ける。縁だけ灰色にして、掛けた後も同じ色の濃い縁になるようにする
        private static readonly Color32 TintBase = new Color32(255, 255, 255, 255);
        private static readonly Color32 TintEdge = new Color32(150, 150, 150, 255);
        private static readonly Color32 TintShade = new Color32(225, 225, 225, 255);

        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);
        private static readonly Color32 Outline = new Color32(40, 34, 44, 255);
        private static readonly Color32 Skin = new Color32(250, 210, 175, 255);
        private static readonly Color32 Blush = new Color32(245, 150, 150, 255);
        private static readonly Color32 Pants = new Color32(60, 64, 88, 255);
        private static readonly Color32 Shoes = new Color32(40, 34, 30, 255);

        private enum HairStyle
        {
            Short,
            Spiky,
            Long,
            Bun,
        }

        /// <summary>CharacterIds と同じ並び。席の色（赤青緑黄）と混ざらないよう、服は少しくすませる</summary>
        private static readonly (Color32 Hair, Color32 Shirt, HairStyle Style)[] CharacterLooks =
        {
            (new Color32(110, 70, 40, 255), new Color32(80, 150, 120, 255), HairStyle.Short),
            (new Color32(40, 36, 44, 255), new Color32(215, 110, 50, 255), HairStyle.Spiky),
            (new Color32(235, 195, 90, 255), new Color32(140, 105, 195, 255), HairStyle.Long),
            (new Color32(75, 50, 35, 255), new Color32(90, 110, 150, 255), HairStyle.Bun),
        };

        // 結婚相手・子供の顔。運転手（キャラ）と見分けやすいよう髪は明るい灰色にする
        private static readonly (Color32 Hair, Color32 Shirt, HairStyle Style) FamilyLook =
            (new Color32(170, 160, 150, 255), new Color32(200, 200, 200, 255), HairStyle.Short);

        /// <summary>
        /// 建物を建てる節目のマスと、LifeThemeData の欄の名前。壁と屋根の色で見分け、正面にマスのアイコンを看板として描く。
        /// 屋根の形はテーマで変える（現代＝平屋根・ファンタジー＝三角屋根・宇宙＝ドーム）
        /// </summary>
        private static readonly (LifeCellType Type, string Field, Color32 Wall, Color32 Roof)[] Landmarks =
        {
            (LifeCellType.Start, "_landmarkStart", new Color32(240, 225, 190, 255), new Color32(200, 90, 70, 255)),
            (LifeCellType.JobOffer, "_landmarkJob", new Color32(190, 200, 215, 255), new Color32(80, 95, 120, 255)),
            (LifeCellType.Graduation, "_landmarkSchool", new Color32(200, 120, 90, 255), new Color32(120, 70, 60, 255)),
            (LifeCellType.Marriage, "_landmarkWedding", new Color32(250, 250, 245, 255), new Color32(240, 150, 180, 255)),
            (LifeCellType.Goal, "_landmarkGoal", new Color32(250, 215, 110, 255), new Color32(200, 140, 40, 255)),
        };

        private static readonly Color32 WindowColor = new Color32(165, 215, 250, 255);
        private static readonly Color32 Wood = new Color32(140, 92, 52, 255);
        private static readonly Color32 Leaf = new Color32(70, 150, 75, 255);
        private static readonly Color32 LeafDark = new Color32(45, 110, 60, 255);
        private static readonly Color32 LeafLight = new Color32(120, 195, 105, 255);
        private static readonly Color32 Rock = new Color32(130, 130, 150, 255);
        private static readonly Color32 RockDark = new Color32(95, 95, 115, 255);
        private static readonly Color32 Red = new Color32(225, 65, 65, 255);
        private static readonly Color32 Cream = new Color32(240, 230, 210, 255);

        /// <summary>全素材を描き直す（Rebuild LifeGame から呼ばれる）</summary>
        public static void GenerateAll()
        {
            Directory.CreateDirectory(SpriteDirectory);

            GenerateBoardSprites();
            GenerateThemeSprites();
            GenerateCharacterSprites();

            AssetDatabase.Refresh();
            Debug.Log($"[LifeGameArtGenerator] 人生ゲームの素材を生成しました: {SpriteDirectory}");
        }

        public static Sprite Load(string spriteName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDirectory}/{spriteName}.png");
        }

        /// <summary>LifeCellType の番号順に並べたアイコン（BoardView が種類の番号で引く）</summary>
        public static Sprite[] LoadIcons()
        {
            var types = (LifeCellType[])Enum.GetValues(typeof(LifeCellType));
            var icons = new Sprite[types.Length];
            foreach (LifeCellType type in types) icons[(int)type] = Load(IconName(type));
            return icons;
        }

        /// <summary>
        /// テーマの乗り物・背景と、キャラの顔・立ち絵を、空いている欄にだけ入れる（手描きに差し替えた参照を消さないため）。
        /// themes は LifeThemeDefaults.All と同じ並び
        /// </summary>
        public static void AssignArt(LifeThemeData[] themes, LifeCharacterCatalog catalog)
        {
            for (int i = 0; i < themes.Length && i < LifeThemeDefaults.All.Length; i++)
            {
                string id = LifeThemeDefaults.All[i].Id;
                SetIfEmpty(themes[i], ("_vehicleBody", Load(VehicleBodyName(id))),
                    ("_vehicleDetail", Load(VehicleDetailName(id))), ("_backgroundTile", Load(TileName(id))));
                foreach (var landmark in Landmarks)
                {
                    SetIfEmpty(themes[i], (landmark.Field, Load(LandmarkName(id, landmark.Type))));
                }

                SetArrayIfEmpty(themes[i], "_decorations", LoadDecorations(id));
            }

            for (int i = 0; i < catalog.Count && i < CharacterIds.Length; i++)
            {
                SetIfEmpty(catalog.Get(i), ("_face", Load(FaceName(CharacterIds[i]))),
                    ("_portrait", Load(PortraitName(CharacterIds[i]))));
            }

            AssetDatabase.SaveAssets();
        }

        private static string IconName(LifeCellType type) => $"Icon_{type}";
        private static string TileName(string themeId) => $"Tile_{themeId}";
        private static string VehicleBodyName(string themeId) => $"Vehicle_{themeId}_Body";
        private static string VehicleDetailName(string themeId) => $"Vehicle_{themeId}_Detail";
        private static string LandmarkName(string themeId, LifeCellType type) => $"Landmark_{themeId}_{type}";
        private static string DecorationName(string themeId, int index) => $"Decoration_{themeId}_{index}";
        private static string FaceName(string characterId) => $"Face_{characterId}";
        private static string PortraitName(string characterId) => $"Portrait_{characterId}";

        private static void SetIfEmpty(Object target, params (string Name, Sprite Sprite)[] refs)
        {
            var so = new SerializedObject(target);
            foreach ((string name, Sprite sprite) in refs)
            {
                SerializedProperty property = so.FindProperty(name);
                if (property.objectReferenceValue == null) property.objectReferenceValue = sprite;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArrayIfEmpty(Object target, string name, Sprite[] sprites)
        {
            var so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(name);
            if (property.arraySize > 0) return;

            property.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite[] LoadDecorations(string themeId)
        {
            var sprites = new Sprite[DecorationsPerTheme];
            for (int i = 0; i < sprites.Length; i++) sprites[i] = Load(DecorationName(themeId, i));
            return sprites;
        }

        // ------------------------------------------------------------------
        // 盤面（マス・道・アイコン）
        // ------------------------------------------------------------------
        private static void GenerateBoardSprites()
        {
            SaveSprite(CellName, BuildCell(), CellSize, CellSize);
            // 道は長さを自由に変えるので、上下の縁だけ伸ばさない Sliced にする
            SaveSprite(RoadName, BuildRoad(), RoadWidth, RoadHeight, new Vector4(0, 1, 0, 1));

            foreach (KeyValuePair<LifeCellType, string[]> icon in LifeArtPatterns.Icons)
            {
                SaveSprite(IconName(icon.Key), FromPattern(icon.Value, IconSize, IconSize, LifeArtPatterns.FixedPalette),
                    IconSize, IconSize);
            }
        }

        /// <summary>角を丸めた白いマス。縁と下側の影で、道の上に置いたとき板のように見せる</summary>
        private static Color32[] BuildCell()
        {
            var pixels = NewCanvas(CellSize, CellSize);
            int last = CellSize - 1;
            for (int y = 0; y < CellSize; y++)
            {
                for (int x = 0; x < CellSize; x++)
                {
                    if (IsRoundedCorner(x, y, last)) continue;

                    bool edge = x == 0 || y == 0 || x == last || y == last;
                    Color32 color = edge ? TintEdge : (y == last - 1 ? TintShade : TintBase);
                    SetPixel(pixels, CellSize, CellSize, x, y, color);
                }
            }

            return pixels;
        }

        /// <summary>四隅の外側2ピクセルを削る</summary>
        private static bool IsRoundedCorner(int x, int y, int last)
        {
            bool cornerX = x == 0 || x == last;
            bool cornerY = y == 0 || y == last;
            bool nearX = x <= 1 || x >= last - 1;
            bool nearY = y <= 1 || y >= last - 1;
            return (cornerX && nearY) || (cornerY && nearX);
        }

        private static Color32[] BuildRoad()
        {
            var pixels = NewCanvas(RoadWidth, RoadHeight);
            for (int y = 0; y < RoadHeight; y++)
            {
                bool edge = y == 0 || y == RoadHeight - 1;
                FillRect(pixels, RoadWidth, RoadHeight, 0, y, RoadWidth, 1, edge ? TintEdge : TintBase);
            }

            return pixels;
        }

        // ------------------------------------------------------------------
        // テーマ（背景タイル・乗り物）
        // ------------------------------------------------------------------
        private static void GenerateThemeSprites()
        {
            for (int i = 0; i < LifeThemeDefaults.All.Length; i++)
            {
                LifeThemeDefaults theme = LifeThemeDefaults.All[i];
                SaveSprite(TileName(theme.Id), BuildTile(i, theme.Background), TileSize, TileSize);

                string[] vehicle = LifeArtPatterns.Vehicles[i];
                SaveSprite(VehicleBodyName(theme.Id),
                    FromPattern(vehicle, VehicleWidth, VehicleHeight, LifeArtPatterns.BodyPalette), VehicleWidth, VehicleHeight);
                SaveSprite(VehicleDetailName(theme.Id),
                    FromPattern(vehicle, VehicleWidth, VehicleHeight, DetailOnly()), VehicleWidth, VehicleHeight);

                foreach (var landmark in Landmarks)
                {
                    SaveSprite(LandmarkName(theme.Id, landmark.Type),
                        BuildLandmark(i, landmark.Type, landmark.Wall, landmark.Roof), LandmarkSize, LandmarkSize);
                }

                for (int d = 0; d < DecorationsPerTheme; d++)
                {
                    SaveSprite(DecorationName(theme.Id, d), BuildDecoration(i, d), DecorationSize, DecorationSize);
                }
            }
        }

        /// <summary>車体の文字（w/s/d）を除いた固定色。車体と同じ文字が固定色にもあるので、車体側を優先して外す</summary>
        private static Dictionary<char, Color32> DetailOnly()
        {
            var palette = new Dictionary<char, Color32>(LifeArtPatterns.FixedPalette);
            foreach (char key in LifeArtPatterns.BodyPalette.Keys) palette.Remove(key);
            return palette;
        }

        /// <summary>
        /// 盤面の後ろに敷き詰める背景。宇宙の星は背景より明るいので、色を掛けるのではなくテーマの背景色で直接描く。
        /// themeIndex は LifeThemeDefaults.All の並び（0 = 草原・1 = 花畑・2 = 星空）
        /// </summary>
        private static Color32[] BuildTile(int themeIndex, Color background)
        {
            var pixels = NewCanvas(TileSize, TileSize);
            Color32 baseColor = background;
            FillRect(pixels, TileSize, TileSize, 0, 0, TileSize, TileSize, baseColor);

            Color32 darker = background * 0.85f;
            darker.a = 255;
            switch (themeIndex)
            {
                case 0:
                    Scatter(pixels, 10, 11, (x, y) => DrawTuft(pixels, x, y, darker));
                    Scatter(pixels, 2, 12, (x, y) => SetWrapped(pixels, x, y, new Color32(255, 250, 235, 255)));
                    break;
                case 1:
                    Scatter(pixels, 7, 21, (x, y) => DrawTuft(pixels, x, y, darker));
                    Scatter(pixels, 4, 22, (x, y) => DrawFlower(pixels, x, y, new Color32(250, 215, 90, 255)));
                    Scatter(pixels, 3, 23, (x, y) => DrawFlower(pixels, x, y, new Color32(245, 160, 190, 255)));
                    break;
                default:
                    Scatter(pixels, 12, 31, (x, y) => SetWrapped(pixels, x, y, new Color32(200, 205, 230, 255)));
                    Scatter(pixels, 2, 32, (x, y) => DrawFlower(pixels, x, y, new Color32(255, 255, 255, 255)));
                    break;
            }

            return pixels;
        }

        /// <summary>座標から決まるノイズで count 個の位置を選ぶ（毎回同じ絵にするため乱数は使わない）</summary>
        private static void Scatter(Color32[] pixels, int count, int seed, Action<int, int> draw)
        {
            for (int i = 0; i < count; i++) draw(Hash(i, seed) % TileSize, Hash(seed, i * 7 + 3) % TileSize);
        }

        /// <summary>草の「v」。タイルの端をまたいでも継ぎ目が出ないよう、はみ出した分は反対側に描く</summary>
        private static void DrawTuft(Color32[] pixels, int x, int y, Color32 color)
        {
            SetWrapped(pixels, x, y, color);
            SetWrapped(pixels, x + 2, y, color);
            SetWrapped(pixels, x + 1, y + 1, color);
        }

        /// <summary>十字の小さな花（宇宙では明るい星）</summary>
        private static void DrawFlower(Color32[] pixels, int x, int y, Color32 color)
        {
            SetWrapped(pixels, x, y, color);
            SetWrapped(pixels, x - 1, y, color);
            SetWrapped(pixels, x + 1, y, color);
            SetWrapped(pixels, x, y - 1, color);
            SetWrapped(pixels, x, y + 1, color);
        }

        private static void SetWrapped(Color32[] pixels, int x, int y, Color32 color)
        {
            int wx = (x % TileSize + TileSize) % TileSize;
            int wy = (y % TileSize + TileSize) % TileSize;
            SetPixel(pixels, TileSize, TileSize, wx, wy, color);
        }

        // ------------------------------------------------------------------
        // 建物と飾り（themeIndex は LifeThemeDefaults.All の並び：0 = 現代・1 = ファンタジー・2 = 宇宙）
        // ------------------------------------------------------------------
        /// <summary>屋根（テーマごとの形）→ 壁 → 窓とドア → 正面の看板（マスのアイコン）の順に重ねる</summary>
        private static Color32[] BuildLandmark(int themeIndex, LifeCellType type, Color32 wall, Color32 roof)
        {
            const int s = LandmarkSize;
            var pixels = NewCanvas(s, s);
            int wallTop = DrawRoof(pixels, themeIndex, roof);

            FillRect(pixels, s, s, 3, wallTop, s - 6, s - wallTop, wall);
            for (int y = wallTop + 2; y < s - 6; y += 5)
            {
                FillRect(pixels, s, s, 5, y, 2, 2, WindowColor);
                FillRect(pixels, s, s, s - 7, y, 2, 2, WindowColor);
            }

            FillRect(pixels, s, s, s / 2 - 2, s - 5, 4, 5, Wood);

            Color32[] icon = FromPattern(LifeArtPatterns.Icons[type], IconSize, IconSize, LifeArtPatterns.FixedPalette);
            Paste(pixels, s, s, icon, IconSize, IconSize, (s - IconSize) / 2, wallTop + 1);

            AddOutline(pixels, s, s);
            return pixels;
        }

        /// <summary>屋根を描いて、壁を描き始める行を返す</summary>
        private static int DrawRoof(Color32[] pixels, int themeIndex, Color32 roof)
        {
            const int s = LandmarkSize;
            const int center = s / 2;
            switch (themeIndex)
            {
                case 0:
                    // 平屋根のビル。屋上の縁だけ張り出させる
                    FillRect(pixels, s, s, 2, 5, s - 4, 3, roof);
                    return 8;
                case 1:
                    // 三角屋根。下の行ほど広げる
                    for (int y = 0; y < 10; y++)
                    {
                        int half = 1 + Mathf.RoundToInt(y * 1.35f);
                        FillRect(pixels, s, s, center - half, y, half * 2, 1, roof);
                    }

                    return 10;
                default:
                    // 宇宙基地のドームと、先が赤く光るアンテナ
                    FillRect(pixels, s, s, center - 1, 0, 1, 3, RockDark);
                    SetPixel(pixels, s, s, center - 1, 0, Red);
                    FillEllipse(pixels, s, s, center, 12f, 11f, 9f, roof);
                    return 11;
            }
        }

        /// <summary>道の外に散らす飾り。テーマごとに2種類（現代＝木と茂み・ファンタジー＝針葉樹とキノコ・宇宙＝岩と輪のある星）</summary>
        private static Color32[] BuildDecoration(int themeIndex, int variant)
        {
            const int s = DecorationSize;
            var pixels = NewCanvas(s, s);
            switch (themeIndex * DecorationsPerTheme + variant)
            {
                case 0:
                    FillRect(pixels, s, s, 7, 10, 2, 5, Wood);
                    FillEllipse(pixels, s, s, 8f, 6.5f, 5.5f, 5f, Leaf);
                    FillEllipse(pixels, s, s, 6.5f, 5f, 2f, 1.8f, LeafLight);
                    break;
                case 1:
                    FillEllipse(pixels, s, s, 8f, 11f, 6.5f, 3.8f, Leaf);
                    FillEllipse(pixels, s, s, 6f, 10f, 2f, 1.5f, LeafLight);
                    SetPixel(pixels, s, s, 10, 10, new Color32(245, 130, 170, 255));
                    SetPixel(pixels, s, s, 5, 12, new Color32(250, 205, 60, 255));
                    break;
                case 2:
                    FillRect(pixels, s, s, 7, 12, 2, 3, Wood);
                    for (int y = 1; y < 12; y++)
                    {
                        // 3段の枝。段の切れ目ごとに幅を戻してギザギザにする
                        int half = 1 + y % 4 + y / 4;
                        FillRect(pixels, s, s, 8 - half, y, half * 2, 1, y % 4 == 0 ? LeafLight : LeafDark);
                    }

                    break;
                case 3:
                    FillEllipse(pixels, s, s, 8f, 9f, 6f, 5f, Red);
                    // 傘の下半分を消して半円にし、軸を立てる
                    FillRect(pixels, s, s, 0, 9, s, s - 9, Transparent);
                    FillRect(pixels, s, s, 6, 9, 4, 6, Cream);
                    SetPixel(pixels, s, s, 5, 6, TintBase);
                    SetPixel(pixels, s, s, 9, 5, TintBase);
                    SetPixel(pixels, s, s, 11, 7, TintBase);
                    break;
                case 4:
                    FillEllipse(pixels, s, s, 8f, 10f, 6.5f, 4.5f, Rock);
                    FillEllipse(pixels, s, s, 6f, 9.5f, 1.5f, 1f, RockDark);
                    FillEllipse(pixels, s, s, 10.5f, 11f, 1.2f, 0.9f, RockDark);
                    break;
                default:
                    FillEllipse(pixels, s, s, 8f, 8f, 4.5f, 4.5f, new Color32(235, 150, 80, 255));
                    FillRect(pixels, s, s, 1, 8, s - 2, 1, new Color32(250, 215, 140, 255));
                    FillEllipse(pixels, s, s, 6.5f, 6.5f, 1.2f, 1f, new Color32(250, 190, 120, 255));
                    break;
            }

            AddOutline(pixels, s, s);
            return pixels;
        }

        /// <summary>透明な所を除いて重ねる（x,y は左上原点。source は FromPattern などで作った配列）</summary>
        private static void Paste(Color32[] pixels, int w, int h, Color32[] source, int sw, int sh, int x0, int y0)
        {
            for (int y = 0; y < sh; y++)
            {
                for (int x = 0; x < sw; x++)
                {
                    Color32 color = source[(sh - 1 - y) * sw + x];
                    if (color.a != 0) SetPixel(pixels, w, h, x0 + x, y0 + y, color);
                }
            }
        }

        // ------------------------------------------------------------------
        // キャラ（顔・立ち絵）
        // ------------------------------------------------------------------
        private static void GenerateCharacterSprites()
        {
            for (int i = 0; i < CharacterIds.Length; i++)
            {
                SaveSprite(FaceName(CharacterIds[i]), BuildFace(CharacterLooks[i]), FaceSize, FaceSize);
                SaveSprite(PortraitName(CharacterIds[i]), BuildPortrait(CharacterLooks[i]), PortraitWidth, PortraitHeight);
            }

            SaveSprite(FamilyFaceName, BuildFace(FamilyLook), FaceSize, FaceSize);
        }

        /// <summary>コマに乗せる顔。小さく出るので、髪の色と形だけで見分けられるようにする</summary>
        private static Color32[] BuildFace((Color32 Hair, Color32 Shirt, HairStyle Style) look)
        {
            var pixels = NewCanvas(FaceSize, FaceSize);
            DrawHead(pixels, FaceSize, FaceSize, 6f, 7f, 4.3f, look.Hair, look.Style, false);
            AddOutline(pixels, FaceSize, FaceSize);
            return pixels;
        }

        /// <summary>勝利演出とキャラ選択の立ち絵。頭を大きくした2頭身</summary>
        private static Color32[] BuildPortrait((Color32 Hair, Color32 Shirt, HairStyle Style) look)
        {
            const int w = PortraitWidth;
            const int h = PortraitHeight;
            var pixels = NewCanvas(w, h);

            // 脚・靴
            FillRect(pixels, w, h, 8, 26, 3, 4, Pants);
            FillRect(pixels, w, h, 13, 26, 3, 4, Pants);
            FillRect(pixels, w, h, 7, 30, 4, 1, Shoes);
            FillRect(pixels, w, h, 13, 30, 4, 1, Shoes);

            // 腕を上げてバンザイさせ、勝利演出で喜んでいるように見せる
            FillRect(pixels, w, h, 3, 13, 2, 7, Skin);
            FillRect(pixels, w, h, 19, 13, 2, 7, Skin);
            FillRect(pixels, w, h, 5, 18, 14, 9, look.Shirt);

            DrawHead(pixels, w, h, 12f, 10.5f, 7.5f, look.Hair, look.Style, true);
            AddOutline(pixels, w, h);
            return pixels;
        }

        /// <summary>丸い顔に髪をかぶせる。髪型は上に足す形（トゲ・お団子）と横に垂らす形（長髪）で描き分ける</summary>
        private static void DrawHead(Color32[] pixels, int w, int h, float cx, float cy, float r, Color32 hair,
            HairStyle style, bool detailed)
        {
            if (style == HairStyle.Long)
            {
                float sideWidth = r * 0.4f;
                FillRect(pixels, w, h, Mathf.RoundToInt(cx - r - 0.5f), Mathf.RoundToInt(cy - r * 0.3f),
                    Mathf.CeilToInt(sideWidth), Mathf.CeilToInt(r * 1.4f), hair);
                FillRect(pixels, w, h, Mathf.RoundToInt(cx + r - sideWidth + 0.5f), Mathf.RoundToInt(cy - r * 0.3f),
                    Mathf.CeilToInt(sideWidth), Mathf.CeilToInt(r * 1.4f), hair);
            }

            if (style == HairStyle.Bun) FillEllipse(pixels, w, h, cx, cy - r - r * 0.2f, r * 0.45f, r * 0.45f, hair);

            FillEllipse(pixels, w, h, cx, cy, r, r, Skin);
            DrawHairCap(pixels, w, h, cx, cy, r, hair);

            if (style == HairStyle.Spiky)
            {
                for (int i = -1; i <= 1; i++)
                {
                    int x = Mathf.RoundToInt(cx + i * r * 0.55f - 0.5f);
                    FillRect(pixels, w, h, x, Mathf.RoundToInt(cy - r - 1.5f), 1, 2, hair);
                }
            }

            DrawFace(pixels, w, h, cx, cy, r, detailed);
        }

        /// <summary>頭の上側（中心より少し上まで）を髪で塗る</summary>
        private static void DrawHairCap(Color32[] pixels, int w, int h, float cx, float cy, float r, Color32 hair)
        {
            const float hairLine = 0.2f;
            for (int y = Mathf.FloorToInt(cy - r) - 1; y < cy - r * hairLine; y++)
            {
                for (int x = Mathf.FloorToInt(cx - r) - 1; x <= Mathf.CeilToInt(cx + r); x++)
                {
                    float dx = (x + 0.5f - cx) / r;
                    float dy = (y + 0.5f - cy) / r;
                    if (dx * dx + dy * dy <= 1f) SetPixel(pixels, w, h, x, y, hair);
                }
            }
        }

        private static void DrawFace(Color32[] pixels, int w, int h, float cx, float cy, float r, bool detailed)
        {
            int eyeY = Mathf.RoundToInt(cy + r * 0.1f);
            int leftEye = Mathf.RoundToInt(cx - r * 0.4f - 0.5f);
            int rightEye = Mathf.RoundToInt(cx + r * 0.4f - 0.5f);
            int eyeHeight = detailed ? 2 : 1;
            FillRect(pixels, w, h, leftEye, eyeY, 1, eyeHeight, Outline);
            FillRect(pixels, w, h, rightEye, eyeY, 1, eyeHeight, Outline);
            if (!detailed) return;

            int cheekY = eyeY + 2;
            SetPixel(pixels, w, h, leftEye - 1, cheekY, Blush);
            SetPixel(pixels, w, h, rightEye + 1, cheekY, Blush);
            FillRect(pixels, w, h, Mathf.RoundToInt(cx - 1f), cheekY + 1, 2, 1, Outline);
        }

        // ------------------------------------------------------------------
        // 描画ヘルパー（x,y は左上原点。Texture2D は左下原点なので SetPixel で反転する）
        // ------------------------------------------------------------------
        /// <summary>1文字＝1ピクセルの文字列を色にする。パレットに無い文字は透明</summary>
        private static Color32[] FromPattern(string[] rows, int w, int h, Dictionary<char, Color32> palette)
        {
            var pixels = NewCanvas(w, h);
            for (int y = 0; y < h && y < rows.Length; y++)
            {
                for (int x = 0; x < w && x < rows[y].Length; x++)
                {
                    if (palette.TryGetValue(rows[y][x], out Color32 color)) SetPixel(pixels, w, h, x, y, color);
                }
            }

            return pixels;
        }

        private static Color32[] NewCanvas(int w, int h)
        {
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Transparent;
            return pixels;
        }

        private static void SetPixel(Color32[] pixels, int w, int h, int x, int y, Color32 color)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;

            pixels[(h - 1 - y) * w + x] = color;
        }

        private static void FillRect(Color32[] pixels, int w, int h, int x0, int y0, int rw, int rh, Color32 color)
        {
            for (int y = y0; y < y0 + rh; y++)
            {
                for (int x = x0; x < x0 + rw; x++) SetPixel(pixels, w, h, x, y, color);
            }
        }

        private static void FillEllipse(Color32[] pixels, int w, int h, float cx, float cy, float rx, float ry, Color32 color)
        {
            for (int y = Mathf.FloorToInt(cy - ry) - 1; y <= Mathf.CeilToInt(cy + ry) + 1; y++)
            {
                for (int x = Mathf.FloorToInt(cx - rx) - 1; x <= Mathf.CeilToInt(cx + rx) + 1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) SetPixel(pixels, w, h, x, y, color);
                }
            }
        }

        /// <summary>塗った部分を1ピクセルの縁で囲む。どのテーマの背景の上でも輪郭が埋もれないようにする</summary>
        private static void AddOutline(Color32[] pixels, int w, int h)
        {
            var source = (Color32[])pixels.Clone();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (source[i].a != 0) continue;

                int x = i % w;
                int y = i / w;
                bool touches = IsFilled(source, w, h, x - 1, y) || IsFilled(source, w, h, x + 1, y) ||
                               IsFilled(source, w, h, x, y - 1) || IsFilled(source, w, h, x, y + 1);
                if (touches) pixels[i] = Outline;
            }
        }

        private static bool IsFilled(Color32[] pixels, int w, int h, int x, int y)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return false;

            return pixels[y * w + x].a != 0;
        }

        private static int Hash(int x, int y)
        {
            int value = x * 73856093 ^ y * 19349663;
            return Mathf.Abs(value % 1000);
        }

        // ------------------------------------------------------------------
        // 保存とインポート設定
        // ------------------------------------------------------------------
        private static void SaveSprite(string spriteName, Color32[] pixels, int w, int h, Vector4 border = default)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();

            string path = $"{SpriteDirectory}/{spriteName}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ConfigureImporter(path, border);
        }

        private static void ConfigureImporter(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Point; // ドット絵をぼかさない
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            // Sliced・Tiled で描くには FullRect が必要（Tight だと縁の切り出しができない）
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }
    }
}
