using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 投擲・転倒・静止判定の調整値（§8.6）。
    /// 「狙って倒せる／たまに予想外に散らばる」のバランスはプレイしながら詰めるため、コードから切り出している。
    /// 座標は地面の平面（X＝左右／Y＝奥行きZ）で、投擲ラインが Z=0。
    /// </summary>
    [CreateAssetMenu(fileName = "MolkkyPhysicsSettings", menuName = "MiniGame/Molkky/Physics Settings")]
    public class MolkkyPhysicsSettings : ScriptableObject
    {
        [Header("Field")]
        [Tooltip("投擲ラインから初期配置の手前の列までの距離")]
        [SerializeField] private float _pinSetDistance = 3.5f;
        [Tooltip("初期配置で隣り合うピンの中心間の距離。直径より少し広くして、置いた瞬間に押し合わないようにする")]
        [SerializeField] private float _pinSpacing = 0.42f;
        [SerializeField] private float _pinRadius = 0.2f;
        [SerializeField] private float _throwLineHalfWidth = 1.5f;
        [Tooltip("立て直し時にピンを寄せる範囲（x:左右 / y:奥行き）")]
        [SerializeField] private Rect _fieldBounds = new Rect(-3f, 1.5f, 6f, 8f);

        [Header("Throw")]
        [SerializeField] private float _minThrowSpeed = 3f;
        [SerializeField] private float _maxThrowSpeed = 11f;
        [Tooltip("投擲方向の最大角度（左右・度）")]
        [SerializeField] private float _maxThrowAngle = 25f;

        [Header("Stick")]
        [SerializeField] private float _stickLength = 0.9f;
        [SerializeField] private float _stickThickness = 0.22f;
        [SerializeField] private float _stickMass = 1f;
        [SerializeField] private float _stickDamping = 0.9f;
        [SerializeField] private float _stickAngularDamping = 2f;
        [Tooltip("見た目上の滞空時間（秒）。当たり判定には影響しない")]
        [SerializeField] private float _stickAirTime = 0.7f;
        [Tooltip("最大の強さで投げたときの見た目上の最高点")]
        [SerializeField] private float _stickPeakHeight = 1.2f;

        [Header("Pin")]
        [SerializeField] private float _pinMassStanding = 1.5f;
        [SerializeField] private float _pinMassFallen = 1.2f;
        [SerializeField] private float _pinDampingStanding = 3f;
        [SerializeField] private float _pinDampingFallen = 0.9f;
        [Tooltip("ぶつかったときの跳ね返り。0だとピン同士の連鎖が起きにくい")]
        [SerializeField] private float _bounciness = 0.55f;

        [Header("Fall")]
        [SerializeField] private float _fallImpactThreshold = 1.2f;
        [SerializeField] private float _fallMoveThreshold = 0.25f;

        [Header("Settle")]
        [SerializeField] private float _settleSpeedThreshold = 0.05f;
        [SerializeField] private float _settleDuration = 0.4f;
        [SerializeField] private float _maxThrowDuration = 5f;

        public float PinSetDistance => _pinSetDistance;
        public float PinSpacing => _pinSpacing;
        public float PinRadius => _pinRadius;
        public float ThrowLineHalfWidth => _throwLineHalfWidth;
        public Rect FieldBounds => _fieldBounds;

        public float MinThrowSpeed => _minThrowSpeed;
        public float MaxThrowSpeed => _maxThrowSpeed;
        public float MaxThrowAngle => _maxThrowAngle;

        public float StickLength => _stickLength;
        public float StickThickness => _stickThickness;
        public float StickMass => _stickMass;
        public float StickDamping => _stickDamping;
        public float StickAngularDamping => _stickAngularDamping;
        public float StickAirTime => _stickAirTime;
        public float StickPeakHeight => _stickPeakHeight;

        public float PinMassStanding => _pinMassStanding;
        public float PinMassFallen => _pinMassFallen;
        public float PinDampingStanding => _pinDampingStanding;
        public float PinDampingFallen => _pinDampingFallen;
        public float Bounciness => _bounciness;

        public float FallImpactThreshold => _fallImpactThreshold;
        public float FallMoveThreshold => _fallMoveThreshold;

        public float SettleSpeedThreshold => _settleSpeedThreshold;
        public float SettleDuration => _settleDuration;
        public float MaxThrowDuration => _maxThrowDuration;
    }
}
