using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MiniGame.Golf
{
    /// <summary>
    /// 方向・クラブ・ゲージの入力をまとめて ShotRequest にする（§7.1、§7.6）。
    /// 方向は画面の左右ドラッグと◀▶ボタン、ゲージはゲージのボタンで開始して以降は画面のどこをタップしてもよい。
    /// マウスでもタッチでも同じ操作になるよう、Input System の Pointer で読む。
    /// </summary>
    public class ShotInput : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private ClubSelector _clubs;
        [SerializeField] private GolfPhysicsSettings _settings;
        [SerializeField] private HoldButton _rotateLeftButton;
        [SerializeField] private HoldButton _rotateRightButton;

        [Tooltip("画面の横幅ぶんドラッグしたときに回る角度（度）")]
        [SerializeField] private float _dragDegreesPerScreenWidth = 120f;

        [Tooltip("◀▶ボタンを押している間に回る速さ（度/秒）。細かく合わせる用なのでゆっくり")]
        [SerializeField] private float _buttonDegreesPerSecond = 20f;

        private bool _dragging;
        private float _lastPointerX;

        /// <summary>打つ方向（地面の平面、長さ1）</summary>
        public Vector2 Direction { get; private set; } = Vector2.up;

        public ShotGauge Gauge { get; private set; }

        /// <summary>かけるスピン。構え直すたびに「なし」へ戻し、前の人・前のショットの設定を引き継がない</summary>
        public ShotSpin Spin { get; private set; }

        /// <summary>方向とクラブを変えられる（ボールが止まっていて、ゲージを操作していない）</summary>
        public bool CanAim => CanShoot && Gauge.State == ShotGauge.GaugeState.Idle;

        // 手番でないとき（「○○の番」表示中など）は GolfGameManager がこのコンポーネントを無効にする
        private bool CanShoot => enabled && !_ball.IsMoving && !_ball.IsInCup;

        private void Awake()
        {
            Gauge = new ShotGauge(_settings.GaugeSpeed, _settings.ImpactZoneCenter, _settings.ImpactZoneHalfWidth);
        }

        /// <summary>ゲージのボタンから呼ぶ（①タップ）</summary>
        public void BeginSwing()
        {
            if (!CanAim) return;

            _dragging = false;
            Gauge.Begin();
        }

        private void Update()
        {
            if (!CanShoot) return;

            if (Gauge.IsSwinging)
            {
                UpdateSwing();
            }
            else if (CanAim)
            {
                UpdateAim();
            }
        }

        /// <summary>手番の人のボールを置いたら、カップの方向・距離に合うクラブで構え直す（§7.2、§7.5）。ゾーンの幅はライで変える（§7.4）</summary>
        public void PrepareShot()
        {
            Gauge.Reset();
            Gauge.SetZoneScale(_ball.ImpactZoneRate);
            _dragging = false;
            Spin = ShotSpin.None;

            Vector2 toCup = _ball.CupPosition - _ball.GroundPosition;
            if (toCup.sqrMagnitude > 0f) Direction = toCup.normalized;

            _clubs.SelectDefault();
        }

        private void UpdateSwing()
        {
            Pointer pointer = ResolvePointer();
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                Gauge.Tap();
            }

            // タップで終わらなかったときは、時間で進めて左端まで戻ったら打つ
            if (Gauge.IsSwinging) Gauge.Tick(Time.deltaTime);

            if (Gauge.State == ShotGauge.GaugeState.Finished) HitWithGauge();
        }

        /// <summary>今の方向・クラブ・ゲージの結果で打つ。NPC もゲージを動かし終えたらここから打つ</summary>
        public void HitWithGauge()
        {
            _ball.Hit(Direction, _clubs.Current.Config, Gauge.Power, Gauge.ImpactOffset, Spin);
        }

        /// <summary>スピンのボタンから呼ぶ。なし → バック → トップ → なし の順に切り替える</summary>
        public void CycleSpin()
        {
            if (!CanAim) return;

            Spin = Spin switch
            {
                ShotSpin.None => ShotSpin.Back,
                ShotSpin.Back => ShotSpin.Top,
                _ => ShotSpin.None,
            };
        }

        /// <summary>NPC が狙った方向を線で見せるために使う</summary>
        public void SetDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0f) Direction = direction.normalized;
        }

        private void UpdateAim()
        {
            float degrees = ButtonRotation() + DragRotation();
            if (degrees != 0f) Rotate(degrees);
        }

        /// <summary>◀で左回り（反時計回り）、▶で右回り</summary>
        private float ButtonRotation()
        {
            float input = (_rotateLeftButton.IsHeld ? 1f : 0f) - (_rotateRightButton.IsHeld ? 1f : 0f);
            return input * _buttonDegreesPerSecond * Time.deltaTime;
        }

        /// <summary>右へドラッグすると右へ回る。ボタンなどUIの上から始めたドラッグは無視する</summary>
        private float DragRotation()
        {
            Pointer pointer = ResolvePointer();
            if (pointer == null) return 0f;

            float x = pointer.position.ReadValue().x;
            if (pointer.press.wasPressedThisFrame)
            {
                _dragging = !IsPointerOverUI(pointer);
                _lastPointerX = x;
                return 0f;
            }

            if (!_dragging) return 0f;

            if (!pointer.press.isPressed)
            {
                _dragging = false;
                return 0f;
            }

            float deltaX = x - _lastPointerX;
            _lastPointerX = x;
            return -deltaX / Screen.width * _dragDegreesPerScreenWidth;
        }

        private void Rotate(float degrees)
        {
            Direction = (Quaternion.Euler(0f, 0f, degrees) * Direction).normalized;
        }

        /// <summary>触られている間はタッチを優先し、PCではマウスを使う。タッチは主タッチ（最初の指）だけを見る</summary>
        private static Pointer ResolvePointer()
        {
            Touchscreen touchscreen = Touchscreen.current;
            bool touching = touchscreen != null
                && (touchscreen.press.isPressed || touchscreen.press.wasReleasedThisFrame);

            return touching ? touchscreen : Pointer.current;
        }

        private static bool IsPointerOverUI(Pointer pointer)
        {
            if (EventSystem.current == null) return false;

            // タッチは指のIDを渡さないと正しく判定できない
            if (pointer is Touchscreen touchscreen)
            {
                return EventSystem.current.IsPointerOverGameObject(touchscreen.primaryTouch.touchId.ReadValue());
            }

            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
