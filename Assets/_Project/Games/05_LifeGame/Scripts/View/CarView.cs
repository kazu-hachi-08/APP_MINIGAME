using System.Collections;
using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// コマ。テーマの乗り物を席の色に染め、運転手にキャラの顔を乗せる（仕様書 §3.2）。
    /// 結婚すると助手席、子供が生まれると後部座席に1人ずつ乗る（最大4人）。
    /// </summary>
    public class CarView : MonoBehaviour
    {
        private const int SortingOrderBase = 10;
        // 車体・窓や車輪・人の3枚。席ごとにずらし、下の角（席番号が大きい）の車を手前に描く
        private const int LayersPerCar = 3;
        private const float CarScale = 0.38f;
        private const float AdultFaceScale = 0.42f;
        private const float ChildFaceScale = 0.32f;

        // 同じマスに複数台いても重ならないよう、席ごとにマスの四隅へずらす
        private static readonly Vector2[] CornerOffsets =
        {
            new Vector2(-0.22f, 0.2f),
            new Vector2(0.22f, 0.2f),
            new Vector2(-0.22f, -0.2f),
            new Vector2(0.22f, -0.2f),
        };

        /// <summary>
        /// 乗り物の絵（幅16px・中心がピボット・1単位＝16px）の車内の席。運転席（右前）・助手席（左前）・後部座席2つの順。
        /// 3テーマの乗り物は LifeGameArtGenerator でどれも同じ位置に車内を描いてある
        /// </summary>
        private static readonly Vector2[] SeatPositions =
        {
            new Vector2(0.156f, 0.0625f),
            new Vector2(-0.156f, 0.0625f),
            new Vector2(-0.156f, -0.1875f),
            new Vector2(0.156f, -0.1875f),
        };

        private const int SpouseSeat = 1;
        private const int FirstChildSeat = 2;

        // 実行時に AddComponent で作るので、Inspector ではなく定数で持つ
        private const float HopHeight = 0.25f;
        private const float HopDuration = 0.45f;

        private Vector2 _offset;
        private SpriteRenderer[] _people;
        private Transform _body;

        public static CarView Create(Transform parent, int seat, LifeThemeData theme, Sprite driverFace, Sprite familyFace)
        {
            var obj = new GameObject($"Car_P{seat + 1}");
            obj.transform.SetParent(parent, false);

            var car = obj.AddComponent<CarView>();
            car._offset = CornerOffsets[seat % CornerOffsets.Length];
            car.Build(seat, theme, driverFace, familyFace);
            return car;
        }

        /// <summary>跳ねる演出で本体だけ動かすため、位置を持つルートと絵の親（Body）を分ける</summary>
        private void Build(int seat, LifeThemeData theme, Sprite driverFace, Sprite familyFace)
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            _body.localScale = new Vector3(CarScale, CarScale, 1f);

            int order = SortingOrderBase + seat * LayersPerCar;
            CreateSprite("Vehicle", theme.VehicleBody, LifeColors.Seat(seat), Vector2.zero, 1f, order);
            CreateSprite("Detail", theme.VehicleDetail, Color.white, Vector2.zero, 1f, order + 1);

            _people = new SpriteRenderer[SeatPositions.Length];
            for (int i = 0; i < SeatPositions.Length; i++)
            {
                bool isChild = i >= FirstChildSeat;
                Sprite face = i == 0 ? driverFace : familyFace;
                _people[i] = CreateSprite($"Person{i}", face, Color.white, SeatPositions[i],
                    isChild ? ChildFaceScale : AdultFaceScale, order + 2);
            }

            SetFamily(false, 0);
        }

        private SpriteRenderer CreateSprite(string name, Sprite sprite, Color color, Vector2 position, float scale, int order)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(_body, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = new Vector3(scale, scale, 1f);

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        /// <summary>運転手は常に乗せ、結婚相手と子供（後部座席の数まで）を出す</summary>
        public void SetFamily(bool married, int children)
        {
            _people[SpouseSeat].enabled = married;
            for (int i = FirstChildSeat; i < _people.Length; i++) _people[i].enabled = i - FirstChildSeat < children;
        }

        public void PlaceAt(Vector2 cellPosition)
        {
            transform.position = cellPosition + _offset;
        }

        /// <summary>隣のマスへ1マスぶん動く</summary>
        public IEnumerator StepTo(Vector2 cellPosition, float duration)
        {
            Vector2 from = transform.position;
            Vector2 to = cellPosition + _offset;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                transform.position = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }

            transform.position = to;
        }

        /// <summary>給料日・結婚などで1回跳ねる。移動の邪魔をしないよう絵だけを動かし、待たずに進める</summary>
        public void Celebrate()
        {
            StopAllCoroutines();
            StartCoroutine(Hop());
        }

        private IEnumerator Hop()
        {
            for (float t = 0f; t < HopDuration; t += Time.deltaTime)
            {
                _body.localPosition = Vector3.up * (HopHeight * Mathf.Sin(Mathf.PI * t / HopDuration));
                yield return null;
            }

            _body.localPosition = Vector3.zero;
        }
    }
}
