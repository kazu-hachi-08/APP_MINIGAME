using System;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// モルック（棒）の物理（§8.2）。ThrowRequest から初速を与える。
    /// 高さは見た目用の放物線で、当たり判定には使わない（調整を軽くするため）。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public class StickThrower : MonoBehaviour
    {
        // 縦投げで棒の長軸（ローカルX）を奥（+Y）へ向ける回転。
        // 本物のモルックは縦回転で投げるため、真上から見ると棒は進行方向と平行になり、当たり幅が細く1本だけを狙える
        private const float VerticalRotation = 90f;
        private const float HorizontalRotation = 0f;

        [SerializeField] private MolkkyPhysicsSettings _settings;

        private Rigidbody2D _body;
        private float _throwTime;
        private float _peakHeight;

        public bool IsThrown { get; private set; }
        public ThrowStyle Style { get; private set; } = ThrowStyle.Horizontal;
        public Vector2 GroundPosition => _body.position;
        public float RotationDegrees => _body.rotation;
        public float Speed => IsThrown ? _body.linearVelocity.magnitude : 0f;

        /// <summary>投げた棒が何かに当たった（引数は衝突の相対速度）。当たる音の強さに使う</summary>
        public event Action<float> Hit;

        /// <summary>見た目上の高さ。投げてから StickAirTime の間だけ放物線を描く</summary>
        public float Height
        {
            get
            {
                if (!IsThrown) return 0f;

                float t = (Time.time - _throwTime) / _settings.StickAirTime;
                if (t >= 1f) return 0f;

                return 4f * _peakHeight * t * (1f - t);
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.mass = _settings.StickMass;
            _body.linearDamping = _settings.StickDamping;
            _body.angularDamping = _settings.StickAngularDamping;
            // 速い棒がピンをすり抜けないようにする
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var capsule = GetComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Horizontal;
            capsule.size = new Vector2(_settings.StickLength, _settings.StickThickness);
            capsule.sharedMaterial = new PhysicsMaterial2D("Stick") { bounciness = _settings.Bounciness, friction = 0f };

            PlaceOnLine(0f);
        }

        /// <summary>投擲ライン上に棒を戻す。狙っている間はピンに押されないよう Kinematic にしておく</summary>
        public void PlaceOnLine(float x)
        {
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;

            float clampedX = Mathf.Clamp(x, -_settings.ThrowLineHalfWidth, _settings.ThrowLineHalfWidth);
            _body.position = new Vector2(clampedX, 0f);
            float rotation = BaseRotation(Style);
            _body.rotation = rotation;
            transform.SetPositionAndRotation(_body.position, Quaternion.Euler(0f, 0f, rotation));

            IsThrown = false;
        }

        /// <summary>投げ方を変え、構えている棒をその場で向き直す</summary>
        public void SetStyle(ThrowStyle style)
        {
            Style = style;
            PlaceOnLine(_body.position.x);
        }

        public void Throw(ThrowRequest request)
        {
            Style = request.Style;
            PlaceOnLine(request.PositionX);

            _body.bodyType = RigidbodyType2D.Dynamic;
            // 縦投げは進行方向と平行、横投げは進行方向に対して横向きで飛ぶ（§8.2）
            _body.rotation = BaseRotation(Style) - request.AngleDegrees;
            _body.linearVelocity = request.Direction * request.Speed;

            _throwTime = Time.time;
            _peakHeight = _settings.StickPeakHeight * request.Speed / _settings.MaxThrowSpeed;
            IsThrown = true;
        }

        private static float BaseRotation(ThrowStyle style)
        {
            return style == ThrowStyle.Vertical ? VerticalRotation : HorizontalRotation;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsThrown) return;

            Hit?.Invoke(collision.relativeVelocity.magnitude);
        }
    }
}
