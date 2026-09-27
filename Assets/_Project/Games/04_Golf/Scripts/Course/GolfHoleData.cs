using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 1ホール分のデータ（§9.3）。1ホール＝1プレハブ＋1データにして、ホール追加で Scene を触らずに済むようにする。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfHoleData", menuName = "MiniGame/Golf/Hole Data")]
    public class GolfHoleData : ScriptableObject
    {
        [SerializeField] private string _displayName = "ホール";
        [SerializeField] private int _par = 3;
        [SerializeField] private HoleCourse _prefab;

        public string DisplayName => _displayName;
        public int Par => _par;
        public HoleCourse Prefab => _prefab;
    }
}
