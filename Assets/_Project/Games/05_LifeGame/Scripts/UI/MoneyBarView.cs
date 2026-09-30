using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 画面上部の全員の所持金。手番の人を強調し、約束手形があれば枚数も出す（仕様書 §3.3）。
    /// 金額は一気に変えず数えるように動かし、増えたら緑・減ったら赤で光らせて、誰のお金が動いたかを目で追えるようにする。
    /// </summary>
    public class MoneyBarView : MonoBehaviour
    {
        [Tooltip("席ごとの欄（最大4人）。使わない欄は隠す")]
        [SerializeField] private Text[] _cells;
        [SerializeField] private Image[] _backgrounds;
        [SerializeField] private Color _idleBackground = new Color(0.15f, 0.17f, 0.22f, 0.85f);
        [Range(0f, 1f)] [SerializeField] private float _currentTint = 0.6f;

        [Header("Count")]
        [SerializeField] private float _countDuration = 0.6f;
        [SerializeField] private Color _gainColor = new Color(0.45f, 1f, 0.45f);
        [SerializeField] private Color _lossColor = new Color(1f, 0.45f, 0.4f);

        private const int MaxSeats = 4;

        private readonly float[] _from = new float[MaxSeats];
        private readonly int[] _target = new int[MaxSeats];
        private readonly float[] _changedAt = new float[MaxSeats];
        private readonly string[] _notes = new string[MaxSeats];
        private readonly Color[] _restColors = new Color[MaxSeats];
        private int _seatCount;
        private bool _initialized;

        public void Refresh(LifeGameState state)
        {
            _seatCount = Mathf.Min(state.Players.Count, _cells.Length);
            for (int seat = 0; seat < _cells.Length; seat++)
            {
                bool used = seat < _seatCount;
                _backgrounds[seat].gameObject.SetActive(used);
                if (used) RefreshSeat(state, seat);
            }

            _initialized = true;
        }

        private void RefreshSeat(LifeGameState state, int seat)
        {
            LifePlayerState player = state.Players[seat];
            // 最初の表示は数えずにそのまま出す（試合開始時に0から数え上がらないように）
            if (!_initialized)
            {
                _from[seat] = player.Money;
                _target[seat] = player.Money;
            }

            if (player.Money != _target[seat])
            {
                _from[seat] = ShownMoney(seat);
                _target[seat] = player.Money;
                _changedAt[seat] = Time.time;
            }

            _notes[seat] = player.Notes > 0 ? $"\n<size=26>手形{player.Notes}枚</size>" : "";

            bool isCurrent = seat == state.CurrentSeat && state.Pending != LifePending.Finished;
            // 手番の人の欄だけ席の色に寄せ、誰の番かを画面上部でも分かるようにする
            _backgrounds[seat].color = isCurrent
                ? Color.Lerp(_idleBackground, LifeColors.Seat(seat), _currentTint)
                : _idleBackground;
            _restColors[seat] = isCurrent ? Color.white : LifeColors.Seat(seat);
            Draw(seat);
        }

        private void Update()
        {
            for (int seat = 0; seat < _seatCount; seat++) Draw(seat);
        }

        private float CountRate(int seat) => _countDuration <= 0f ? 1f : Mathf.Clamp01((Time.time - _changedAt[seat]) / _countDuration);

        private float ShownMoney(int seat) => Mathf.Lerp(_from[seat], _target[seat], CountRate(seat));

        private void Draw(int seat)
        {
            int shown = Mathf.RoundToInt(ShownMoney(seat));
            _cells[seat].text = $"{LifeTexts.PlayerName(seat)} {shown:#,0}{_notes[seat]}";

            bool counting = CountRate(seat) < 1f;
            Color changeColor = _target[seat] >= _from[seat] ? _gainColor : _lossColor;
            _cells[seat].color = counting ? changeColor : _restColors[seat];
        }
    }
}
