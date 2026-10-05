using System;
using System.Collections.Generic;
using MiniGame.Common.Audio;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ステージ選択。1章ずつ横一列にマスを並べ、左右のボタンで章を切り替える。
    /// マスはひな形から実行時に作る（ステージを足しても Scene を作り直さずに済むように。ずかんと同じ方式）
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

        private readonly List<StageNode> _nodes = new List<StageNode>();
        private CampaignProgress _progress;
        private Action<string> _onSortie;
        private Action _onBack;
        private int _chapter = FirstChapter;

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
            Refresh();
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        private void ChangeChapter(int delta)
        {
            PlayClick();
            _chapter = Mathf.Clamp(_chapter + delta, FirstChapter, StageDefinitions.ChapterCount);
            Refresh();
        }

        private void Refresh()
        {
            List<StageDefinition> stages = StageDefinitions.InChapter(_chapter);
            EnsureNodes(stages.Count);
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (i < stages.Count) _nodes[i].Show(stages[i], _progress, OpenDetail);
                else _nodes[i].Hide();
            }

            _chapterLabel.text = $"第{_chapter}章";
            _prevChapterButton.interactable = _chapter > FirstChapter;
            _nextChapterButton.interactable = _chapter < StageDefinitions.ChapterCount;
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
