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
        [SerializeField] private MolkkyPhysicsSettings _settings;

        private Rigidbody2D _body;
        private float _throwTime;
        private float _peakHeight;

        public bool IsThrown { get; private set; }
        public Vector2 GroundPosition => _body.position;
        public float RotationDegrees => _body.rotation;
        public float Speed => IsThrown ? _body.linearVelocity.magnitude : 0f;

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
            _body.rotation = 0f;
            transform.SetPositionAndRotation(_body.position, Quaternion.identity);

            IsThrown = false;
        }

        public void Throw(ThrowRequest request)
        {
            PlaceOnLine(request.PositionX);

            _body.bodyType = RigidbodyType2D.Dynamic;
            // 棒は進行方向に対して横向きで飛ぶ（§8.2）
            _body.rotation = -request.AngleDegrees;
            _body.linearVelocity = request.Direction * request.Speed;

            _throwTime = Time.time;
            _peakHeight = _settings.StickPeakHeight * request.Speed / _settings.MaxThrowSpeed;
            IsThrown = true;
        }
    }
}
