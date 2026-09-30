using System.Collections;
using UnityEngine;

namespace MiniGame.LifeGame
{
    /// <summary>コマ（仮図形：席の色の丸）。フェーズ6で車の絵に置き換える</summary>
    public class CarView : MonoBehaviour
    {
        private const int SortingOrder = 5;
        private const float Diameter = 0.5f;

        // 同じマスに複数台いても重ならないよう、席ごとにマスの四隅へずらす
        private static readonly Vector2[] SeatOffsets =
        {
            new Vector2(-0.22f, 0.22f),
            new Vector2(0.22f, 0.22f),
            new Vector2(-0.22f, -0.22f),
            new Vector2(0.22f, -0.22f),
        };

        private Vector2 _offset;

        public static CarView Create(Transform parent, int seat)
        {
            var obj = new GameObject($"Car_P{seat + 1}");
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = new Vector3(Diameter, Diameter, 1f);

            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = LifeShapes.Circle;
            renderer.color = LifeColors.Seat(seat);
            renderer.sortingOrder = SortingOrder;

            var car = obj.AddComponent<CarView>();
            car._offset = SeatOffsets[seat % SeatOffsets.Length];
            return car;
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
    }
}
