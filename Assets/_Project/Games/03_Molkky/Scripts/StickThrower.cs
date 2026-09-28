using System;
using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// モルック（棒）の物理。ThrowRequest から初速を与える。
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
        private CapsuleCollider2D _capsule;
        private float _throwTime;
        private float _airTime;
        private float _peakHeight;

        // 山なりで空中にいる間。当たり判定を切っているので、着地で戻す必要がある
        private bool _isLobbing;

        // 着地までの残り物理ステップ数。Time.time で測ると投げた瞬間のフレームの位置で着地ステップが1つずれ、
        // オンラインの相手端末の再生で着地点が変わってしまうため、ステップ数で数える
        private int _lobStepsLeft;

        public bool IsThrown { get; private set; }

        /// <summary>手番のキャラの倍率を掛けた棒の長さ（地面単位）。見た目（StickView）もこの長さで描く</summary>
        public float Length { get; private set; }

        public ThrowStyle Style { get; private set; } = ThrowStyle.Horizontal;
        public Vector2 GroundPosition => _body.position;
        public float RotationDegrees => _body.rotation;
        public float Speed => IsThrown ? _body.linearVelocity.magnitude : 0f;

        /// <summary>投げた棒が何かに当たった（引数は衝突の相対速度）。当たる音の強さに使う</summary>
        public event Action<float> Hit;

        /// <summary>見た目上の高さ。投げてから滞空時間の間だけ放物線を描く</summary>
        public float Height
        {
            get
            {
                if (!IsThrown) return 0f;

                float t = (Time.time - _throwTime) / _airTime;
                if (t >= 1f) return 0f;

                // t*(1-t) は t=0.5 で最大 0.25 になるので、4倍して頂点がちょうど _peakHeight になるようにする
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

            _capsule = GetComponent<CapsuleCollider2D>();
            _capsule.direction = CapsuleDirection2D.Horizontal;
            SetLength(_settings.StickLength);
            _capsule.sharedMaterial = new PhysicsMaterial2D("Stick") { bounciness = _settings.Bounciness, friction = 0f };

            PlaceOnLine(0f);
        }

        /// <summary>投擲ライン上に棒を戻す。狙っている間はピンに押されないよう Kinematic にしておく</summary>
        public void PlaceOnLine(float x)
        {
            _body.simulated = true;
            _body.bodyType = RigidbodyType2D.Kinematic;
            StopMotion();
            _capsule.enabled = true;
            // 山なりの途中で打ち切られた（Freeze 等）場合に、減速なしのまま次の投擲へ持ち越さない
            _body.linearDamping = _settings.StickDamping;
            _isLobbing = false;

            float clampedX = Mathf.Clamp(x, -_settings.ThrowLineHalfWidth, _settings.ThrowLineHalfWidth);
            _body.position = new Vector2(clampedX, 0f);
            float rotation = BaseRotation(Style);
            _body.rotation = rotation;
            transform.SetPositionAndRotation(_body.position, Quaternion.Euler(0f, 0f, rotation));

            IsThrown = false;
        }

        /// <summary>
        /// 手番のキャラの棒の長さにする。当たり判定が変わるので、オンラインでは相手の手番でも必ず呼ぶ
        /// （揃えないと相手端末と倒れるピンが変わる）
        /// </summary>
        public void SetCharacter(MolkkyCharacterData character)
        {
            SetLength(_settings.StickLength * character.StickLengthMultiplier);
        }

        private void SetLength(float length)
        {
            Length = length;
            _capsule.size = new Vector2(Length, _settings.StickThickness);
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
            // 縦投げは進行方向と平行、横投げは進行方向に対して横向きで飛ぶ
            _body.rotation = BaseRotation(Style) - request.AngleDegrees;
            _body.linearVelocity = request.Direction * request.Speed;

            _throwTime = Time.time;
            IsThrown = true;

            if (request.Arc == ThrowArc.High)
            {
                StartLob();
            }
            else
            {
                StartLowArc(request.Speed);
            }
        }

        /// <summary>低めは見た目だけ弾ませる。強く投げたほど高く見えるよう、最高点を初速に比例させる</summary>
        private void StartLowArc(float speed)
        {
            _airTime = _settings.StickAirTime;
            _peakHeight = _settings.StickPeakHeight * speed / _settings.MaxThrowSpeed;
        }

        private void StartLob()
        {
            _airTime = _settings.LobAirTime;
            _peakHeight = _settings.LobPeakHeight;
            // 空中は減速しないため、低めと同じ初速だと約1.7倍飛んでしまう。初速を落として飛距離を揃える
            _body.linearVelocity *= _settings.LobSpeedRatio;
            // 空中では当たらないようにして、手前のピンを飛び越えさせる
            _capsule.enabled = false;
            // 空中は地面の摩擦を受けないので減速させない。着地時の減速と二重に削られて、ピンを倒せなくなるのを防ぐ
            _body.linearDamping = 0f;
            _isLobbing = true;
            _lobStepsLeft = Mathf.CeilToInt(_airTime / Time.fixedDeltaTime);
        }

        private void FixedUpdate()
        {
            if (!_isLobbing) return;

            _lobStepsLeft--;
            if (_lobStepsLeft <= 0) Land();
        }

        /// <summary>山なりの着地。上から落ちた棒は前へ滑りにくいので、速度を大きく削って落ちた場所の近くで止める</summary>
        private void Land()
        {
            _isLobbing = false;
            _capsule.enabled = true;
            _body.linearDamping = _settings.StickDamping;
            _body.linearVelocity *= _settings.LobLandingSpeedRatio;
        }

        /// <summary>
        /// その場で止めて当たり判定を外す。相手端末の結果でピンを上書きしたとき、
        /// 自分の端末で止まった棒と重なってピンが押し出されないようにするため。PlaceOnLine で元に戻る
        /// </summary>
        public void Freeze()
        {
            StopMotion();
            _body.simulated = false;
        }

        private void StopMotion()
        {
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
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
