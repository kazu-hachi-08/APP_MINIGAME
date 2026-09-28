using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 常に出すHUD。ホール・パー・全員の打数・今のライ・カップまでの残り距離と、直前のショットの結果を出す。
    /// 風は WindView、池ポチャ・カップインなどの大きな演出は GolfMessageView が出すので、ここは文字情報だけにする。
    /// 打数は GolfGameManager（GolfPlayerSlot）が数えたものを読むだけにする。
    /// </summary>
    public class GolfHudView : MonoBehaviour
    {
        [SerializeField] private GolfBall _ball;
        [SerializeField] private ShotInput _input;
        [SerializeField] private HoleLoader _holeLoader;
        [SerializeField] private GolfGameManager _manager;
        [SerializeField] private GolfPhysicsSettings _settings;
        [SerializeField] private Text _text;

        private const string HintMessage = "ドラッグ／◀▶で方向を決めて\nゲージを3回タップ";

        // ゾーンの中心付近は曲がりがほとんど見えないので「ナイスショット」と出す
        private const float NiceShotRange = 0.2f;

        // ずれは ±1 がゾーンの端。ゾーンを外したらミスショット
        private const float ZoneEdge = 1f;

        private string _impactResult = string.Empty;
        private string _lastResult = string.Empty;

        // 罰打になったショットは飛距離の代わりにこれを出す。罰打が無ければ null
        private string _penaltyResult;

        private void OnEnable()
        {
            _ball.Launched += OnBallLaunched;
            _ball.Penalized += OnBallPenalized;
            _ball.Stopped += OnBallStopped;
            _manager.TurnStarted += OnTurnStarted;
        }

        private void OnDisable()
        {
            _ball.Launched -= OnBallLaunched;
            _ball.Penalized -= OnBallPenalized;
            _ball.Stopped -= OnBallStopped;
            _manager.TurnStarted -= OnTurnStarted;
        }

        private void Update()
        {
            GolfHoleData hole = _holeLoader.CurrentHole;
            if (hole == null || _manager.CurrentPlayer < 0)
            {
                _text.text = string.Empty;
                return;
            }

            string header = $"H{_manager.HoleNumber + 1}/{_manager.HoleCount} {hole.DisplayName} PAR{hole.Par}\n{StrokesLine()}\n";

            if (_ball.IsInCup)
            {
                _text.text = header + "カップイン！";
            }
            else if (_ball.IsMoving)
            {
                _text.text = header + _impactResult;
            }
            else
            {
                _text.text = header + RestingInfo();
            }
        }

        /// <summary>止まっている間は、次のショットを考える材料（ライ・残り距離・前のショット結果）と操作の案内を出す</summary>
        private string RestingInfo()
        {
            string hint = _input.CanAim ? HintMessage : string.Empty;
            return $"{GroundName(_ball.Ground)}  残り {RemainingYards()}y\n" + _lastResult + hint;
        }

        /// <summary>「▶P1 2打  P2 3打」。手番の人に ▶ を付け、名前はプレイヤー色にする</summary>
        private string StrokesLine()
        {
            var line = new StringBuilder();
            foreach (GolfPlayerSlot slot in _manager.Slots)
            {
                if (line.Length > 0) line.Append("  ");
                if (slot.Seat == _manager.CurrentPlayer) line.Append("▶");
                line.Append(GolfPlayerColors.Colored(slot.Seat, GolfPlayerColors.Name(slot.Seat)));
                line.Append(slot.IsHoledOut ? $" {slot.Strokes}打(IN)" : $" {slot.Strokes}打");
            }

            return line.ToString();
        }

        private int RemainingYards()
        {
            return YardsBetween(_ball.GroundPosition, _ball.CupPosition);
        }

        private int YardsBetween(Vector2 from, Vector2 to)
        {
            return Mathf.RoundToInt(Vector2.Distance(from, to) * _settings.YardsPerUnit);
        }

        /// <summary>前の人のショット結果が残らないよう、手番が替わったら消す</summary>
        private void OnTurnStarted()
        {
            _lastResult = string.Empty;
        }

        private void OnBallLaunched()
        {
            _impactResult = ImpactName(_input.Gauge.ImpactOffset);
            _penaltyResult = null;
        }

        /// <summary>打ち直しの位置へ移るまでの間も、何が起きたかを出しておく</summary>
        private void OnBallPenalized(GroundType ground)
        {
            int penalty = GolfRules.PenaltyStrokes;
            _penaltyResult = ground == GroundType.Water ? $"池ポチャ +{penalty}打" : $"OB +{penalty}打";
            _impactResult = _penaltyResult;
        }

        private void OnBallStopped()
        {
            if (_penaltyResult != null)
            {
                _lastResult = _penaltyResult + "\n";
                return;
            }

            _lastResult = $"{_impactResult} 飛距離 {YardsBetween(_ball.LaunchPosition, _ball.GroundPosition)}y\n";
        }

        /// <summary>ずれの符号は ShotRequest.ImpactOffset と同じ（+ が右）</summary>
        private static string ImpactName(float offset)
        {
            if (Mathf.Abs(offset) > ZoneEdge) return "ミスショット";
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
