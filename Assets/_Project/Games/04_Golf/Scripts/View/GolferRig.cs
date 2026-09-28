using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// ゴルファーの体の部位（下半身・上半身・頭・腕・クラブ）を、クラブの角度に合わせて並べる。
    /// いつ・どの角度で振るかは GolferView が決め、ここは「その角度のときの姿勢」だけを受け持つ。
    /// 寸法はすべて背の高さを1とした値（親の Golfer が実際の背の高さに拡大する）。
    /// </summary>
    public class GolferRig : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _legs;

        [Tooltip("上半身。プレイヤー色を掛けてシャツの色にする")]
        [SerializeField] private SpriteRenderer _torso;

        [Tooltip("頭（髪）。キャラの髪の色を掛ける")]
        [SerializeField] private SpriteRenderer _head;

        [Tooltip("帽子。頭の子に置いて一緒に動かし、キャラの帽子の色を掛ける")]
        [SerializeField] private SpriteRenderer _cap;

        [SerializeField] private SpriteRenderer _leftArm;
        [SerializeField] private SpriteRenderer _rightArm;

        [Tooltip("半袖。腕の付け根に重ねてプレイヤー色にする")]
        [SerializeField] private SpriteRenderer _leftSleeve;
        [SerializeField] private SpriteRenderer _rightSleeve;

        [Tooltip("両手のグローブ。手元に1つ重ねる")]
        [SerializeField] private SpriteRenderer _glove;

        [Tooltip("クラブ。手元を回転の中心にするため、親（手元）の下に置く")]
        [SerializeField] private SpriteRenderer _club;

        [Header("体の寸法（背の高さを1として）")]
        [Tooltip("腰の高さ。上半身はここを軸に前傾する")]
        [SerializeField] private float _waistHeight = 0.42f;

        [Tooltip("腰から肩の付け根までの高さ")]
        [SerializeField] private float _shoulderHeight = 0.4f;

        [Tooltip("肩の付け根の中心からの横幅。上半身のスプライトの肩の丸みの内側に腕の付け根を置く")]
        [SerializeField] private float _shoulderHalfWidth = 0.14f;

        [Tooltip("腰から頭の中心までの高さ")]
        [SerializeField] private float _headHeight = 0.68f;

        [SerializeField] private float _headDiameter = 0.34f;

        [Tooltip("肩の中心から手元までの長さ。構えたときにクラブのヘッドが地面に届く長さ")]
        [SerializeField] private float _armLength = 0.38f;

        [SerializeField] private float _armWidth = 0.055f;

        [Tooltip("腕のうち半袖で覆う割合（肩から）")]
        [SerializeField, Range(0f, 1f)] private float _sleeveRatio = 0.35f;

        [SerializeField] private float _gloveDiameter = 0.08f;
        [SerializeField] private Color _skinColor = new Color(0.93f, 0.76f, 0.62f);
        [SerializeField] private Color _gloveColor = Color.white;

        [Header("クラブ")]
        [Tooltip("構えたときにヘッドがボールの横の地面に届く長さ")]
        [SerializeField] private float _clubLength = 0.55f;

        [SerializeField] private float _clubWidth = 0.04f;
        [SerializeField] private Color _clubColor = new Color(0.8f, 0.8f, 0.85f);

        [Tooltip("クラブの角度に対して、手元が肩の周りを回る割合。1未満にすると手首のコックでクラブの方が大きく回って見える")]
        [SerializeField, Range(0f, 1f)] private float _handArcRatio = 0.6f;

        [Header("姿勢")]
        [Tooltip("構えたときの前傾（度）。正でボール側（右）へ傾く。背中越しでも「ボールを覗き込む姿勢」に見せる")]
        [SerializeField] private float _addressLean = 8f;

        [Tooltip("フィニッシュの傾き（度）。前傾が起き上がって立つ")]
        [SerializeField] private float _finishLean = -4f;

        [Tooltip("トップ・フィニッシュで上半身の横幅をどれだけ縮めるか。肩が回って横を向いたように見せる")]
        [SerializeField, Range(0f, 0.6f)] private float _shoulderTurn = 0.3f;

        [Tooltip("トップで腰がどれだけ回るか。肩より小さくして捻転（肩と腰の回り方の差）を見せる")]
        [SerializeField, Range(0f, 0.6f)] private float _hipTurnAtTop = 0.08f;

        [Tooltip("フィニッシュで腰がどれだけ回るか。振り抜くと腰も目標へ向き切る")]
        [SerializeField, Range(0f, 0.6f)] private float _hipTurnAtFinish = 0.25f;

        [Tooltip("フィニッシュで頭を持ち上げる量。打球を目で追う動き")]
        [SerializeField] private float _finishHeadLift = 0.05f;

        private SpriteRenderer[] _parts;

        private void Awake()
        {
            _parts = new[] { _legs, _torso, _head, _cap, _leftArm, _rightArm, _leftSleeve, _rightSleeve, _glove, _club };

            _legs.sprite = GolfShapeSprites.GolferLegs;
            _torso.sprite = GolfShapeSprites.GolferTorso;
            _head.sprite = GolfShapeSprites.GolferHead;
            _head.transform.localScale = Vector3.one * _headDiameter;
            _cap.sprite = GolfShapeSprites.GolferCap;

            SetUpLimb(_leftArm, _skinColor);
            SetUpLimb(_rightArm, _skinColor);
            SetUpLimb(_leftSleeve, Color.white);
            SetUpLimb(_rightSleeve, Color.white);
            _glove.sprite = GolfShapeSprites.Circle;
            _glove.color = _gloveColor;
            _glove.transform.localScale = Vector3.one * _gloveDiameter;
            SetUpClub();
        }

        public void SetVisible(bool visible)
        {
            foreach (SpriteRenderer part in _parts) part.enabled = visible;
        }

        /// <summary>シャツと袖を手番の人の色にする</summary>
        public void SetPlayerColor(Color color)
        {
            _torso.color = color;
            _leftSleeve.color = color;
            _rightSleeve.color = color;
        }

        /// <summary>帽子と髪をキャラの色にする。シャツはプレイヤー色のままなので、同じキャラ同士でも区別できる</summary>
        public void SetCharacterColors(Color cap, Color hair)
        {
            _cap.color = cap;
            _head.color = hair;
        }

        /// <param name="clubAngle">クラブの角度（度）。0で真下、正でボール側へ振り上がる</param>
        /// <param name="backswing">構え0 → トップ1</param>
        /// <param name="followThrough">構え0 → フィニッシュ1</param>
        public void ApplyPose(float clubAngle, float backswing, float followThrough)
        {
            float lean = Mathf.Lerp(_addressLean, _finishLean, followThrough);
            Quaternion tilt = Quaternion.Euler(0f, 0f, -lean);
            float shoulderScale = 1f - Mathf.Max(backswing, followThrough) * _shoulderTurn;
            float hipScale = 1f - (backswing * _hipTurnAtTop + followThrough * _hipTurnAtFinish);
            Vector3 waist = Vector3.up * _waistHeight;

            // 足は地面に着いたまま、腰から上だけを傾ける
            _legs.transform.localScale = new Vector3(hipScale, 1f, 1f);
            _torso.transform.localPosition = waist;
            _torso.transform.localRotation = tilt;
            _torso.transform.localScale = new Vector3(shoulderScale, 1f, 1f);
            _head.transform.localPosition = waist + tilt * (Vector3.up * _headHeight)
                + Vector3.up * (followThrough * _finishHeadLift);

            Vector3 shoulderCenter = waist + tilt * (Vector3.up * _shoulderHeight);
            Vector3 hand = shoulderCenter + Quaternion.Euler(0f, 0f, clubAngle * _handArcRatio) * (Vector3.down * _armLength);
            PlaceArms(shoulderCenter, tilt * (Vector3.right * (_shoulderHalfWidth * shoulderScale)), hand);

            _club.transform.parent.localPosition = hand;
            _club.transform.parent.localRotation = Quaternion.Euler(0f, 0f, clubAngle);
        }

        /// <summary>両肩から手元へ腕を伸ばす。肩が回って縮んだ分だけ付け根も内側へ寄る</summary>
        private void PlaceArms(Vector3 shoulderCenter, Vector3 shoulderSide, Vector3 hand)
        {
            Vector3 leftShoulder = shoulderCenter - shoulderSide;
            Vector3 rightShoulder = shoulderCenter + shoulderSide;
            PlaceLimb(_leftArm, leftShoulder, hand);
            PlaceLimb(_rightArm, rightShoulder, hand);
            PlaceLimb(_leftSleeve, leftShoulder, Vector3.Lerp(leftShoulder, hand, _sleeveRatio));
            PlaceLimb(_rightSleeve, rightShoulder, Vector3.Lerp(rightShoulder, hand, _sleeveRatio));
            _glove.transform.localPosition = hand;
        }

        /// <summary>1ユニットの正方形を、2点を結ぶ細長い棒に伸ばす</summary>
        private void PlaceLimb(SpriteRenderer limb, Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            limb.transform.localPosition = (from + to) * 0.5f;
            limb.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
            limb.transform.localScale = new Vector3(_armWidth, delta.magnitude, 1f);
        }

        private static void SetUpLimb(SpriteRenderer limb, Color color)
        {
            limb.sprite = GolfShapeSprites.Square;
            limb.color = color;
        }

        /// <summary>1ユニットの正方形をクラブの形に伸ばし、上端が手元（親の原点）に来るように下へずらす</summary>
        private void SetUpClub()
        {
            _club.sprite = GolfShapeSprites.Square;
            _club.color = _clubColor;
            _club.transform.localPosition = Vector3.down * (_clubLength * 0.5f);
            _club.transform.localScale = new Vector3(_clubWidth, _clubLength, 1f);
        }
    }
}
