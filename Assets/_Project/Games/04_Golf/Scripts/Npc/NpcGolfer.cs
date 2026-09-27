using System.Collections;
using UnityEngine;

namespace MiniGame.Golf
{
    /// <summary>
    /// NPC の手番（§10.4）。NpcShotPlanner で狙いを決め、人間と同じゲージを動かして打つ。
    /// 人間の入力（ShotInput の Update）は止めたまま、方向とゲージだけを借りるので、線・ゲージ・HUD の表示はそのまま使える。
    /// </summary>
    public class NpcGolfer : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private ShotInput _input;
        [SerializeField] private ClubSelector _clubs;
        [SerializeField] private GolfNpcDifficulty _difficulty;

        [Tooltip("狙いの線を見せてからスイングを始めるまでの時間（秒）。何を狙ったか人間に分かるようにする")]
        [SerializeField] private float _thinkSeconds = 0.8f;

        // 最後のフレームで狙いの位置ちょうどまで進めても、浮動小数の誤差でわずかに届かないことがあるため
        private const float MarkerTolerance = 0.0001f;

        private readonly System.Random _random = new System.Random();

        /// <summary>ShotInput.PrepareShot でクラブ（①）を選んだ後に呼ぶ。ボールを打つところまで進める</summary>
        public IEnumerator TakeShot(GolfPlayerType type)
        {
            var planner = new NpcShotPlanner(_ball.Simulation, _difficulty.Get(type), _random);
            ShotRequest shot = planner.Plan(_clubs.Current.Config, ToNumerics(_ball.CupPosition));

            _input.SetDirection(new Vector2(shot.Direction.X, shot.Direction.Y));
            yield return new WaitForSeconds(_thinkSeconds);

            yield return Swing(shot.Power, shot.ImpactOffset);
            _input.HitWithGauge();
        }

        /// <summary>
        /// ②タップはパワー、③タップはインパクトの位置でマーカーを止める。
        /// 1フレームで進む量のせいで行き過ぎると難易度に関係なくずれるので、最後のフレームは狙いの位置までしか進めない
        /// </summary>
        private IEnumerator Swing(float power, float impactOffset)
        {
            ShotGauge gauge = _input.Gauge;
            gauge.Begin();

            while (gauge.State == ShotGauge.GaugeState.Rising)
            {
                yield return null;
                gauge.Tick(Mathf.Min(Time.deltaTime, Mathf.Max((power - gauge.Marker) / gauge.Speed, 0f)));
                if (gauge.Marker >= power - MarkerTolerance) gauge.Tap();
            }

            float impactMarker = Mathf.Max(gauge.ZoneCenter + impactOffset * gauge.ZoneHalfWidth, 0f);
            while (gauge.State == ShotGauge.GaugeState.Returning)
            {
                yield return null;
                gauge.Tick(Mathf.Min(Time.deltaTime, Mathf.Max((gauge.Marker - impactMarker) / gauge.Speed, 0f)));
                if (gauge.Marker <= impactMarker + MarkerTolerance) gauge.Tap();
            }
        }

        private static System.Numerics.Vector2 ToNumerics(Vector2 v) => new System.Numerics.Vector2(v.x, v.y);
    }
}
