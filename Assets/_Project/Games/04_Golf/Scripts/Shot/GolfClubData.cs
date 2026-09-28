using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 1本のクラブ。飛び方の数値は手応えを見ながら詰めるため、クラブごとのアセットに切り出す。
    /// 計算に使う値はテストしやすいようエンジン非依存の ClubConfig にまとめて持つ。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfClubData", menuName = "MiniGame/Golf/Club Data")]
    public class GolfClubData : ScriptableObject
    {
        [SerializeField] private string _displayName = "クラブ";
        [SerializeField] private ClubConfig _config = new ClubConfig();

        public string DisplayName => _displayName;
        public ClubConfig Config => _config;
        public bool IsPutter => _config.IsPutter;
    }
}
