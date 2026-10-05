using System;
using System.Collections;
using System.Collections.Generic;
using MiniGame.Common.Audio;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ステージ選択。1章ずつ横一列にマスを並べ、左右のボタンで章を切り替える。
    /// マスはひな形から実行時に作る（ステージを足しても Scene を作り直さずに済むように。ずかんと同じ方式）。
    /// 新しく遊べるようになったステージは開いたときに鍵が外れる演出を出し、それが次の章なら章を送って題字を出す（ステージ計画 Phase 7）
    /// </summary>
    public class StageSelectPanel : MonoBehaviour
    {
        private const int FirstChapter = 1;

        [Tooltip("非表示のひな形。章のステージ数だけ複製して使い回す")]
        [SerializeField] private StageNode _nodeTemplate;
        [SerializeField] private Text _chapterLabel;
        [SerializeField] private Button _prevChapterButton;
        [SerializeField] private Button _nextChapterButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private StageDetailPanel _detailPanel;
        [SerializeField] private Button _deckButton;
        [SerializeField] private DeckEditPanel _deckEditPanel;

        [Header("章送り・鍵が外れる演出")]
        [SerializeField] private CanvasGroup _chapterBanner;
        [SerializeField] private Text _chapterBannerLabel;
        [Tooltip("開いてから章を送り始めるまで（今の章をひと目見せる）")]
        [SerializeField] private float _chapterScrollDelay = 0.6f;
        [SerializeField] private float _chapterScrollSeconds = 0.35f;
        [Tooltip("マスの列が画面の外へ出ていく距離")]
        [SerializeField] private float _chapterScrollDistance = 1900f;
        [SerializeField] private float _bannerSeconds = 1.8f;
        [SerializeField] private float _bannerFadeSeconds = 0.3f;
        [Tooltip("章を開いてから鍵が外れるまで")]
        [SerializeField] private float _unlockDelay = 0.4f;

        private readonly List<StageNode> _nodes = new List<StageNode>();
        private CampaignProgress _progress;
        private Action<string> _onSortie;
        private Action _onBack;
        private int _chapter = FirstChapter;
        private bool _isScrolling;
        // 章送りの途中で閉じられても、次に開いたとき列がずれたままにならないよう元の位置を覚えておく
        private Vector2? _rowBasePosition;

        private RectTransform Row => (RectTransform)_nodeTemplate.transform.parent;

        private void Awake()
        {
            _prevChapterButton.onClick.AddListener(() => ChangeChapter(-1));
            _nextChapterButton.onClick.AddListener(() => ChangeChapter(1));
            _backButton.onClick.AddListener(Back);
            _deckButton.onClick.AddListener(OpenDeckEdit);
        }

        /// <summary>最後に遊んだステージの章を開く（クリアして戻ってきたとき、続きがすぐ見えるように）</summary>
        public void Show(CampaignProgress progress, Action<string> onSortie, Action onBack)
        {
            _progress = progress;
            _onSortie = onSortie;
            _onBack = onBack;
            StageDefinition last = progress.LastPlayedId != null ? StageDefinitions.Find(progress.LastPlayedId) : null;
            _chapter = last != null ? last.Chapter : FirstChapter;
            _detailPanel.Hide();
            _deckEditPanel.Hide();
            _chapterBanner.gameObject.SetActive(false);
            _isScrolling = false;
            _rowBasePosition ??= Row.anchoredPosition;
            Row.anchoredPosition = _rowBasePosition.Value;
            Refresh();
            gameObject.SetActive(true);
            PlayReveals();
        }

        /// <summary>まだ鍵の演出を見せていないステージがあれば見せる。記録はすぐ保存して、途中で閉じても2回は出さない</summary>
        private void PlayReveals()
        {
            var revealIds = new List<string>();
            foreach (StageDefinition stage in StageDefinitions.All)
            {
                if (_progress.NeedsUnlockReveal(stage.Id)) revealIds.Add(stage.Id);
            }
            if (revealIds.Count == 0) return;

            foreach (string id in revealIds) _progress.MarkUnlockRevealed(id);
            CampaignSave.Save(_progress);

            int revealChapter = StageDefinitions.Find(revealIds[0]).Chapter;
            if (revealChapter > _chapter) StartCoroutine(ScrollToChapter(revealChapter, revealIds));
            else PlayUnlocks(revealIds);
        }

        /// <summary>ボスを初めて倒して戻ってきたとき: 今の章を見せてから列を左へ流し、次の章を右から入れて題字を出す</summary>
        private IEnumerator ScrollToChapter(int chapter, List<string> revealIds)
        {
            _isScrolling = true;
            RectTransform row = Row;
            Vector2 basePosition = _rowBasePosition.Value;
            yield return new WaitForSecondsRealtime(_chapterScrollDelay);

            yield return SlideRow(row, basePosition, 0f, -_chapterScrollDistance);
            _chapter = chapter;
            Refresh();
            yield return SlideRow(row, basePosition, _chapterScrollDistance, 0f);
            _isScrolling = false;

            PlayUnlocks(revealIds);
            yield return ShowChapterBanner();
        }

        private IEnumerator SlideRow(RectTransform row, Vector2 basePosition, float fromX, float toX)
        {
            for (float t = 0f; t < _chapterScrollSeconds; t += Time.unscaledDeltaTime)
            {
                float x = Mathf.Lerp(fromX, toX, Mathf.SmoothStep(0f, 1f, t / _chapterScrollSeconds));
                row.anchoredPosition = basePosition + Vector2.right * x;
                yield return null;
            }
            row.anchoredPosition = basePosition + Vector2.right * toX;
        }

        private IEnumerator ShowChapterBanner()
        {
            _chapterBannerLabel.text = StageLabels.Chapter(_chapter);
            _chapterBanner.gameObject.SetActive(true);
            for (float t = 0f; t < _bannerSeconds; t += Time.unscaledDeltaTime)
            {
                float fadeIn = Mathf.Clamp01(t / _bannerFadeSeconds);
                float fadeOut = Mathf.Clamp01((_bannerSeconds - t) / _bannerFadeSeconds);
                _chapterBanner.alpha = Mathf.Min(fadeIn, fadeOut);
                yield return null;
            }
            _chapterBanner.gameObject.SetActive(false);
        }

        /// <summary>今開いている章にあるものだけ鍵を外す（別の章の分は記録だけ済ませる）</summary>
        private void PlayUnlocks(List<string> revealIds)
        {
            foreach (StageNode node in _nodes)
            {
                if (node.gameObject.activeSelf && revealIds.Contains(node.StageId)) node.PlayUnlock(_unlockDelay);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        private void ChangeChapter(int delta)
        {
            if (_isScrolling) return;

            PlayClick();
            _chapter = Mathf.Clamp(_chapter + delta, FirstChapter, StageDefinitions.ChapterCount);
            Refresh();
        }

        private void Refresh()
        {
            List<StageDefinition> stages = StageDefinitions.InChapter(_chapter);
            string latestId = LatestPlayableId();
            EnsureNodes(stages.Count);
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (i < stages.Count) _nodes[i].Show(stages[i], _progress, stages[i].Id == latestId, OpenDetail);
                else _nodes[i].Hide();
            }

            _chapterLabel.text = StageLabels.Chapter(_chapter);
            _prevChapterButton.interactable = _chapter > FirstChapter;
            _nextChapterButton.interactable = _chapter < StageDefinitions.ChapterCount;
        }

        /// <summary>遊べるステージのうち、遊ぶ順で一番先のもの</summary>
        private string LatestPlayableId()
        {
            string latest = null;
            foreach (StageDefinition stage in StageDefinitions.All)
            {
                if (_progress.IsPlayable(stage.Id)) latest = stage.Id;
            }
            return latest;
        }

        private void EnsureNodes(int count)
        {
            while (_nodes.Count < count)
            {
                _nodes.Add(Instantiate(_nodeTemplate, _nodeTemplate.transform.parent));
            }
        }

        private void OpenDetail(StageDefinition stage)
        {
            PlayClick();
            _detailPanel.Show(stage, _progress, Sortie, OpenDeckEdit);
        }

        private void OpenDeckEdit()
        {
            PlayClick();
            _deckEditPanel.Show(_progress, OnDeckEditClosed);
        }

        /// <summary>詳細から開いた場合は、詳細の編成アイコンを新しい編成に描き直す</summary>
        private void OnDeckEditClosed()
        {
            if (_detailPanel.gameObject.activeSelf) _detailPanel.RefreshDeck(_progress);
        }

        private void Sortie(string stageId)
        {
            Hide();
            _onSortie?.Invoke(stageId);
        }

        private void Back()
        {
            PlayClick();
            Hide();
            _onBack?.Invoke();
        }

        private static void PlayClick()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
        }
    }
}
