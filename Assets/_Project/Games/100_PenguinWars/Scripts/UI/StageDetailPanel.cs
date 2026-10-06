using System;
using System.Collections.Generic;
using MiniGame.Common.Audio;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ステージ選択でマスを押すと出る詳細。★の条件を先に見せて、守り・速攻のどちらを狙うか決めてから出撃してもらう。
    /// 今の編成10体も小さく出し、押すと編成画面を開く（ステージに合わせて組み替えてから出撃できるように）。
    /// 出てくる敵の顔と特別ルールも出し、編成制限に当たるキャラは暗くする（何を外せばよいか分かるように）
    /// </summary>
    public class StageDetailPanel : MonoBehaviour
    {
        private const string BestPrefix = "ベスト ";

        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _descriptionLabel;
        [Tooltip("StageLabels.StarOrder と同じ順（クリア・城HP・タイム）")]
        [SerializeField] private Text[] _conditionLabels;
        [SerializeField] private Text _bestLabel;
        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("今の編成をコストの低い順に出す")]
        [SerializeField] private Image[] _deckIcons;
        [SerializeField] private Button _deckButton;
        [Tooltip("出てくる敵の枠。並びは定義の行の順。足りない分は出さない")]
        [SerializeField] private Image[] _enemyFrames;
        [SerializeField] private Image[] _enemyIcons;
        [SerializeField] private Text _rulesLabel;
        [SerializeField] private Button _sortieButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Color _earnedColor = new Color(1f, 0.88f, 0.3f);
        [SerializeField] private Color _missingColor = new Color(0.6f, 0.62f, 0.68f);
        [SerializeField] private Color _enemyFrameColor = new Color(1f, 1f, 1f, 0.15f);
        [Tooltip("ボスの行で出てくる敵の枠")]
        [SerializeField] private Color _bossFrameColor = new Color(0.9f, 0.2f, 0.2f);
        [Tooltip("編成制限で出せないキャラの色")]
        [SerializeField] private Color _bannedTint = new Color(0.25f, 0.25f, 0.25f, 0.8f);

        private Action<string> _onSortie;
        private Action _onEditDeck;
        private StageDefinition _stage;

        private void Awake()
        {
            _sortieButton.onClick.AddListener(Sortie);
            _closeButton.onClick.AddListener(Close);
            // 音は開く側（StageSelectPanel）が鳴らす
            _deckButton.onClick.AddListener(() => _onEditDeck?.Invoke());
        }

        public void Show(StageDefinition stage, CampaignProgress progress, Action<string> onSortie, Action onEditDeck)
        {
            _stage = stage;
            _onSortie = onSortie;
            _onEditDeck = onEditDeck;
            _titleLabel.text = StageLabels.Title(stage);
            _descriptionLabel.text = stage.Description;
            ShowConditions(stage, progress.GetStars(stage.Id));
            _bestLabel.text = BestPrefix + StageLabels.BestTime(progress.GetBestSeconds(stage.Id));
            ShowEnemies(stage);
            _rulesLabel.text = StageLabels.Rules(stage);
            RefreshDeck(progress);
            gameObject.SetActive(true);
        }

        /// <summary>編成画面から戻ったときにも呼ぶ</summary>
        public void RefreshDeck(CampaignProgress progress)
        {
            List<int> deck = DeckRules.CurrentDeck(progress);
            for (int i = 0; i < _deckIcons.Length; i++)
            {
                PenguinUnitData data = i < deck.Count ? _catalog.Get(deck[i]) : null;
                Sprite icon = data != null ? data.GetSprites(Side.Left).Icon : null;
                _deckIcons[i].sprite = icon;
                _deckIcons[i].enabled = icon != null;
                _deckIcons[i].color = i < deck.Count && !DeckRules.IsAllowed(_stage, deck[i]) ? _bannedTint : Color.white;
            }
        }

        private void ShowEnemies(StageDefinition stage)
        {
            List<int> enemyNos = stage.DistinctEnemyNos();
            for (int i = 0; i < _enemyIcons.Length; i++)
            {
                bool hasEnemy = i < enemyNos.Count;
                _enemyFrames[i].gameObject.SetActive(hasEnemy);
                if (!hasEnemy) continue;

                PenguinUnitData data = _catalog.Get(enemyNos[i]);
                // 戦場と同じ敵の色（赤）で見せる
                Sprite icon = data != null ? data.GetSprites(Side.Right).Icon : null;
                _enemyIcons[i].sprite = icon;
                _enemyIcons[i].enabled = icon != null;
                _enemyFrames[i].color = stage.IsBossUnit(enemyNos[i]) ? _bossFrameColor : _enemyFrameColor;
            }
        }

        public void Hide() => gameObject.SetActive(false);

        private void ShowConditions(StageDefinition stage, StarFlags earned)
        {
            for (int i = 0; i < _conditionLabels.Length; i++)
            {
                StarFlags star = StageLabels.StarOrder[i];
                bool has = StarRule.Has(earned, star);
                _conditionLabels[i].text = $"{StageLabels.Star(has)} {StageLabels.Condition(stage, star)}";
                _conditionLabels[i].color = has ? _earnedColor : _missingColor;
            }
        }

        private void Sortie()
        {
            PlayClick();
            Hide();
            _onSortie?.Invoke(_stage.Id);
        }

        private void Close()
        {
            PlayClick();
            Hide();
        }

        private static void PlayClick()
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
        }
    }
}
