using MiniGame.Common.Audio;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// ボールの物理挙動・ドリブル保持・リセット処理
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Ball : MonoBehaviour
    {
        // パス・シュートで別々のSE素材を用意せず、ピッチだけ変えて蹴り分けを表現する
        private const float PassSePitch = 1.2f;
        private const float ShootSePitch = 0.85f;

        [Header("Kick SE")]
        [SerializeField] private float _shootSpeedThreshold = 10f;

        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private Collider2D _holder;

        public Vector2 Position => _rigidbody.position;
        public Vector2 Velocity => _rigidbody.linearVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
        }

        /// <summary>
        /// ゴール後などにボールを指定位置へ停止させて戻す
        /// </summary>
        public void ResetBall(Vector2 position)
        {
            Release();
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.position = position;
        }

        public bool IsHeldBy(Collider2D holder)
        {
            return holder != null && _holder == holder;
        }

        /// <summary>
        /// ドリブルの保持を開始する。保持者の体に当たって弾かれると足元に留まらないため、保持中だけ衝突を切る
        /// </summary>
        public void Hold(Collider2D holder)
        {
            if (_holder == holder) return;

            Release();
            _holder = holder;
            Physics2D.IgnoreCollision(_collider, _holder, true);
        }

        public void Release()
        {
            if (_holder == null) return;

            Physics2D.IgnoreCollision(_collider, _holder, false);
            _holder = null;
        }

        /// <summary>
        /// 保持中に足元の目標位置へ運ぶ。位置を直接書き換えず速度で動かし、壁との衝突判定を残す
        /// </summary>
        public void MoveHeldTo(Vector2 targetPosition)
        {
            _rigidbody.linearVelocity = (targetPosition - _rigidbody.position) / Time.fixedDeltaTime;
        }

        /// <summary>
        /// パス・シュート用に、指定方向へ一定の速度でボールを蹴る
        /// </summary>
        public void Kick(Vector2 direction, float speed)
        {
            if (direction.sqrMagnitude < 0.0001f) return;
            Release(); // 誰が蹴っても保持は解除される（AIが蹴った＝奪われた扱い）
            _rigidbody.linearVelocity = direction.normalized * speed;
            PlayKickSe(speed);
        }

        /// <summary>
        /// プレイヤー・AIどちらのキックもこのクラスを通るため、SEはここ1箇所で鳴らす
        /// </summary>
        private void PlayKickSe(float speed)
        {
            if (!AudioManager.HasInstance) return;

            float pitch = speed >= _shootSpeedThreshold ? ShootSePitch : PassSePitch;
            AudioManager.Instance.PlaySe(SeId.Kick, pitch);
        }
    }
}
