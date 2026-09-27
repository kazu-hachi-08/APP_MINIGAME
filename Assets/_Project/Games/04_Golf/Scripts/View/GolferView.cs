using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 背後視点で打つ人を見せる（背後視点 Phase B〜C）。表示だけの責任で、ショットの判定には関わらない。
    /// 地面（XY 平面）に寝かさず Z 方向へ立て、打つ方向に正対させる。カメラへ向けるビルボードより、
    /// 背後視点のカメラと同じ向きに揃えた方が背中が歪まず安定して見える。
    /// 真上視点では立てたスプライトが線にしか見えないので、狙っている間とフォロースルーの間だけ表示する。
    /// </summary>
    public class GolferView : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private ShotInput _input;
        [SerializeField] private AimGuideView _aimGuide;

        [Tooltip("胴と脚。プレイヤー色を掛けて服の色にする")]
        [SerializeField] private SpriteRenderer _body;

        [Tooltip("頭（後ろ姿なので髪の色）")]
        [SerializeField] private SpriteRenderer _head;

        [Tooltip("クラブ。手元を回転の中心にするため、親（手元）の下に置く")]
        [SerializeField] private SpriteRenderer _club;

        [Tooltip("ボールから打つ方向の左へずらす距離（右打ちの立ち位置）。ボールが体に隠れないだけ離す")]
        [SerializeField] private float _sideOffset = 0.6f;

        [Tooltip("ボールより後ろへ下げる距離。0でボールの真横")]
        [SerializeField] private float _backOffset = 0.1f;

        [Tooltip("立ち絵の高さ（ユニット）。ボールの直径 0.35 に対して人らしく見える大きさ")]
        [SerializeField] private float _height = 1.2f;

        [Tooltip("胴の高さを1としたときの頭の中心の高さ")]
        [SerializeField] private float _headHeight = 1.1f;

        [Tooltip("胴の高さを1としたときの頭の直径")]
        [SerializeField] private float _headDiameter = 0.34f;

        [SerializeField] private Color _hairColor = new Color(0.22f, 0.15f, 0.1f);

        [Header("スイング（Phase C）")]
        [Tooltip("胴の高さを1としたときの手元の位置。クラブはここを中心に回る")]
        [SerializeField] private Vector2 _handPosition = new Vector2(0.12f, 0.45f);

        [Tooltip("胴の高さを1としたときのクラブの長さ。構えたときにヘッドがボールの横に届く長さ")]
        [SerializeField] private float _clubLength = 0.55f;

        [SerializeField] private float _clubWidth = 0.04f;
        [SerializeField] private Color _clubColor = new Color(0.8f, 0.8f, 0.85f);

        [Tooltip("クラブの角度（度）。0で真下、正でボール側（右）へ振り上がる")]
        [SerializeField] private float _addressAngle = 35f;

        [Tooltip("テイクバックの頂点の角度。ゲージのマーカーが右端のときこの角度になる")]
        [SerializeField] private float _topAngle = 160f;

        [Tooltip("フォロースルーの角度。負で左肩の上へ振り抜く")]
        [SerializeField] private float _followThroughAngle = -150f;

        [Tooltip("打ってからフォロースルーを見せる時間（秒）。この間は背後視点のまま。見えにくければ 0 で即真上視点")]
        [SerializeField] private float _followThroughSeconds = 0.3f;

        private float _followThroughEndTime = float.NegativeInfinity;

        /// <summary>背後視点を続ける間 true（狙っている間＋打った後のフォロースルー）。カメラもこれで切り替える</summary>
        public bool IsShowing => _aimGuide.IsShowing || IsFollowingThrough;

        /// <summary>ゴルファーが構えているボールの位置。打った後もフォロースルーの間は打った位置に留める</summary>
        public Vector2 AddressPosition => IsFollowingThrough ? _ball.LaunchPosition : _ball.GroundPosition;

        private bool IsFollowingThrough => Time.time < _followThroughEndTime;

        private void Awake()
        {
            _body.sprite = GolfShapeSprites.GolferBody;
            _head.sprite = GolfShapeSprites.Circle;
            _head.color = _hairColor;
            _head.transform.localPosition = Vector3.up * _headHeight;
            _head.transform.localScale = Vector3.one * _headDiameter;
            SetUpClub();
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
            _body.color = color;
        }

        /// <summary>1ユニットの正方形をクラブの形に伸ばし、上端が手元（親の原点）に来るように下へずらす</summary>
        private void SetUpClub()
        {
            _club.sprite = GolfShapeSprites.Square;
            _club.color = _clubColor;
            _club.transform.localPosition = Vector3.down * (_clubLength * 0.5f);
            _club.transform.localScale = new Vector3(_clubWidth, _clubLength, 1f);
            _club.transform.parent.localPosition = _handPosition;
        }

        /// <summary>オンライン相手の手番はゲージが届かないが、打った瞬間はここに来るので構え → フォロースルーは必ず見せられる</summary>
        private void OnLaunched()
        {
            _followThroughEndTime = Time.time + _followThroughSeconds;
        }

        private void LateUpdate()
        {
            bool visible = IsShowing;
            _body.enabled = visible;
            _head.enabled = visible;
            _club.enabled = visible;
            if (!visible) return;

            Vector2 dir = _input.Direction;
            Vector2 left = new Vector2(-dir.y, dir.x);
            transform.position = AddressPosition + left * _sideOffset - dir * _backOffset;
            // 前＝打つ方向、上＝空（-Z）。カメラと同じ規則なので、スプライトの右が画面の右になる
            transform.rotation = Quaternion.LookRotation(dir, Vector3.back);
            _club.transform.parent.localRotation = Quaternion.Euler(0f, 0f, ClubAngle());
        }

        /// <summary>
        /// テイクバック中はゲージのマーカーに合わせて振り上げ・振り下ろす。
        /// パワーを決めて折り返すとマーカーが戻るので、そのままダウンスイングに見える。NPC も同じゲージを動かすので同じ演出になる
        /// </summary>
        private float ClubAngle()
        {
            if (IsFollowingThrough) return _followThroughAngle;

            ShotGauge gauge = _input.Gauge;
            if (gauge.IsSwinging) return Mathf.Lerp(_addressAngle, _topAngle, gauge.Marker);
            return _addressAngle;
        }
    }
}
