using UnityEngine;

namespace MiniGame.Soccer
{
    /// <summary>
    /// 所属チーム
    /// </summary>
    public enum TeamSide
    {
        Home,
        Away
    }

    /// <summary>
    /// フィールド上の選手が所属するチームを保持する
    /// </summary>
    public class TeamMember : MonoBehaviour
    {
        [SerializeField] private TeamSide _team = TeamSide.Home;

        public TeamSide Team => _team;

        /// <summary>このチームが攻める方向（Home: +X, Away: -X）</summary>
        public float AttackDirection => _team == TeamSide.Home ? 1f : -1f;

        public void SetTeam(TeamSide team)
        {
            _team = team;
        }
    }
}
