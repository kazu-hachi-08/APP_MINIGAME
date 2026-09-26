using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// プレイヤーのラケット位置を管理する。
    /// タップ/ドラッグした画面位置をコート座標へ逆変換し、可動範囲内へ収めて追従させる。
    /// 角度は固定（仕様通り）で、打球そのものは PlayerSwing が担当する。
    /// </summary>
    public class RacketController : MonoBehaviour, ICourtActor
    {
        [SerializeField] private TableLayout _table;
        [SerializeField] private FlickInput _flickInput;
        [SerializeField] private Camera _camera;
        [SerializeField] private SpriteRenderer _renderer;

        [Header("Position")]
        [Tooltip("ラケットが置かれる奥行き。台の手前端よりさらに手前に置く")]
        [SerializeField] private float _racketZ = -1.7f;

        [Header("Move Range (m)")]
        [SerializeField] private float _rangeX = 1.1f;
        [SerializeField] private float _minHeight = -0.15f;
        [SerializeField] private float _maxHeight = 1.35f;

        [Tooltip("指の位置へ追いつく速さ。大きいほど機敏になる")]
        [SerializeField] private float _followSpeed = 30f;

        [Tooltip("手前端にいるときの表示直径（ワールド単位）")]
        [SerializeField] private float _displaySizeAtNear = 0.85f;

        [Header("Touch")]
        [Tooltip("タッチ操作時、指でラケットとボールが隠れないよう指より上へずらす量（画面高さ比）")]
        [SerializeField] private float _touchOffsetRatio = 0.06f;

        public float RacketZ => _racketZ;

        public Vector3 CourtPosition => new Vector3(_current.x, _current.y, _racketZ);

        private Vector2 _current;
        private Vector2 _target;
        private float _spriteUnitSize = 1f;

        /// <summary>選手の能力による追従速度の倍率</summary>
        private float _followSpeedMultiplier = 1f;

        /// <summary>相手の必殺技による足止めの残り時間と、その間の追従速度の倍率</summary>
        private float _stunTimer;
        private float _stunMoveMultiplier = 1f;

        private void Awake()
        {
            _current = new Vector2(0f, 0.25f);
            _target = _current;
        }

        private void Start()
        {
            UpdateSpriteUnitSize();
            ApplyView();
        }

        /// <summary>選んだ選手の能力を反映する（試合開始前に呼ぶ）</summary>
        public void SetFollowSpeedMultiplier(float multiplier)
        {
            _followSpeedMultiplier = multiplier;
        }

        /// <summary>相手の必殺技で、指定秒数だけ指への追従を鈍らせる</summary>
        public void Stun(float duration, float moveMultiplier)
        {
            _stunTimer = duration;
            _stunMoveMultiplier = moveMultiplier;
        }

        /// <summary>選んだラケットの見た目に差し替える</summary>
        public void SetSprite(Sprite sprite)
        {
            if (_renderer == null || sprite == null) return;

            _renderer.sprite = sprite;
            UpdateSpriteUnitSize();
        }

        private void UpdateSpriteUnitSize()
        {
            if (_renderer != null && _renderer.sprite != null)
            {
                _spriteUnitSize = Mathf.Max(0.0001f, _renderer.sprite.bounds.size.x);
            }
        }

        private void OnEnable()
        {
            if (_flickInput != null)
            {
                _flickInput.OnPointerDragged += MoveToScreenPoint;
            }
        }

        private void OnDisable()
        {
            if (_flickInput != null)
            {
                _flickInput.OnPointerDragged -= MoveToScreenPoint;
            }
        }

        /// <summary>タップ/ドラッグ位置へラケットの目標位置を移す</summary>
        public void MoveToScreenPoint(Vector2 screenPoint)
        {
            if (IsTouching())
            {
                screenPoint.y += Screen.height * _touchOffsetRatio;
            }

            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, 0f));
            Vector3 court = _table.Unproject(world, _racketZ);

            _target = new Vector2(
                Mathf.Clamp(court.x, -_rangeX, _rangeX),
                Mathf.Clamp(court.y, _minHeight, _maxHeight));
        }

        private void Update()
        {
            // フレームレートに依存しない指数補間で目標位置へ寄せる
            float stunScale = 1f;
            if (_stunTimer > 0f)
            {
                _stunTimer -= Time.deltaTime;
                stunScale = _stunMoveMultiplier;
            }

            float t = 1f - Mathf.Exp(-_followSpeed * _followSpeedMultiplier * stunScale * Time.deltaTime);
            _current = Vector2.Lerp(_current, _target, t);
            ApplyView();
        }

        /// <summary>指で操作中かどうか（PCのマウス操作ではオフセットを掛けない）</summary>
        private static bool IsTouching()
        {
            return Touchscreen.current != null && Touchscreen.current.press.isPressed;
        }

        private void ApplyView()
        {
            float depthScale = _table.ScaleAt(_racketZ) / _table.NearScale;
            float size = _displaySizeAtNear * depthScale / _spriteUnitSize;

            transform.position = _table.Project(CourtPosition);
            transform.localScale = new Vector3(size, size, 1f);
        }
    }
}
