using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// Phase 1〜2 の仮の表示。打数・ライ・パワー・飛距離を出して、ホールを回れることとライの違いを確かめられるようにする。
    /// 打数は GolfRules（Phase 6）ができるまでここで数える。Phase 3 のゲージ、Phase 9 の HudView で置き換える。
    /// </summary>
    public class PrototypeShotHud : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private PrototypeShotInput _input;
        [SerializeField] private HoleLoader _holeLoader;
        [SerializeField] private GolfPhysicsSettings _settings;
        [SerializeField] private Text _text;

        private const string HintMessage = "長押しでパワーを溜めて\n離した方向へ打つ";

        private int _strokes;
        private string _lastResult = string.Empty;

        private void OnEnable()
        {
            _ball.Launched += OnBallLaunched;
            _ball.Stopped += OnBallStopped;
        }

        private void OnDisable()
        {
            _ball.Launched -= OnBallLaunched;
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
            else if (_input.IsCharging)
            {
                _text.text = header + $"パワー {Mathf.RoundToInt(_input.Power * 100f)}%";
            }
            else if (_ball.IsMoving)
            {
                _text.text = header;
            }
            else
            {
                _text.text = header + $"{GroundName(_ball.Ground)}  " + _lastResult + HintMessage;
            }
        }

        private void OnBallLaunched()
        {
            _strokes++;
        }

        private void OnBallStopped()
        {
            float units = Vector2.Distance(_ball.LaunchPosition, _ball.GroundPosition);
            _lastResult = $"飛距離 {Mathf.RoundToInt(units * _settings.YardsPerUnit)}y\n";
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
