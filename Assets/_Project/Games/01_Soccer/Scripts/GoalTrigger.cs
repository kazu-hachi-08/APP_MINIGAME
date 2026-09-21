using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// ゴール判定用センサー。ボールの侵入を検知してゲームマネージャーへ通知する
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GoalTrigger : MonoBehaviour
    {
        [SerializeField] private SoccerGameManager _gameManager;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<Ball>(out _)) return;

            if (_gameManager != null)
            {
                _gameManager.OnGoalScored();
            }
        }
    }
}
