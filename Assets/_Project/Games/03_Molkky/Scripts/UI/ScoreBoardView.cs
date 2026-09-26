using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 画面上部のスコアボード（§12.1）と「あと○点」（§12.3）。
    /// プレイヤーごとにプレイヤー色の枠を並べ、手番は明るく大きく、失格はグレーで見せる。
    /// 点数は数字をカウントさせて、何点動いたか（特に25点に戻ったこと）が目で追えるようにする。
    /// </summary>
    public class ScoreBoardView : MonoBehaviour
    {
        [Tooltip("P1〜P4 の枠。人数ぶんだけ表示する")]
        [SerializeField] private RectTransform[] _cells;
        [SerializeField] private Image[] _cellBackgrounds;
        [SerializeField] private Text[] _nameTexts;
        [SerializeField] private Text[] _scoreTexts;
        [SerializeField] private Text[] _missTexts;
        [SerializeField] private Text _remainingText;

        [Header("Look")]
        [Tooltip("手番でない人の枠の明るさ（プレイヤー色に対する割合）。手番の人を目立たせるため暗くする")]
        [Range(0f, 1f)] [SerializeField] private float _idleBrightness = 0.4f;
        [SerializeField] private float _currentScale = 1.1f;
        [SerializeField] private Color _disqualifiedColor = new Color(0.35f, 0.35f, 0.38f);
        [SerializeField] private Color _warningColor = new Color(1f, 0.4f, 0.4f);

        [Header("Animation")]
        [SerializeField] private float _countDuration = 0.5f;
        [SerializeField] private float _shakeDuration = 0.45f;
        [SerializeField] private float _shakeAngle = 10f;
        [SerializeField] private float _shakeFrequency = 30f;

        private int[] _shownScores;
        private Coroutine[] _countRoutines;

        private void Awake()
        {
            _shownScores = new int[_cells.Length];
            _countRoutines = new Coroutine[_cells.Length];
        }

        public void Show(IReadOnlyList<PlayerSlot> players, int currentIndex)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                bool used = i < players.Count;
                _cells[i].gameObject.SetActive(used);
                if (used) ShowCell(i, players[i], i == currentIndex);
            }

            ShowRemaining(players[currentIndex], currentIndex);
        }

        /// <summary>枠を揺らす。25点に戻った・失格になったときの「やらかした」感を出す</summary>
        public void Shake(int index)
        {
            StartCoroutine(ShakeRoutine(_cells[index]));
        }

        private void ShowCell(int index, PlayerSlot player, bool isCurrent)
        {
            Color playerColor = MolkkyPlayerColors.Get(index);

            _cellBackgrounds[index].color = player.IsDisqualified
                ? _disqualifiedColor
                : Color.Lerp(Color.black, playerColor, isCurrent ? 1f : _idleBrightness);
            _cells[index].localScale = Vector3.one * (isCurrent ? _currentScale : 1f);

            _nameTexts[index].text = isCurrent ? $"▶{player.Name}" : player.Name;
            _missTexts[index].text = player.IsDisqualified ? "失格" : new string('×', player.MissCount);
            _missTexts[index].color = player.MissCount >= MolkkyRules.MaxConsecutiveMisses - 1 ? _warningColor : Color.white;

            CountTo(index, player.Score);
        }

        private void ShowRemaining(PlayerSlot current, int currentIndex)
        {
            string name = $"<color=#{ColorUtility.ToHtmlStringRGB(MolkkyPlayerColors.Get(currentIndex))}>{current.Name}</color>";
            string warning = current.MissCount == MolkkyRules.MaxConsecutiveMisses - 1
                ? $"  <color=#{ColorUtility.ToHtmlStringRGB(_warningColor)}>失格注意!</color>"
                : "";
            _remainingText.text = $"{name}  あと <size=56>{current.Remaining}</size> 点{warning}";
        }

        private void CountTo(int index, int target)
        {
            if (_shownScores[index] == target) return;

            if (_countRoutines[index] != null) StopCoroutine(_countRoutines[index]);
            _countRoutines[index] = StartCoroutine(CountRoutine(index, _shownScores[index], target));
        }

        private IEnumerator CountRoutine(int index, int from, int to)
        {
            for (float t = 0f; t < _countDuration; t += Time.deltaTime)
            {
                _shownScores[index] = Mathf.RoundToInt(Mathf.Lerp(from, to, t / _countDuration));
                _scoreTexts[index].text = _shownScores[index].ToString();
                yield return null;
            }

            _shownScores[index] = to;
            _scoreTexts[index].text = to.ToString();
            _countRoutines[index] = null;
        }

        /// <summary>
        /// 位置は HorizontalLayoutGroup が管理しているため、揺らすのは回転にする（レイアウトと干渉しない）
        /// </summary>
        private IEnumerator ShakeRoutine(RectTransform cell)
        {
            for (float t = 0f; t < _shakeDuration; t += Time.deltaTime)
            {
                float decay = 1f - t / _shakeDuration;
                float angle = Mathf.Sin(t * _shakeFrequency) * _shakeAngle * decay;
                cell.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            cell.localRotation = Quaternion.identity;
        }
    }
}
