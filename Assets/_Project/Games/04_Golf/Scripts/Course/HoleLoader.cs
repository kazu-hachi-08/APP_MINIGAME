using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// GolfHoleCatalog からホールを生成し、風を決めてティーにボールを置く（§15）。
    /// ランダム選択・複数ホールは Phase 6。それまでは確認用にインスペクタで選んだ1ホールを読み込む。
    /// </summary>
    public class HoleLoader : MonoBehaviour
    {
        [SerializeField] private GolfHoleCatalog _catalog;
        [SerializeField] private GolfBall _ball;

        [Tooltip("確認用：読み込むホールの番号（0始まり）。Phase 6 でランダム選択に置き換える")]
        [SerializeField] private int _holeIndex;

        private const float FullCircleDegrees = 360f;

        public GolfHoleData CurrentHole { get; private set; }
        public HoleCourse CurrentCourse { get; private set; }
        public Wind CurrentWind { get; private set; }

        // カメラなどが Start でボール位置を読むため、それより前の Awake でティーに置いておく
        private void Awake()
        {
            int index = Mathf.Clamp(_holeIndex, 0, _catalog.Holes.Count - 1);
            Load(_catalog.Holes[index]);
        }

        public void Load(GolfHoleData hole)
        {
            if (CurrentCourse != null) Destroy(CurrentCourse.gameObject);

            CurrentHole = hole;
            CurrentCourse = Instantiate(hole.Prefab);
            CurrentWind = RandomWind(hole);
            _ball.SetCourse(CurrentCourse, CurrentCourse, CurrentCourse.TeePosition, CurrentCourse.CupPosition, CurrentWind);
        }

        /// <summary>§9.4 向きは全方向、強さはホールデータの範囲内でランダム</summary>
        private static Wind RandomWind(GolfHoleData hole)
        {
            float degrees = Random.Range(0f, FullCircleDegrees);
            float strength = Random.Range(hole.MinWindStrength, hole.MaxWindStrength);
            return Wind.FromDegrees(degrees, strength);
        }
    }
}
