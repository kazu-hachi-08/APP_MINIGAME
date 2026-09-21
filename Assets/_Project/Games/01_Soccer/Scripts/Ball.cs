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
    }
}
