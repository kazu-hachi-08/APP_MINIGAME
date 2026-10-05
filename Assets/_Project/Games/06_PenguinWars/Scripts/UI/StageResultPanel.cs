using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using MiniGame.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ステージのリザルト。共通の ResultDialog はボタンが「リトライ」「タイトル」の2つだけなので、
    /// 「つぎのステージ」「もういちど」「ステージ選択」を出せるペンギン大戦争専用のものを持つ（Common は変えない）。
    /// 初クリアで仲間が増えたら右側に絵と名前を並べる（演出は Phase 7）
    /// </summary>
    public class StageResultPanel : MonoBehaviour
    {
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _starsLabel;
        [SerializeField] private Text _detailLabel;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _selectButton;

        [Header("なかまになった！")]
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private GameObject _unlockGroup;
        [Tooltip("1ステージで仲間になる数の上限ぶん。あふれた分は出さない")]
        [SerializeField] private Image[] _unlockIcons;
        [SerializeField] private Text[] _unlockNames;
        [Tooltip("仲間を出すとき、文字を左に寄せて右側をあける量")]
        [SerializeField] private float _detailShiftWithUnlocks = -300f;

        private Vector2? _detailBasePosition;

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
        /// <param name="unlockNos">このクリアで仲間になったキャラ。いなければ空</param>
        /// <param name="onNext">次のステージが無い・失敗のときは null（ボタンを出さない）</param>
        public void Show(string title, string stars, string detail, IReadOnlyList<int> unlockNos, Action onNext, Action onRetry, Action onSelect)
        {
            _titleLabel.text = title;
            _starsLabel.gameObject.SetActive(stars != null);
            _starsLabel.text = stars ?? string.Empty;
            _detailLabel.text = detail;
            _onNext = onNext;
            _onRetry = onRetry;
            _onSelect = onSelect;
            _nextButton.gameObject.SetActive(onNext != null);
            ShowUnlocks(unlockNos);
            gameObject.SetActive(true);
        }

        private void ShowUnlocks(IReadOnlyList<int> unlockNos)
        {
            bool hasUnlocks = unlockNos.Count > 0;
            _unlockGroup.SetActive(hasUnlocks);
            // 非表示のまま Show されると Awake より先にここへ来るので、最初の1回で元の位置を覚える
            _detailBasePosition ??= _detailLabel.rectTransform.anchoredPosition;
            _detailLabel.rectTransform.anchoredPosition = _detailBasePosition.Value + Vector2.right * (hasUnlocks ? _detailShiftWithUnlocks : 0f);

            for (int i = 0; i < _unlockIcons.Length; i++)
            {
                bool hasUnit = i < unlockNos.Count;
                _unlockIcons[i].gameObject.SetActive(hasUnit);
                _unlockNames[i].gameObject.SetActive(hasUnit);
                if (hasUnit) ShowUnit(i, unlockNos[i]);
            }
        }

        private void ShowUnit(int index, int unitNo)
        {
            PenguinUnitData data = _catalog.Get(unitNo);
            Sprite icon = data != null ? data.GetSprites(Side.Left).Icon : null;
            _unlockIcons[index].sprite = icon;
            _unlockIcons[index].enabled = icon != null;
            _unlockNames[index].text = data != null ? data.DisplayName : $"No.{unitNo}";
        }

        private void Choose(Action action)
        {
            if (AudioManager.HasInstance) AudioManager.Instance.PlaySe(SeId.ButtonClick);
            gameObject.SetActive(false);
            action?.Invoke();
        }
    }
}
