using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// GolfHoleCatalog のホールを生成し、ティーにボールを置く（§15）。
    /// どのホールを何番目に、どの風で遊ぶかは GolfGameManager が決める（オンラインでは全端末で揃えるため）。
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

        public void Load(GolfHoleData hole, Wind wind)
        {
            if (CurrentCourse != null) Destroy(CurrentCourse.gameObject);

            CurrentHole = hole;
            CurrentCourse = Instantiate(hole.Prefab);
            CurrentWind = wind;
            _ball.SetCourse(CurrentCourse, CurrentCourse, CurrentCourse.TeePosition, CurrentCourse.CupPosition, CurrentWind);
            HoleLoaded?.Invoke();
        }

        /// <summary>§9.4 向きは全方向、強さはホールデータの範囲内でランダム</summary>
        public static Wind RandomWind(GolfHoleData hole)
        {
            float degrees = UnityEngine.Random.Range(0f, FullCircleDegrees);
            float strength = UnityEngine.Random.Range(hole.MinWindStrength, hole.MaxWindStrength);
            return Wind.FromDegrees(degrees, strength);
        }
    }
}
