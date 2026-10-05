using System;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>ステージ選択のマス1つ。クリア済み・遊べる・ロックで色を変え、ボスステージは大きく赤くして章の山場だと分かるようにする</summary>
    public class StageNode : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _background;
        [SerializeField] private Text _idLabel;
        [SerializeField] private Text _starsLabel;
        [SerializeField] private GameObject _lockMark;
        [SerializeField] private GameObject _bossMark;
        [SerializeField] private Color _clearedColor = new Color(0.2f, 0.5f, 0.85f);
        [SerializeField] private Color _playableColor = new Color(0.95f, 0.55f, 0.15f);
        [SerializeField] private Color _lockedColor = new Color(0.25f, 0.27f, 0.32f);
        [SerializeField] private Color _bossColor = new Color(0.8f, 0.2f, 0.25f);
        [SerializeField] private float _bossScale = 1.2f;

        public void Show(StageDefinition stage, CampaignProgress progress, Action<StageDefinition> onClick)
        {
            bool isPlayable = progress.IsPlayable(stage.Id);
            bool isCleared = progress.IsCleared(stage.Id);

            _idLabel.text = stage.Id;
            _starsLabel.text = StageLabels.Stars(progress.GetStars(stage.Id));
            _starsLabel.gameObject.SetActive(isPlayable);
            _lockMark.SetActive(!isPlayable);
            _bossMark.SetActive(stage.IsBossStage);
            _background.color = PickColor(stage, isPlayable, isCleared);
            transform.localScale = Vector3.one * (stage.IsBossStage ? _bossScale : 1f);

            _button.interactable = isPlayable;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClick(stage));
            gameObject.SetActive(true);
        }

        /// <summary>まだ遊んでいないステージを一番目立たせ、次に何を遊べばいいかすぐ分かるようにする</summary>
        private Color PickColor(StageDefinition stage, bool isPlayable, bool isCleared)
        {
            if (!isPlayable) return _lockedColor;
            if (!isCleared) return _playableColor;
            return stage.IsBossStage ? _bossColor : _clearedColor;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
