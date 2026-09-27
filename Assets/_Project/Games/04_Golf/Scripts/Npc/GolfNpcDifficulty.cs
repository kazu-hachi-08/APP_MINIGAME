using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// NPC の3段階の強さ（§10.3）。強さの差は遊びながら詰めるため、アセットに切り出す。
    /// 計算に使う値はテストしやすいようエンジン非依存の NpcDifficultyConfig にまとめて持つ。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfNpcDifficulty", menuName = "MiniGame/Golf/Npc Difficulty")]
    public class GolfNpcDifficulty : ScriptableObject
    {
        [SerializeField] private NpcDifficultyConfig _weak = new NpcDifficultyConfig(10f, 0.2f, 1.3f, false, false, false);
        [SerializeField] private NpcDifficultyConfig _normal = new NpcDifficultyConfig(4f, 0.08f, 0.7f, false, true, false);
        [SerializeField] private NpcDifficultyConfig _strong = new NpcDifficultyConfig(1.5f, 0.03f, 0.3f, true, true, true);

        public NpcDifficultyConfig Get(GolfPlayerType type)
        {
            switch (type)
            {
                case GolfPlayerType.NpcWeak: return _weak;
                case GolfPlayerType.NpcStrong: return _strong;
                default: return _normal;
            }
        }
    }
}
