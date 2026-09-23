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
        [SerializeField] private TeamSide _defendingTeam = TeamSide.Away;
        [SerializeField] private GoalReaction _goalReaction;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent<Ball>(out _)) return;
            if (_gameManager == null) return;

            if (_goalReaction != null)
            {
                _goalReaction.Play();
            }

            // 守っているチームの逆側が得点する
            TeamSide scoringTeam = _defendingTeam == TeamSide.Home ? TeamSide.Away : TeamSide.Home;
            _gameManager.OnGoalScored(scoringTeam);
        }
    }
}
