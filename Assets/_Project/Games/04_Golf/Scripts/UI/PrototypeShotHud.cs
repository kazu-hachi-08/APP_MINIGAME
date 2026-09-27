using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// Phase 1〜4 の仮の表示。打数・ライ・飛距離・インパクトの結果・罰打を出して、ホールを回れることとショットの違いを確かめられるようにする。
    /// 打数は GolfRules（Phase 6）ができるまでここで数える。Phase 9 の HudView で置き換える。
    /// </summary>
    public class PrototypeShotHud : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private ShotInput _input;
        [SerializeField] private HoleLoader _holeLoader;
        [SerializeField] private GolfPhysicsSettings _settings;
        [SerializeField] private Text _text;

        private const string HintMessage = "ドラッグ／◀▶で方向を決めて\nゲージを3回タップ";

        // ゾーンの中心付近は曲がりがほとんど見えないので「ナイスショット」と出す
        private const float NiceShotRange = 0.2f;

        // §6.5 池・OBはどちらも1打罰
        private const int PenaltyStrokes = 1;

        private int _strokes;
        private string _impactResult = string.Empty;
        private string _lastResult = string.Empty;

        // 罰打になったショットは飛距離の代わりにこれを出す。罰打が無ければ null
        private string _penaltyResult;

        private void OnEnable()
        {
            _ball.Launched += OnBallLaunched;
            _ball.Penalized += OnBallPenalized;
            _ball.Stopped += OnBallStopped;
        }

        private void OnDisable()
        {
            _ball.Launched -= OnBallLaunched;
            _ball.Penalized -= OnBallPenalized;
            _ball.Stopped -= OnBallStopped;
        }

        private void Update()
        {
            GolfHoleData hole = _holeLoader.CurrentHole;
            string header = $"{hole.DisplayName} PAR{hole.Par}  {_strokes}打\n";

            if (_ball.IsInCup)
            {
                _text.text = header + $"カップイン！ {_strokes}打";
            }
            else if (_ball.IsMoving)
            {
                _text.text = header + _impactResult;
            }
            else
            {
                _text.text = header + $"{GroundName(_ball.Ground)}  " + _lastResult + HintMessage;
            }
        }

        private void OnBallLaunched()
        {
            _strokes++;
            _impactResult = ImpactName(_input.Gauge.ImpactOffset);
            _penaltyResult = null;
        }

        /// <summary>打ち直しの位置へ移るまでの間も、何が起きたかを出しておく</summary>
        private void OnBallPenalized(GroundType ground)
        {
            _strokes += PenaltyStrokes;
            _penaltyResult = ground == GroundType.Water ? $"池ポチャ… +{PenaltyStrokes}打" : $"OB +{PenaltyStrokes}打";
            _impactResult = _penaltyResult;
        }

        private void OnBallStopped()
        {
            if (_penaltyResult != null)
            {
                _lastResult = _penaltyResult + "\n";
                return;
            }

            float units = Vector2.Distance(_ball.LaunchPosition, _ball.GroundPosition);
            _lastResult = $"{_impactResult} 飛距離 {Mathf.RoundToInt(units * _settings.YardsPerUnit)}y\n";
        }

        /// <summary>ずれの符号は ShotRequest.ImpactOffset と同じ（+ が右）</summary>
        private static string ImpactName(float offset)
        {
            if (Mathf.Abs(offset) > 1f) return "ミスショット";
            if (Mathf.Abs(offset) <= NiceShotRange) return "ナイスショット";
            return offset > 0f ? "スライス" : "フック";
        }

        private static string GroundName(GroundType ground)
        {
            switch (ground)
            {
                case GroundType.Tee: return "ティー";
                case GroundType.Fairway: return "フェアウェイ";
                case GroundType.Rough: return "ラフ";
                case GroundType.Bunker: return "バンカー";
                case GroundType.Green: return "グリーン";
                case GroundType.Water: return "池";
                default: return "OB";
            }
        }
    }
}
