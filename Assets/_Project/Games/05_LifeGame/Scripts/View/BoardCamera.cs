using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 手番のコマを追うカメラと「全体」表示の切り替え（仕様書 §3.1）。ピンチ操作は入れない（PCでも同じ操作にするため）。
    /// 盤面を縦にドラッグすると、離れたマスを見に行ける（コマが動き出すと追従に戻る）。
    /// </summary>
    public class BoardCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _followSize = 6f;
        [Tooltip("追う対象を画面の中央より上に出す量。下部のルーレットにコマが隠れないようにするため")]
        [SerializeField] private float _followOffsetY = -1.5f;
        [SerializeField] private float _followSharpness = 6f;
        [SerializeField] private float _overviewMargin = 1f;

        private Transform _target;
        private Rect _boardBounds;

        // ドラッグで動かしている間は追従をやめ、このY座標に留まる
        private bool _isManual;
        private float _manualY;
        private bool _pressValid;
        private bool _isDragging;
        private Vector2 _pressScreenPosition;
        private float _pressCameraY;
        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

        public bool IsOverview { get; private set; }

        private void Awake()
        {
            // マスを押して効果を見るため。シーンを編集せず実行時に付ける（2人でシーンを触って競合しないように）
            if (!_camera.TryGetComponent(out Physics2DRaycaster _)) _camera.gameObject.AddComponent<Physics2DRaycaster>();
        }

        public void SetBoardBounds(Rect bounds)
        {
            _boardBounds = bounds;
        }

        /// <summary>盤面の外はカメラの背景色で塗る。テーマの背景色にする</summary>
        public void SetBackground(Color color)
        {
            _camera.backgroundColor = color;
        }

        public void Follow(Transform target)
        {
            _target = target;
            ResumeFollow();
        }

        /// <summary>ドラッグで見に行った位置から、追従先へ戻る</summary>
        public void ResumeFollow()
        {
            _isManual = false;
        }

        public void ToggleOverview()
        {
            IsOverview = !IsOverview;
            ResumeFollow();
        }

        /// <summary>カメラを今の追従先へすぐ合わせる（試合開始直後に盤面の外から滑ってこないように）</summary>
        public void SnapToTarget()
        {
            _camera.orthographicSize = TargetSize();
            _camera.transform.position = TargetPosition();
        }

        private void Update()
        {
            Pointer pointer = ResolvePointer();
            if (pointer == null) return;

            if (pointer.press.wasPressedThisFrame) BeginPress(pointer);
            else if (pointer.press.isPressed && _pressValid) UpdateDrag(pointer);
            else if (pointer.press.wasReleasedThisFrame) EndPress();
        }

        private void BeginPress(Pointer pointer)
        {
            Vector2 position = pointer.position.ReadValue();
            // ルーレットやボタンの上で始まった操作はUIのものなので、盤面は動かさない
            _pressValid = !IsOverview && _target != null && !IsOverUI(position);
            _isDragging = false;
            _pressScreenPosition = position;
            _pressCameraY = _camera.transform.position.y;
        }

        private void EndPress()
        {
            _pressValid = false;
            _isDragging = false;
        }

        private void UpdateDrag(Pointer pointer)
        {
            float deltaY = pointer.position.ReadValue().y - _pressScreenPosition.y;
            // マスのタップと同じしきい値にして、少し指がぶれただけのタップでスクロールが始まらないようにする
            if (!_isDragging && Mathf.Abs(deltaY) < DragThreshold()) return;

            _isDragging = true;
            _isManual = true;
            float worldPerPixel = _camera.orthographicSize * 2f / Screen.height;
            _manualY = ClampManualY(_pressCameraY - deltaY * worldPerPixel);
        }

        /// <summary>どのマスでも、コマを追っているときと同じ画面上の位置までは持ってこられる範囲にする</summary>
        private float ClampManualY(float y)
        {
            return Mathf.Clamp(y, _boardBounds.yMin + _followOffsetY, _boardBounds.yMax + _followOffsetY);
        }

        private static float DragThreshold()
        {
            return EventSystem.current != null ? EventSystem.current.pixelDragThreshold : 10f;
        }

        /// <summary>
        /// マスも Physics2DRaycaster で当たるので IsPointerOverGameObject は使えない。UI（GraphicRaycaster）に当たったかだけを見る
        /// </summary>
        private bool IsOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;

            var eventData = new PointerEventData(EventSystem.current) { position = screenPosition };
            EventSystem.current.RaycastAll(eventData, _raycastResults);
            foreach (RaycastResult result in _raycastResults)
            {
                if (result.module is GraphicRaycaster) return true;
            }
            return false;
        }

        /// <summary>触られている間はタッチを優先する（卓球の FlickInput と同じ。2本目の指で飛ばないように）</summary>
        private static Pointer ResolvePointer()
        {
            Touchscreen touchscreen = Touchscreen.current;
            bool touching = touchscreen != null
                && (touchscreen.press.isPressed || touchscreen.press.wasReleasedThisFrame);

            return touching ? touchscreen : Pointer.current;
        }

        private void LateUpdate()
        {
            if (_target == null && !IsOverview) return;

            // 時間ではなくフレーム間の差で追うと端末のフレームレートで速さが変わるので、指数補間にする。
            // ドラッグ中は指に遅れて付いてくると気持ち悪いので、そのまま合わせる
            float blend = _isDragging && _isManual ? 1f : 1f - Mathf.Exp(-_followSharpness * Time.unscaledDeltaTime);
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, TargetSize(), blend);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, TargetPosition(), blend);
        }

        private float TargetSize()
        {
            if (!IsOverview) return _followSize;

            float halfHeight = _boardBounds.height * 0.5f + _overviewMargin;
            float halfWidth = _boardBounds.width * 0.5f + _overviewMargin;
            return Mathf.Max(halfHeight, halfWidth / _camera.aspect);
        }

        private Vector3 TargetPosition()
        {
            Vector2 center = IsOverview || _target == null
                ? _boardBounds.center
                : (Vector2)_target.position + new Vector2(0f, _followOffsetY);
            if (!IsOverview) center.x = ClampToBoardX(center.x);
            if (_isManual && !IsOverview) center.y = _manualY;

            return new Vector3(center.x, center.y, _camera.transform.position.z);
        }

        /// <summary>
        /// 盤面が画面の横幅に収まるなら中央に固定し、道が左右にうねるたびに揺れないようにする。
        /// 縦長の端末で収まらないときだけ、盤面の外が見えない範囲でコマを追う
        /// </summary>
        private float ClampToBoardX(float x)
        {
            float halfView = TargetSize() * _camera.aspect;
            float slack = _boardBounds.width * 0.5f - halfView;
            if (slack <= 0f) return _boardBounds.center.x;

            return Mathf.Clamp(x, _boardBounds.center.x - slack, _boardBounds.center.x + slack);
        }
    }
}
