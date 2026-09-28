using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 手番ではない人のボールの表示。ボール同士は当たらないので、止まっている位置に描くだけにする。
    /// 人数は試合ごとに変わるので、最大人数ぶんを最初に作っておき、使わない分は隠す。
    /// </summary>
    public class OtherBallsView : MonoBehaviour
    {
        [SerializeField] private GolfGameManager _manager;

        [Tooltip("ボールの直径（ユニット）。BallView と揃える")]
        [SerializeField] private float _diameter = 0.35f;

        [Tooltip("プレイヤー色の縁の太さ（直径に対する倍率）")]
        [SerializeField] private float _ringScale = 1.35f;

        // 手番のボール（BallView）より下に描く
        [SerializeField] private int _sortingOrder = 15;

        private readonly List<Transform> _balls = new List<Transform>();

        private void Awake()
        {
            for (int i = 0; i < GolfSetupPanel.MaxPlayers; i++)
            {
                _balls.Add(CreateBall(i));
            }
        }

        private void LateUpdate()
        {
            IReadOnlyList<GolfPlayerSlot> slots = _manager.Slots;
            for (int i = 0; i < _balls.Count; i++)
            {
                bool visible = i < slots.Count && i != _manager.CurrentPlayer && !slots[i].IsFinished;
                _balls[i].gameObject.SetActive(visible);
                if (visible) _balls[i].position = new Vector2(slots[i].Position.X, slots[i].Position.Y);
            }
        }

        /// <summary>プレイヤー色の円の上にボールを重ねて、縁取りに見せる</summary>
        private Transform CreateBall(int seat)
        {
            var ball = new GameObject($"OtherBall_P{seat + 1}").transform;
            ball.SetParent(transform);
            ball.localScale = Vector3.one * _diameter;

            SpriteRenderer ring = CreateCircle("Ring", ball, GolfShapeSprites.Circle, GolfPlayerColors.Get(seat), _sortingOrder);
            ring.transform.localScale = Vector3.one * _ringScale;
            CreateCircle("Body", ball, GolfShapeSprites.Ball, Color.white, _sortingOrder + 1);

            ball.gameObject.SetActive(false);
            return ball;
        }

        private static SpriteRenderer CreateCircle(string name, Transform parent, Sprite sprite, Color color,
            int sortingOrder)
        {
            var renderer = new GameObject(name).AddComponent<SpriteRenderer>();
            renderer.transform.SetParent(parent, false);
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
