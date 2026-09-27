using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 方向の線と着地予測の円（§7.3）。選んでいるクラブをフルパワー・まっすぐで打った着地点を示し、
    /// パターのときは円を出さず、転がる距離の目安を線の長さで見せる。風と曲がりは予測に含めない（読むのがプレイヤーの仕事）。
    /// </summary>
    public class AimGuideView : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private ShotInput _input;
        [SerializeField] private ClubSelector _clubs;
        [SerializeField] private SpriteRenderer _line;
        [SerializeField] private SpriteRenderer _landingMarker;

        [Tooltip("方向の線の太さ（ユニット）")]
        [SerializeField] private float _lineWidth = 0.06f;

        [Tooltip("着地予測の円の直径（ユニット）")]
        [SerializeField] private float _landingMarkerDiameter = 0.8f;

        /// <summary>方向の線を出している（狙っている）間 true</summary>
        public bool IsShowing { get; private set; }

        /// <summary>方向の線の先（着地予測の位置、パターは止まる位置）</summary>
        public Vector2 TargetPoint { get; private set; }

        private void Awake()
        {
            _line.sprite = GolfShapeSprites.Square;
            _landingMarker.sprite = GolfShapeSprites.Circle;
            _landingMarker.transform.localScale = Vector3.one * _landingMarkerDiameter;
        }

        private void LateUpdate()
        {
            // 飛んでいる間は狙いが変わらないので消して、ボールを見やすくする
            bool visible = !_ball.IsMoving && !_ball.IsInCup;
            _line.enabled = visible;
            _landingMarker.enabled = visible && !_clubs.Current.IsPutter;
            IsShowing = visible;
            if (!visible) return;

            Vector2 start = _ball.GroundPosition;
            Vector2 end = _ball.PredictFullPower(_clubs.Current.Config, _input.Direction);
            DrawLine(start, end);
            _landingMarker.transform.position = end;
            TargetPoint = end;
        }

        /// <summary>1ユニットの正方形を、始点と終点の間に伸ばして回す</summary>
        private void DrawLine(Vector2 start, Vector2 end)
        {
            Vector2 delta = end - start;
            Transform lineTransform = _line.transform;
            lineTransform.position = (start + end) * 0.5f;
            lineTransform.rotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, delta));
            lineTransform.localScale = new Vector3(_lineWidth, delta.magnitude, 1f);
        }
    }
}
