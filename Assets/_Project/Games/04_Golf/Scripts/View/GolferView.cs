using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 背後視点で打つ人を見せる（背後視点 Phase B〜C）。表示だけの責任で、ショットの判定には関わらない。
    /// 地面（XY 平面）に寝かさず Z 方向へ立て、打つ方向に正対させる。カメラへ向けるビルボードより、
    /// 背後視点のカメラと同じ向きに揃えた方が背中が歪まず安定して見える。
    /// 真上視点では立てたスプライトが線にしか見えないので、狙っている間とフォロースルーの間だけ表示する。
    /// ここは立ち位置と「いつ・どの角度で振るか」を決め、体の部位の並べ方は GolferRig に任せる。
    /// </summary>
    public class GolferView : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private ShotInput _input;
        [SerializeField] private AimGuideView _aimGuide;
        [SerializeField] private GolferRig _rig;

        [Tooltip("ボールから打つ方向の左へずらす距離（右打ちの立ち位置）。ボールが体に隠れないだけ離す")]
        [SerializeField] private float _sideOffset = 0.6f;

        [Tooltip("ボールより後ろへ下げる距離。0でボールの真横")]
        [SerializeField] private float _backOffset = 0.1f;

        [Tooltip("立ち絵の高さ（ユニット）。ボールの直径 0.35 に対して人らしく見える大きさ")]
        [SerializeField] private float _height = 1.2f;

        [Header("スイング（Phase C）")]
        [Tooltip("クラブの角度（度）。0で真下、正でボール側（右）へ振り上がる")]
        [SerializeField] private float _addressAngle = 35f;

        [Tooltip("テイクバックの頂点の角度。ゲージのマーカーが右端のときこの角度になる")]
        [SerializeField] private float _topAngle = 160f;

        [Tooltip("フォロースルーの角度。負で左肩の上へ振り抜く")]
        [SerializeField] private float _followThroughAngle = -150f;

        [Tooltip("打ってからフォロースルーを見せる時間（秒）。この間は背後視点のまま。見えにくければ 0 で即真上視点")]
        [SerializeField] private float _followThroughSeconds = 0.3f;

        private float _followThroughEndTime = float.NegativeInfinity;

        /// <summary>直前のフレームで描いたクラブの角度。打った瞬間の角度から振り抜くために覚えておく</summary>
        private float _currentAngle;
        private float _impactAngle;

        /// <summary>背後視点を続ける間 true（狙っている間＋打った後のフォロースルー）。カメラもこれで切り替える</summary>
        public bool IsShowing => _aimGuide.IsShowing || IsFollowingThrough;

        /// <summary>ゴルファーが構えているボールの位置。打った後もフォロースルーの間は打った位置に留める</summary>
        public Vector2 AddressPosition => IsFollowingThrough ? _ball.LaunchPosition : _ball.GroundPosition;

        private bool IsFollowingThrough => Time.time < _followThroughEndTime;

        private void Awake()
        {
            transform.localScale = Vector3.one * _height;
        }

        private void OnEnable()
        {
            _ball.Launched += OnLaunched;
        }

        private void OnDisable()
        {
            _ball.Launched -= OnLaunched;
        }

        /// <summary>手番の人の色にする。ゴルファーは1人を使い回すので、手番が替わるたびに呼ぶ</summary>
        public void SetPlayerColor(Color color)
        {
            _rig.SetPlayerColor(color);
        }

        /// <summary>オンライン相手の手番はゲージが届かないが、打った瞬間はここに来るので構え → フォロースルーは必ず見せられる</summary>
        private void OnLaunched()
        {
            _followThroughEndTime = Time.time + _followThroughSeconds;
            _impactAngle = _currentAngle;
        }

        private void LateUpdate()
        {
            bool visible = IsShowing;
            _rig.SetVisible(visible);
            if (!visible) return;

            Vector2 dir = _input.Direction;
            Vector2 left = new Vector2(-dir.y, dir.x);
            transform.position = AddressPosition + left * _sideOffset - dir * _backOffset;
            // 前＝打つ方向、上＝空（-Z）。カメラと同じ規則なので、スプライトの右が画面の右になる
            transform.rotation = Quaternion.LookRotation(dir, Vector3.back);
            _currentAngle = ClubAngle();
            _rig.ApplyPose(_currentAngle, Mathf.InverseLerp(_addressAngle, _topAngle, _currentAngle),
                Mathf.InverseLerp(_addressAngle, _followThroughAngle, _currentAngle));
        }

        /// <summary>
        /// テイクバック中はゲージのマーカーに合わせて振り上げ・振り下ろす。
        /// パワーを決めて折り返すとマーカーが戻るので、そのままダウンスイングに見える。NPC も同じゲージを動かすので同じ演出になる。
        /// マーカーは等速なので、角度への変換で緩急をつける（ゲージの速さ＝難しさは変えない）
        /// </summary>
        private float ClubAngle()
        {
            if (IsFollowingThrough) return FollowThroughAngle();

            ShotGauge gauge = _input.Gauge;
            if (gauge.State == ShotGauge.GaugeState.Rising) return BackswingAngle(gauge.Marker);
            if (gauge.State == ShotGauge.GaugeState.Returning) return DownswingAngle(gauge.Marker, gauge.Power);
            return _addressAngle;
        }

        /// <summary>始動はゆっくり、トップでも一瞬止まるように SmoothStep を通す。パワーが小さいほどトップが低い</summary>
        private float BackswingAngle(float marker)
        {
            return Mathf.Lerp(_addressAngle, _topAngle, Mathf.SmoothStep(0f, 1f, marker));
        }

        /// <summary>
        /// 折り返した位置（＝パワーを決めたトップ）から、ボールに近づくほど加速して振り下ろす。
        /// パワーを決めた瞬間のバックスイングの角度から始めるので、折り返しで角度が飛ばない
        /// </summary>
        private float DownswingAngle(float marker, float power)
        {
            if (power <= 0f) return _addressAngle;

            float top = BackswingAngle(power);
            float fromTop = 1f - Mathf.Clamp01(marker / power);
            return Mathf.Lerp(top, _addressAngle, fromTop * fromTop);
        }

        /// <summary>打った瞬間の角度から、振り抜き始めが一番速く、フィニッシュで減速して止まる（Ease-Out）</summary>
        private float FollowThroughAngle()
        {
            if (_followThroughSeconds <= 0f) return _followThroughAngle;

            float t = 1f - (_followThroughEndTime - Time.time) / _followThroughSeconds;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            return Mathf.Lerp(_impactAngle, _followThroughAngle, eased);
        }
    }
}
