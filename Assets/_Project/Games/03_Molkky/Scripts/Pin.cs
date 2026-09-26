using System;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// ピン1本の物理と転倒判定（§8.3〜8.4）。見た目は PinView が担当し、ここは地面平面の2D物理だけを持つ。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Pin : MonoBehaviour
    {
        private Rigidbody2D _body;
        private MolkkyPhysicsSettings _settings;

        // 転倒判定は投擲中だけ行う。立て直し直後の押し合いで「倒れた」扱いにしないため
        private bool _armed;
        private Vector2 _throwStartPosition;

        public int Number { get; private set; }
        public bool IsFallen { get; private set; }

        /// <summary>倒れた向き（見た目で横倒しにする方向）</summary>
        public Vector2 FallDirection { get; private set; }

        public Vector2 GroundPosition => _body.position;
        public float Speed => _body.linearVelocity.magnitude;

        /// <summary>倒れた瞬間（倒れる音を鳴らすため）</summary>
        public event Action<Pin> Fell;

        public void Initialize(int number, MolkkyPhysicsSettings settings, PhysicsMaterial2D material)
        {
            Number = number;
            _settings = settings;

            _body = GetComponent<Rigidbody2D>();
            _body.gravityScale = 0f; // 真上から見た平面として扱う
            _body.freezeRotation = true;
            _body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var circle = GetComponent<CircleCollider2D>();
            circle.radius = settings.PinRadius;
            circle.sharedMaterial = material;

            ApplyBody(settings.PinMassStanding, settings.PinDampingStanding);
        }

        /// <summary>指定位置に立て直す</summary>
        public void StandAt(Vector2 position)
        {
            _body.position = position;
            transform.position = position;
            Stop();

            IsFallen = false;
            _armed = false;
            ApplyBody(_settings.PinMassStanding, _settings.PinDampingStanding);
        }

        public void Stop()
        {
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
        }

        public void Arm()
        {
            _throwStartPosition = _body.position;
            _armed = true;
        }

        public void Disarm()
        {
            _armed = false;
        }

        private void FixedUpdate()
        {
            if (!_armed || IsFallen) return;

            Vector2 moved = _body.position - _throwStartPosition;
            if (moved.sqrMagnitude >= _settings.FallMoveThreshold * _settings.FallMoveThreshold)
            {
                Fall(moved);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_armed || IsFallen) return;
            if (collision.relativeVelocity.magnitude < _settings.FallImpactThreshold) return;

            // 当たった点の反対側へ倒す
            Fall(_body.position - collision.GetContact(0).point);
        }

        private void Fall(Vector2 direction)
        {
            IsFallen = true;
            FallDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.up;

            // 倒れたピンは軽く・滑りやすくして、他のピンを巻き込む連鎖を起こしやすくする
            ApplyBody(_settings.PinMassFallen, _settings.PinDampingFallen);
            Fell?.Invoke(this);
        }

        private void ApplyBody(float mass, float damping)
        {
            _body.mass = mass;
            _body.linearDamping = damping;
        }
    }
}
