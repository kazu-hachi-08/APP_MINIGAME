using System;
using MiniGame.Common.Audio;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>ステージ選択でマスを押すと出る詳細。★の条件を先に見せて、守り・速攻のどちらを狙うか決めてから出撃してもらう</summary>
    public class StageDetailPanel : MonoBehaviour
    {
        private const string BestPrefix = "ベスト ";

        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _descriptionLabel;
        [Tooltip("StageLabels.StarOrder と同じ順（クリア・城HP・タイム）")]
        [SerializeField] private Text[] _conditionLabels;
        [SerializeField] private Text _bestLabel;
        [SerializeField] private Button _sortieButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Color _earnedColor = new Color(1f, 0.88f, 0.3f);
        [SerializeField] private Color _missingColor = new Color(0.6f, 0.62f, 0.68f);

        private Action<string> _onSortie;
        private string _stageId;

        private void Awake()
        {
            _sortieButton.onClick.AddListener(Sortie);
            _closeButton.onClick.AddListener(Close);
        }

        public void Show(StageDefinition stage, CampaignProgress progress, Action<string> onSortie)
        {
            _stageId = stage.Id;
            _onSortie = onSortie;
            _titleLabel.text = StageLabels.Title(stage);
            _descriptionLabel.text = stage.Description;
            ShowConditions(stage, progress.GetStars(stage.Id));
            _bestLabel.text = BestPrefix + StageLabels.BestTime(progress.GetBestSeconds(stage.Id));
            gameObject.SetActive(true);
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
            _onSortie?.Invoke(_stageId);
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
