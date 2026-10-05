using System;
using MiniGame.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ステージのリザルト。共通の ResultDialog はボタンが「リトライ」「タイトル」の2つだけなので、
    /// 「つぎのステージ」「もういちど」「ステージ選択」を出せるペンギン大戦争専用のものを持つ（Common は変えない）
    /// </summary>
    public class StageResultPanel : MonoBehaviour
    {
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _starsLabel;
        [SerializeField] private Text _detailLabel;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _selectButton;

        private Action _onNext;
        private Action _onRetry;
        private Action _onSelect;

        private void Awake()
        {
            _nextButton.onClick.AddListener(() => Choose(_onNext));
            _retryButton.onClick.AddListener(() => Choose(_onRetry));
            _selectButton.onClick.AddListener(() => Choose(_onSelect));
        }

        /// <param name="stars">失敗のときは null（★の行を出さない）</param>
        /// <param name="onNext">次のステージが無い・失敗のときは null（ボタンを出さない）</param>
        public void Show(string title, string stars, string detail, Action onNext, Action onRetry, Action onSelect)
        {
            _titleLabel.text = title;
            _starsLabel.gameObject.SetActive(stars != null);
            _starsLabel.text = stars ?? string.Empty;
            _detailLabel.text = detail;
            _onNext = onNext;
            _onRetry = onRetry;
            _onSelect = onSelect;
            _nextButton.gameObject.SetActive(onNext != null);
            gameObject.SetActive(true);
        }

        private void Choose(Action action)
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
            gameObject.SetActive(false);
            action?.Invoke();
        }
    }
}
