using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// GolfHoleCatalog からホールを生成し、ティーにボールを置く（§15）。
    /// Phase 2 は1ホールだけなので先頭を読み込む。ランダム選択・複数ホールは Phase 6。
    /// </summary>
    public class HoleLoader : MonoBehaviour
    {
        [SerializeField] private GolfHoleCatalog _catalog;
        [SerializeField] private GolfBall _ball;

        public GolfHoleData CurrentHole { get; private set; }
        public HoleCourse CurrentCourse { get; private set; }

        // カメラなどが Start でボール位置を読むため、それより前の Awake でティーに置いておく
        private void Awake()
        {
            Load(_catalog.Holes[0]);
        }

        public void Load(GolfHoleData hole)
        {
            if (CurrentCourse != null) Destroy(CurrentCourse.gameObject);

            CurrentHole = hole;
            CurrentCourse = Instantiate(hole.Prefab);
            _ball.SetCourse(CurrentCourse, CurrentCourse.TeePosition, CurrentCourse.CupPosition);
        }
    }
}
