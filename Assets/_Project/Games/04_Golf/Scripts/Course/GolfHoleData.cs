using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// 1ホール分のデータ。1ホール＝1プレハブ＋1データにして、ホール追加で Scene を触らずに済むようにする。
    /// </summary>
    [CreateAssetMenu(fileName = "GolfHoleData", menuName = "MiniGame/Golf/Hole Data")]
    public class GolfHoleData : ScriptableObject
    {
        [SerializeField] private string _displayName = "ホール";
        [SerializeField] private int _par = 3;
        [SerializeField] private HoleCourse _prefab;

        [Header("風の強さの範囲（m）。ホール開始時にこの範囲からランダムに決める")]
        [SerializeField] private float _minWindStrength = 0f;
        [SerializeField] private float _maxWindStrength = 5f;

        public string DisplayName => _displayName;
        public int Par => _par;
        public HoleCourse Prefab => _prefab;
        public float MinWindStrength => _minWindStrength;
        public float MaxWindStrength => _maxWindStrength;
    }
}
