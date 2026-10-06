using MiniGame.PenguinWars.Battle;
using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>カメラと戦場（空・遠くの山・地面・左右の城）。絵は FieldArtGenerator が作る</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const float CameraZ = -10f;
        // 地面の上端が Y=0（ユニットが立つ高さ）。下部の出撃ボタンが地面に重なるよう、カメラを少し上に向ける
        private const float CameraY = 1.5f;
        // 地面の絵を縦に繰り返さないよう、絵の高さと同じ厚さにする
        private const float GroundDepth = (float)FieldArtGenerator.GroundTileHeight / PenguinSpriteWriter.PixelsPerUnit;
        private const float SkyHeight = (float)FieldArtGenerator.SkyHeight / PenguinSpriteWriter.PixelsPerUnit;
        // カメラの端の余白より広く敷き、どこまでスクロールしても地面が切れないようにする
        private const float GroundOverhang = 6f;

        private const float HpBarOffsetY = 4.6f;
        private static readonly Vector2 HpBarSize = new Vector2(2.6f, 0.3f);

        private const int SkySortingOrder = -2;
        private const int MountainSortingOrder = -1;
        private const int GroundSortingOrder = 0;
        private const int CastleSortingOrder = 1;
        // ユニット（Battle.cs の 5〜7）より手前に出す
        private const int HpBarBackSortingOrder = 10;

        private static readonly Color HpBarBackColor = new Color(0.15f, 0.15f, 0.2f);
        private static readonly Color HpBarFillColor = new Color(0.3f, 0.9f, 0.4f);

        /// <summary>orthographicSize は BattleCamera が画面の横幅から毎フレーム決めるので、ここでは設定しない</summary>
        private static BattleCamera CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            // 空の絵より上が見えても切れ目が出ないよう、空の一番上と同じ色にする
            camera.backgroundColor = FieldArtGenerator.SkyTopColor;
            camera.orthographic = true;
            cameraObj.transform.position = new Vector3(0f, CameraY, CameraZ);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            var battleCamera = cameraObj.AddComponent<BattleCamera>();
            SetRefs(battleCamera, ("_camera", camera));
            return battleCamera;
        }

        /// <param name="fieldLength">いちばん長い戦場。対戦のステージはこれより短く、右の城は BattleRunner が試合開始時に動かす</param>
        private static (CastleView left, CastleView right, FieldBackdrop backdrop) CreateField(float fieldLength)
        {
            var fieldRoot = new GameObject("Field").transform;
            float width = fieldLength + GroundOverhang * 2f;
            float centerX = fieldLength * 0.5f;
            SpriteRenderer mountains = CreateBackground(fieldRoot, width, centerX);
            SpriteRenderer ground = CreateTiled(fieldRoot, "Ground", PenguinSpriteWriter.Load(FieldArtGenerator.GroundPath),
                new Vector2(centerX, -GroundDepth * 0.5f), new Vector2(width, GroundDepth), GroundSortingOrder);
            var backdrop = fieldRoot.gameObject.AddComponent<FieldBackdrop>();
            var so = new UnityEditor.SerializedObject(backdrop);
            SerializedArray(so, "_tinted", new[] { mountains, ground });
            so.ApplyModifiedPropertiesWithoutUndo();

            CastleView left = CreateCastle(fieldRoot, "Castle_Left", 0f, Side.Left);
            CastleView right = CreateCastle(fieldRoot, "Castle_Right", fieldLength, Side.Right);
            return (left, right, backdrop);
        }

        /// <summary>空のグラデーションは横に引き伸ばし、山は横に敷き詰める。どちらも地面の上端（Y=0）から上に置く。山を返す（ステージの色を付けるため）</summary>
        private static SpriteRenderer CreateBackground(Transform fieldRoot, float width, float centerX)
        {
            SpriteRenderer sky = CreateSprite(fieldRoot, "Sky", PenguinSpriteWriter.Load(FieldArtGenerator.SkyPath), SkySortingOrder);
            sky.transform.localPosition = new Vector3(centerX, 0f, 0f);
            sky.drawMode = SpriteDrawMode.Sliced;
            sky.size = new Vector2(width, SkyHeight);

            float mountainHeight = (float)FieldArtGenerator.MountainTileHeight / PenguinSpriteWriter.PixelsPerUnit;
            return CreateTiled(fieldRoot, "Mountains", PenguinSpriteWriter.Load(FieldArtGenerator.MountainsPath),
                new Vector2(centerX, 0f), new Vector2(width, mountainHeight), MountainSortingOrder);
        }

        /// <summary>城の足元（X, 0）を原点にする。ユニットとの距離を X だけで比べるため</summary>
        private static CastleView CreateCastle(Transform parent, string name, float x, Side side)
        {
            var castleObj = new GameObject(name);
            castleObj.transform.SetParent(parent, false);
            castleObj.transform.localPosition = new Vector3(x, 0f, 0f);

            SpriteRenderer body = CreateSprite(castleObj.transform, "Body",
                PenguinSpriteWriter.Load(FieldArtGenerator.CastlePath(side)), CastleSortingOrder);
            // 城の絵は右向き（旗が右になびく）なので、右の城は反転して戦場の内側を向かせる
            body.flipX = side == Side.Right;

            HpBarView hpBar = CreateHpBar(castleObj.transform, HpBarOffsetY, HpBarSize, HpBarBackSortingOrder);

            var castleView = castleObj.AddComponent<CastleView>();
            SetRefs(castleView, ("_hpBar", hpBar));
            var collapse = castleObj.AddComponent<CastleCollapse>();
            SetRefs(collapse, ("_body", body.transform), ("_hpBar", hpBar.gameObject));
            return castleView;
        }

        /// <summary>城とユニットで共通のHPバー。Fill は Back の1つ上に描く</summary>
        private static HpBarView CreateHpBar(Transform parent, float offsetY, Vector2 size, int backSortingOrder)
        {
            var hpBarRoot = new GameObject("HpBar");
            hpBarRoot.transform.SetParent(parent, false);
            hpBarRoot.transform.localPosition = new Vector3(0f, offsetY, 0f);
            CreateBox(hpBarRoot.transform, "Back", Vector2.zero, size, HpBarBackColor, backSortingOrder);
            Transform fill = CreateBox(hpBarRoot.transform, "Fill", Vector2.zero, size, HpBarFillColor, backSortingOrder + 1);

            var hpBar = hpBarRoot.AddComponent<HpBarView>();
            SetRefs(hpBar, ("_fill", fill));
            return hpBar;
        }

        /// <summary>生成したドット絵を貼る。原点はスプライトのピボット（キャラ・城は足元）</summary>
        private static SpriteRenderer CreateSprite(Transform parent, string name, Sprite sprite, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        /// <summary>同じ絵を横に敷き詰める。高さは絵と同じにして、縦には繰り返さない</summary>
        private static SpriteRenderer CreateTiled(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, int sortingOrder)
        {
            SpriteRenderer renderer = CreateSprite(parent, name, sprite, sortingOrder);
            renderer.transform.localPosition = position;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = size;
            return renderer;
        }

        /// <summary>HPバー用の単色の四角。PlaceholderSprite が実行時に 1x1 の四角を貼るので、大きさはスケールで決める</summary>
        private static Transform CreateBox(Transform parent, string name, Vector2 localPosition, Vector2 size, Color color, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            obj.AddComponent<PlaceholderSprite>();
            return obj.transform;
        }
    }
}
