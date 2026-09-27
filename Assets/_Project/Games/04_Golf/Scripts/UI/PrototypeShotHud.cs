using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// Phase 1 の仮の表示。溜めているパワーと、止まった後の飛距離を出して「パワーで飛距離が変わる」ことを確かめられるようにする。
    /// Phase 3 のゲージ、Phase 9 の HudView で置き換える。
    /// </summary>
    public class PrototypeShotHud : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private PrototypeShotInput _input;
        [SerializeField] private GolfPhysicsSettings _settings;
        [SerializeField] private Text _text;

        private const string HintMessage = "長押しでパワーを溜めて\n離した方向へ打つ";

        private string _lastResult = string.Empty;

        private void OnEnable()
        {
            _ball.Stopped += OnBallStopped;
        }

        private void OnDisable()
        {
            _ball.Stopped -= OnBallStopped;
        }

        private void Update()
        {
            if (_input.IsCharging)
            {
                _text.text = $"パワー {Mathf.RoundToInt(_input.Power * 100f)}%";
            }
            else if (_ball.IsMoving)
            {
                _text.text = string.Empty;
            }
            else
            {
                _text.text = _lastResult + HintMessage;
            }
        }

        private void OnBallStopped()
        {
            float units = Vector2.Distance(_ball.LaunchPosition, _ball.GroundPosition);
            _lastResult = $"飛距離 {Mathf.RoundToInt(units * _settings.YardsPerUnit)}y\n";
        }
    }
}
