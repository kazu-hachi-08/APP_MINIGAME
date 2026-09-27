using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// GolfHoleCatalog のホールを生成し、風を決めてティーにボールを置く（§15）。
    /// どのホールを何番目に遊ぶかは GolfGameManager が決める。
    /// </summary>
    public class HoleLoader : MonoBehaviour
    {
        [SerializeField] private GolfHoleCatalog _catalog;
        [SerializeField] private GolfBall _ball;

        private const float FullCircleDegrees = 360f;

        public GolfHoleData CurrentHole { get; private set; }
        public HoleCourse CurrentCourse { get; private set; }
        public Wind CurrentWind { get; private set; }

        public IReadOnlyList<GolfHoleData> Holes => _catalog.Holes;

        /// <summary>ホールを読み込んだ（風の表示などを更新する）</summary>
        public event Action HoleLoaded;

        public void Load(GolfHoleData hole)
        {
            if (CurrentCourse != null) Destroy(CurrentCourse.gameObject);

            CurrentHole = hole;
            CurrentCourse = Instantiate(hole.Prefab);
            CurrentWind = RandomWind(hole);
            _ball.SetCourse(CurrentCourse, CurrentCourse, CurrentCourse.TeePosition, CurrentCourse.CupPosition, CurrentWind);
            HoleLoaded?.Invoke();
        }

        /// <summary>§9.4 向きは全方向、強さはホールデータの範囲内でランダム</summary>
        private static Wind RandomWind(GolfHoleData hole)
        {
            float degrees = UnityEngine.Random.Range(0f, FullCircleDegrees);
            float strength = UnityEngine.Random.Range(hole.MinWindStrength, hole.MaxWindStrength);
            return Wind.FromDegrees(degrees, strength);
        }
    }
}
