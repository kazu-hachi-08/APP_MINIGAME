using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// ボールの物理挙動とリセット処理（Phase 1: 選手との接触で押し出されるのみ）
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Ball : MonoBehaviour
    {
        private Rigidbody2D _rigidbody;

        public Vector2 Position => _rigidbody.position;
        public Vector2 Velocity => _rigidbody.linearVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        /// <summary>
        /// ゴール後などにボールを指定位置へ停止させて戻す
        /// </summary>
        public void ResetBall(Vector2 position)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.position = position;
        }

        /// <summary>
        /// ドリブルなど、外部からボールに力を加える
        /// </summary>
        public void ApplyForce(Vector2 force, ForceMode2D mode = ForceMode2D.Force)
        {
            _rigidbody.AddForce(force, mode);
        }

        /// <summary>
        /// パス・シュート用に、指定方向へ一定の速度でボールを蹴る
        /// </summary>
        public void Kick(Vector2 direction, float speed)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            _rigidbody.linearVelocity = direction.normalized * speed;
        }
    }
}
