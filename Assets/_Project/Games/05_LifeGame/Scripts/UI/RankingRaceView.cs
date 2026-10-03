using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>総資産レースの1人ぶん（席順に渡す）</summary>
    public struct RaceEntry
    {
        public Sprite Face;
        public string Name;
        public Color Color;
        public int Total;
        public int Rank;
    }

    /// <summary>
    /// 総資産レースと順位発表（計画 Phase 7 ④⑤）。全員の棒が同じ速さで伸び、自分の総資産に着いた棒から止まるので、
    /// 何もしなくても最下位から順に止まって順位の札が出る。上位2人が残ったところで一度止めてドラムロールを入れ、1位で紙吹雪を降らせる。
    /// タップで最後の状態まで飛ばし、もう一度タップ（または一定時間）で勝利演出へ進む。
    /// </summary>
    public class RankingRaceView : MonoBehaviour
    {
        [SerializeField] private Button _tapArea;
        [SerializeField] private LifeAudio _audio;
        [SerializeField] private ConfettiView _confetti;

        [Header("Lanes（席の数だけ並べる）")]
        [SerializeField] private RectTransform[] _lanes;
        [SerializeField] private RectTransform[] _bars;
        [Tooltip("棒の先端と一緒に上がる部品（顔・金額・順位の札）の親")]
        [SerializeField] private RectTransform[] _tops;
        [SerializeField] private Image[] _faces;
        [SerializeField] private Text[] _moneys;
        [SerializeField] private Text[] _names;
        [SerializeField] private Text[] _badges;

        [SerializeField] private float _maxBarHeight = 900f;

        [Header("Timing (sec)")]
        [SerializeField] private float _beforeRace = 0.5f;
        [SerializeField] private float _raceDuration = 2.5f;
        [Tooltip("ドラムロールの長さ。LifeAudio のドラムロールの音と合わせる")]
        [SerializeField] private float _drumrollDuration = 1.4f;
        [SerializeField] private float _finalDuration = 1f;
        [SerializeField] private float _badgePopDuration = 0.25f;
        [Tooltip("1位が出てから勝利演出へ自動で進むまで。タップでも進める")]
        [SerializeField] private float _holdDuration = 2f;

        [Header("Motion")]
        [Tooltip("上位2人の棒をどこまで伸ばしてからドラムロールで止めるか（2位の総資産に対する割合）")]
        [Range(0f, 1f)] [SerializeField] private float _pauseRatio = 0.8f;
        [SerializeField] private float _badgeOvershoot = 1.4f;
        [SerializeField] private float _winnerBadgeScale = 1.5f;

        private readonly List<RaceEntry> _entries = new List<RaceEntry>();
        private bool[] _stopped;
        private float[] _badgeTimes;
        private bool _tapped;
        private int _top;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
        }

        public IEnumerator Play(IReadOnlyList<RaceEntry> entries)
        {
            gameObject.SetActive(true);
            Setup(entries);

            float second = TotalOfRank(2);
            float pausePoint = Mathf.Clamp(Mathf.Max(TotalOfRank(3), second * _pauseRatio), 0f, Mathf.Max(0f, second));

            yield return Wait(_beforeRace);
            yield return Race(0f, pausePoint, _raceDuration, t => t * t * (3f - 2f * t));
            if (!_tapped) _audio.PlayDrumroll();
            yield return Wait(_drumrollDuration);
            yield return Race(pausePoint, _top, _finalDuration, t => 1f - (1f - t) * (1f - t));

            // ここまでのタップは「飛ばす」に使ったので、勝利演出へ進むタップは改めて待つ
            _tapped = false;
            yield return Wait(_holdDuration);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Setup(IReadOnlyList<RaceEntry> entries)
        {
            _entries.Clear();
            _entries.AddRange(entries);
            _stopped = new bool[entries.Count];
            _badgeTimes = new float[entries.Count];
            _top = 1;
            foreach (RaceEntry entry in entries) _top = Mathf.Max(_top, entry.Total);

            for (int i = 0; i < _lanes.Length; i++)
            {
                bool used = i < entries.Count;
                _lanes[i].gameObject.SetActive(used);
                if (!used) continue;

                _bars[i].GetComponent<Image>().color = entries[i].Color;
                _faces[i].sprite = entries[i].Face;
                _names[i].text = entries[i].Name;
                _names[i].color = entries[i].Color;
                _badges[i].gameObject.SetActive(false);
                SetValue(i, 0f);
            }

            _tapped = false;
        }

        private float TotalOfRank(int rank)
        {
            foreach (RaceEntry entry in _entries)
            {
                if (entry.Rank == rank) return entry.Total;
            }

            return 0f;
        }

        /// <summary>全員共通の「今の額」を from → to へ動かし、各自の棒は自分の総資産で止める</summary>
        private IEnumerator Race(float from, float to, float duration, System.Func<float, float> ease)
        {
            for (float t = 0f; t < duration && !_tapped; t += Time.deltaTime)
            {
                UpdateLanes(Mathf.Lerp(from, to, ease(t / duration)));
                yield return null;
            }

            UpdateLanes(_tapped ? _top : to);
        }

        private void UpdateLanes(float value)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                SetValue(i, Mathf.Min(value, _entries[i].Total));
                if (!_stopped[i] && value >= _entries[i].Total) StopLane(i);
                AnimateBadge(i);
            }
        }

        /// <summary>マイナスの総資産は棒を伸ばさず、金額だけ見せる</summary>
        private void SetValue(int lane, float value)
        {
            float height = _maxBarHeight * Mathf.Clamp01(value / _top);
            _bars[lane].sizeDelta = new Vector2(_bars[lane].sizeDelta.x, height);
            _tops[lane].anchoredPosition = new Vector2(0f, height);
            _moneys[lane].text = LifeTexts.Money(Mathf.RoundToInt(value));
        }

        private void StopLane(int lane)
        {
            _stopped[lane] = true;
            _badgeTimes[lane] = Time.time;
            int rank = _entries[lane].Rank;
            _badges[lane].text = $"{rank}位";
            _badges[lane].gameObject.SetActive(true);

            if (rank != 1)
            {
                _audio.PlayPop();
                return;
            }

            _audio.PlayJan();
            _confetti.Burst(ConfettiColors());
        }

        /// <summary>札は少し大きく出てから戻す。1位だけは大きいまま残して目立たせる</summary>
        private void AnimateBadge(int lane)
        {
            if (!_stopped[lane]) return;

            float t = Mathf.Clamp01((Time.time - _badgeTimes[lane]) / _badgePopDuration);
            if (_tapped) t = 1f;
            float rest = _entries[lane].Rank == 1 ? _winnerBadgeScale : 1f;
            float scale = t < 0.6f ? Mathf.Lerp(0f, _badgeOvershoot * rest, t / 0.6f) : Mathf.Lerp(_badgeOvershoot * rest, rest, (t - 0.6f) / 0.4f);
            _badges[lane].transform.localScale = Vector3.one * scale;
        }

        /// <summary>紙吹雪は全員の席の色。1位の色だけにすると背景（勝利演出は1位の色）に溶けるため</summary>
        private List<Color> ConfettiColors()
        {
            var colors = new List<Color> { Color.white, LifeColors.Celebration };
            foreach (RaceEntry entry in _entries) colors.Add(entry.Color);
            return colors;
        }

        /// <summary>待つ間も札の弾みは動かし続ける</summary>
        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !_tapped; t += Time.deltaTime)
            {
                for (int i = 0; i < _entries.Count; i++) AnimateBadge(i);
                yield return null;
            }

            for (int i = 0; i < _entries.Count; i++) AnimateBadge(i);
        }
    }
}
