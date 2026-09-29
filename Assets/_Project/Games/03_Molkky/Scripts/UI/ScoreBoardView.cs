using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 画面上部のスコアボードと「あと○点」。
    /// チーム（個人戦はプレイヤー）ごとに色付きの枠を並べ、手番は明るく大きく、失格はグレーで見せる。
    /// 点数は数字をカウントさせて、何点動いたか（特に25点に戻ったこと）が目で追えるようにする。
    /// </summary>
    public class ScoreBoardView : MonoBehaviour
    {
        // 「あと○点」の数字だけを周りの文字より大きくして、残り点数を一目で読めるようにする
        private const int RemainingNumberFontSize = 56;

        // 次にミスしたら失格になるミス数。ここから警告色にする
        private const int WarningMissCount = MolkkyRules.MaxConsecutiveMisses - 1;

        [Tooltip("P1〜P4 の枠。人数ぶんだけ表示する")]
        [SerializeField] private RectTransform[] _cells;
        [SerializeField] private Image[] _cellBackgrounds;
        [SerializeField] private Text[] _nameTexts;
        [SerializeField] private Text[] _scoreTexts;
        [SerializeField] private Text[] _missTexts;
        [Tooltip("チーム戦で枠の下に添える「▶ P3 精密型」。チームの中で誰が投げるかが変わるため")]
        [SerializeField] private Text[] _throwerTexts;
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

        /// <summary>
        /// 得点の単位（チーム）ごとに枠を出す。個人戦は1人チームなので枠名はプレイヤー名になる。
        /// throwerName はチーム戦で手番チームの枠に添える、今回投げる人の名前。個人戦は枠名と同じなので null
        /// </summary>
        public void Show(IReadOnlyList<string> teamNames, IReadOnlyList<TeamScore> teams, int currentTeam,
            string throwerName)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                bool used = i < teams.Count;
                _cells[i].gameObject.SetActive(used);
                if (used) ShowCell(i, teamNames[i], teams[i], i == currentTeam, throwerName);
            }

            ShowRemaining(teamNames[currentTeam], teams[currentTeam], currentTeam);
        }

        /// <summary>枠を揺らす。25点に戻った・失格になったときの「やらかした」感を出す</summary>
        public void Shake(int index)
        {
            StartCoroutine(ShakeRoutine(_cells[index]));
        }

        private void ShowCell(int index, string teamName, TeamScore score, bool isCurrent, string throwerName)
        {
            Color playerColor = MolkkyPlayerColors.Get(index);

            _cellBackgrounds[index].color = score.IsDisqualified
                ? _disqualifiedColor
                : Color.Lerp(Color.black, playerColor, isCurrent ? 1f : _idleBrightness);
            _cells[index].localScale = Vector3.one * (isCurrent ? _currentScale : 1f);

            // チーム戦は投げる人の名前の方に▶を付け、▶が2つ並ばないようにする
            bool showThrower = isCurrent && throwerName != null;
            _nameTexts[index].text = isCurrent && !showThrower ? $"▶{teamName}" : teamName;
            ShowThrower(index, showThrower ? throwerName : null, playerColor);
            _missTexts[index].text = score.IsDisqualified ? "失格" : new string('×', score.MissCount);
            _missTexts[index].color = score.MissCount >= WarningMissCount ? _warningColor : Color.white;

            CountTo(index, score.Score);
        }

        private void ShowThrower(int index, string throwerName, Color teamColor)
        {
            _throwerTexts[index].gameObject.SetActive(throwerName != null);
            _throwerTexts[index].text = $"▶ {throwerName}";
            _throwerTexts[index].color = teamColor;
        }

        private void ShowRemaining(string teamName, TeamScore current, int currentIndex)
        {
            string name = Colorize(teamName, MolkkyPlayerColors.Get(currentIndex));
            string warning = current.MissCount == WarningMissCount ? $"  {Colorize("失格注意!", _warningColor)}" : "";
            _remainingText.text = $"{name}  あと <size={RemainingNumberFontSize}>{current.Remaining}</size> 点{warning}";
        }

        private static string Colorize(string text, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
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
