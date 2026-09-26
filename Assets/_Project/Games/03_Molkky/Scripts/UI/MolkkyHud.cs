using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 仮のスコア表示（Phase 2〜）。スコア一覧・残り点数・中央のメッセージを文字だけで出す。
    /// Phase 7 で ScoreBoardView / TurnBannerView / ScorePopupView に分ける。
    /// </summary>
    public class MolkkyHud : MonoBehaviour
    {
        // ①〜⑫ は Unicode で連続しているので、数字から丸数字を作れる
        private const char CircledOne = '①';

        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _remainingText;
        [SerializeField] private Text _messageText;

        [SerializeField] private Color _currentColor = new Color(1f, 0.9f, 0.3f);
        [SerializeField] private Color _disqualifiedColor = new Color(0.6f, 0.6f, 0.6f);

        public void ShowScores(IReadOnlyList<PlayerSlot> players, int currentIndex)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < players.Count; i++)
            {
                if (i > 0) builder.Append("   ");
                builder.Append(FormatPlayer(players[i], i, i == currentIndex));
            }

            _scoreText.text = builder.ToString();

            PlayerSlot current = players[currentIndex];
            string warning = current.MissCount == MolkkyRules.MaxConsecutiveMisses - 1 ? "  <color=#ff6060>失格注意!</color>" : "";
            _remainingText.text = $"{current.Name}  あと {current.Remaining} 点{warning}";
        }

        private string FormatPlayer(PlayerSlot player, int index, bool isCurrent)
        {
            if (player.IsDisqualified)
            {
                return $"<color=#{ColorUtility.ToHtmlStringRGB(_disqualifiedColor)}>{player.Name} 失格</color>";
            }

            // 名前だけプレイヤー色にして、1台を回すときに誰のスコアか一目で分かるようにする
            string name = $"<color=#{ColorUtility.ToHtmlStringRGB(MolkkyPlayerColors.Get(index))}>{player.Name}</color>";
            string text = $"{name} {player.Score} {new string('×', player.MissCount)}";
            if (!isCurrent) return text;

            return $"<color=#{ColorUtility.ToHtmlStringRGB(_currentColor)}>▶{text}</color>";
        }

        public void ShowMessage(string message)
        {
            _messageText.text = message;
            _messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        public void ClearMessage()
        {
            ShowMessage(string.Empty);
        }

        /// <summary>1投の結果を §12.2 の文言にする</summary>
        public static string FormatResult(ThrowResult result)
        {
            switch (result.Outcome)
            {
                case ThrowOutcome.Win:
                    return "50! WIN!";
                case ThrowOutcome.OverTo25:
                    return "オーバー！ 25点に";
                case ThrowOutcome.Disqualified:
                    return "失格…";
                case ThrowOutcome.Miss:
                    return "ミス ×";
                default:
                    return result.SinglePinNumber > 0
                        ? $"{(char)(CircledOne + result.SinglePinNumber - 1)}  +{result.Points}"
                        : $"{result.FallenCount}本  +{result.Points}";
            }
        }
    }
}
