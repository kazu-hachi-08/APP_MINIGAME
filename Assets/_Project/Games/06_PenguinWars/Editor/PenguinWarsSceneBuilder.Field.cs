using UnityEngine;

namespace MiniGame.PenguinWars.Editor
{
    /// <summary>カメラと戦場（地面・左右の城）。見た目は Phase 6 でドット絵に差し替えるまでの仮の四角</summary>
    public static partial class PenguinWarsSceneBuilder
    {
        private const float CameraZ = -10f;
        // 地面の上端が Y=0（ユニットが立つ高さ）。下部の出撃ボタンが地面に重なるよう、カメラを少し上に向ける
        private const float CameraY = 1.5f;
        private const float GroundDepth = 6f;
        // カメラの端の余白より広く敷き、どこまでスクロールしても地面が切れないようにする
        private const float GroundOverhang = 6f;

        private static readonly Vector2 CastleSize = new Vector2(3f, 4f);
        private const float HpBarOffsetY = 4.6f;
        private static readonly Vector2 HpBarSize = new Vector2(2.6f, 0.3f);

        private const int GroundSortingOrder = 0;
        private const int CastleSortingOrder = 1;
        // ユニット（Battle.cs の 5〜7）より手前に出す
        private const int HpBarBackSortingOrder = 10;

        private static readonly Color SkyColor = new Color(0.55f, 0.75f, 0.95f);
        private static readonly Color GroundColor = new Color(0.92f, 0.95f, 1f);
        private static readonly Color LeftCastleColor = new Color(0.55f, 0.8f, 1f);
        private static readonly Color RightCastleColor = new Color(0.9f, 0.35f, 0.35f);
        private static readonly Color HpBarBackColor = new Color(0.15f, 0.15f, 0.2f);
        private static readonly Color HpBarFillColor = new Color(0.3f, 0.9f, 0.4f);

        /// <summary>orthographicSize は BattleCamera が画面の横幅から毎フレーム決めるので、ここでは設定しない</summary>
        private static BattleCamera CreateCamera()
        {
            var cameraObj = new GameObject("Main Camera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;
            camera.orthographic = true;
            cameraObj.transform.position = new Vector3(0f, CameraY, CameraZ);
            cameraObj.AddComponent<AudioListener>();
            cameraObj.tag = "MainCamera";

            var battleCamera = cameraObj.AddComponent<BattleCamera>();
            SetRefs(battleCamera, ("_camera", camera));
            return battleCamera;
        }

        private static (CastleView left, CastleView right) CreateField(float fieldLength)
        {
            var fieldRoot = new GameObject("Field").transform;
            CreateBox(fieldRoot, "Ground",
                new Vector2(fieldLength * 0.5f, -GroundDepth * 0.5f),
                new Vector2(fieldLength + GroundOverhang * 2f, GroundDepth),
                GroundColor, GroundSortingOrder);

            CastleView left = CreateCastle(fieldRoot, "Castle_Left", 0f, LeftCastleColor);
            CastleView right = CreateCastle(fieldRoot, "Castle_Right", fieldLength, RightCastleColor);
            return (left, right);
        }

        /// <summary>城の足元（X, 0）を原点にする。ユニットとの距離を X だけで比べるため</summary>
        private static CastleView CreateCastle(Transform parent, string name, float x, Color color)
        {
            var castleObj = new GameObject(name);
            castleObj.transform.SetParent(parent, false);
            castleObj.transform.localPosition = new Vector3(x, 0f, 0f);

            CreateBox(castleObj.transform, "Body", new Vector2(0f, CastleSize.y * 0.5f), CastleSize, color, CastleSortingOrder);

            HpBarView hpBar = CreateHpBar(castleObj.transform, HpBarOffsetY, HpBarSize, HpBarBackSortingOrder);

            var castleView = castleObj.AddComponent<CastleView>();
            SetRefs(castleView, ("_hpBar", hpBar));
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

        /// <summary>PlaceholderSprite が実行時に 1x1 の四角を貼るので、大きさはスケールで決める</summary>
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
