using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>画面上部の全員の所持金。手番の人を強調し、約束手形があれば枚数も出す（仕様書 §3.3）</summary>
    public class MoneyBarView : MonoBehaviour
    {
        [Tooltip("席ごとの欄（最大4人）。使わない欄は隠す")]
        [SerializeField] private Text[] _cells;
        [SerializeField] private Image[] _backgrounds;
        [SerializeField] private Color _idleBackground = new Color(0.15f, 0.17f, 0.22f, 0.85f);
        [Range(0f, 1f)] [SerializeField] private float _currentTint = 0.6f;

        public void Refresh(LifeGameState state)
        {
            for (int seat = 0; seat < _cells.Length; seat++)
            {
                bool used = seat < state.Players.Count;
                _backgrounds[seat].gameObject.SetActive(used);
                if (!used) continue;

                LifePlayerState player = state.Players[seat];
                string notes = player.Notes > 0 ? $"\n<size=26>手形{player.Notes}枚</size>" : "";
                _cells[seat].text = $"{LifeTexts.PlayerName(seat)} {player.Money:#,0}{notes}";
                _cells[seat].color = LifeColors.Seat(seat);

                bool isCurrent = seat == state.CurrentSeat && state.Pending != LifePending.Finished;
                // 手番の人の欄だけ席の色に寄せ、誰の番かを画面上部でも分かるようにする
                _backgrounds[seat].color = isCurrent
                    ? Color.Lerp(_idleBackground, LifeColors.Seat(seat), _currentTint)
                    : _idleBackground;
                if (isCurrent) _cells[seat].color = Color.white;
            }
        }
    }
}
