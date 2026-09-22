using System.Collections.Generic;
using MiniGame.Common.Input;
using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 操作対象選手の切り替え（Phase 6）
    /// ボールに最も近い自チーム選手へ自動で操作を移し、それ以外の選手はAIに任せる。
    /// Action3（L / C / 仮想ボタン3）で手動切り替えも行える
    /// </summary>
    public class PlayerSwitcher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private List<GameObject> _candidates = new List<GameObject>();
        [SerializeField] private Ball _ball;
        [SerializeField] private CameraFollow _cameraFollow;
        [SerializeField] private Transform _controlMarker;
        [SerializeField] private SoccerGameManager _gameManager;

        [Header("Switch Settings")]
        [SerializeField] private int _defaultIndex; // キックオフ時に操作する選手
        [SerializeField] private float _evaluateInterval = 0.25f; // 自動切り替えの判定間隔
        [SerializeField] private float _switchCooldown = 0.5f; // 切り替え直後に再度切り替わるのを防ぐ待ち時間
        [SerializeField] private float _keepControlRadius = 1.2f; // 操作中の選手がボールに近い間は切り替えない（ドリブル中の奪取防止）

        private int _currentIndex = -1;
        private float _evaluateTimer;
        private float _cooldownTimer;

        public Transform CurrentPlayer => IsValidIndex(_currentIndex) ? _candidates[_currentIndex].transform : null;

        private void Start()
        {
            SelectPlayer(_defaultIndex);
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            if (_gameManager != null && !_gameManager.IsPlaying) return;

            if (InputManager.HasInstance && InputManager.Instance.IsAction3Down)
            {
                SwitchManually();
                return;
            }

            _evaluateTimer -= Time.deltaTime;
            if (_evaluateTimer <= 0f)
            {
                _evaluateTimer = _evaluateInterval;
                EvaluateAutoSwitch();
            }
        }

        private void LateUpdate()
        {
            // マーカーは選手の子にせず、位置だけ追従させる（切り替え時に付け替えずに済む）
            if (_controlMarker == null) return;

            Transform current = CurrentPlayer;
            if (current == null) return;

            _controlMarker.position = new Vector3(current.position.x, current.position.y, _controlMarker.position.z);
        }

        /// <summary>
        /// ゴール後などに、操作対象をキックオフ時の選手へ戻す
        /// </summary>
        public void ResetPlayers()
        {
            // 操作中の選手のAIコンポーネントは無効化されており、
            // SoccerGameManager 側の一括リセットから漏れる可能性があるためここでも戻す
            foreach (var candidate in _candidates)
            {
                if (candidate == null) continue;
                if (candidate.TryGetComponent<AIPlayerController>(out var ai))
                {
                    ai.ResetToHomePosition();
                }
            }

            _cooldownTimer = 0f;
            _evaluateTimer = _evaluateInterval;
            SelectPlayer(_defaultIndex);
        }

        /// <summary>
        /// ボールに最も近い選手へ操作を移す
        /// </summary>
        private void EvaluateAutoSwitch()
        {
            if (_ball == null || _cooldownTimer > 0f) return;

            Transform current = CurrentPlayer;
            if (current != null && Vector2.Distance(current.position, _ball.Position) <= _keepControlRadius) return;

            int nearest = FindNearestIndexToBall(excludeIndex: -1);
            if (nearest >= 0 && nearest != _currentIndex)
            {
                SelectPlayer(nearest);
            }
        }

        /// <summary>
        /// 現在の操作対象を除いて、ボールに最も近い選手へ手動で切り替える
        /// </summary>
        private void SwitchManually()
        {
            int nearest = FindNearestIndexToBall(excludeIndex: _currentIndex);
            if (nearest >= 0)
            {
                SelectPlayer(nearest);
            }
        }

        private int FindNearestIndexToBall(int excludeIndex)
        {
            if (_ball == null) return -1;

            int nearestIndex = -1;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < _candidates.Count; i++)
            {
                if (i == excludeIndex || _candidates[i] == null) continue;

                float distance = Vector2.Distance(_candidates[i].transform.position, _ball.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        private void SelectPlayer(int index)
        {
            if (!IsValidIndex(index) || index == _currentIndex) return;

            for (int i = 0; i < _candidates.Count; i++)
            {
                ApplyControlState(_candidates[i], isControlled: i == index);
            }

            _currentIndex = index;
            _cooldownTimer = _switchCooldown;

            if (_cameraFollow != null)
            {
                _cameraFollow.SetTarget(_candidates[index].transform);
            }
        }

        /// <summary>
        /// 操作対象ならプレイヤー操作を、それ以外ならAIを有効にする
        /// </summary>
        private void ApplyControlState(GameObject player, bool isControlled)
        {
            if (player == null) return;

            if (player.TryGetComponent<PlayerController>(out var playerController))
            {
                playerController.enabled = isControlled;
            }

            // SlidingTackleはPlayerControllerと同じ選手にのみ付与されており、操作権と連動させる
            if (player.TryGetComponent<SlidingTackle>(out var slidingTackle))
            {
                slidingTackle.enabled = isControlled;
            }

            if (player.TryGetComponent<AIPlayerController>(out var ai))
            {
                ai.enabled = !isControlled;
            }
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < _candidates.Count;
        }
    }
}
