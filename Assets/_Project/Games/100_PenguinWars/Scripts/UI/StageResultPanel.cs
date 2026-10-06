using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using MiniGame.Common.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>リザルトに出す中身。引数が多くなりすぎないよう1つにまとめる</summary>
    public class StageResultContent
    {
        public string Title;
        /// <summary>失敗のときは null（★の行を出さない）</summary>
        public StarFlags? Stars;
        /// <summary>今回新しく取った★（光って跳ねる）</summary>
        public StarFlags NewStars;
        public string Detail;
        public bool IsNewRecord;
        /// <summary>このクリアで仲間になったキャラ。いなければ空</summary>
        public IReadOnlyList<int> UnlockNos = Array.Empty<int>();
    }

    /// <summary>
    /// ステージのリザルト。共通の ResultDialog はボタンが「リトライ」「タイトル」の2つだけなので、
    /// 「つぎのステージ」「もういちど」「ステージ選択」を出せるペンギン大戦争専用のものを持つ（Common は変えない）。
    /// ★を1つずつ出し（StarRevealAnimator）、出し終えたら仲間のカード（UnlockRevealPanel）を1体ずつ出す。
    /// カードを閉じた後も見返せるよう、右側に仲間の絵と名前を並べておく
    /// </summary>
    public class StageResultPanel : MonoBehaviour
    {
        [SerializeField] private Text _titleLabel;
        [SerializeField] private StarRevealAnimator _stars;
        [SerializeField] private GameObject _newRecordLabel;
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
        [SerializeField] private UnlockRevealPanel _unlockReveal;

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

        /// <param name="onNext">次のステージが無い・失敗のときは null（ボタンを出さない）</param>
        public void Show(StageResultContent content, Action onNext, Action onRetry, Action onSelect)
        {
            _titleLabel.text = content.Title;
            _detailLabel.text = content.Detail;
            _newRecordLabel.SetActive(content.IsNewRecord);
            _onNext = onNext;
            _onRetry = onRetry;
            _onSelect = onSelect;
            _nextButton.gameObject.SetActive(onNext != null);
            ShowUnlocks(content.UnlockNos);
            _unlockReveal.gameObject.SetActive(false);
            gameObject.SetActive(true);

            if (content.Stars.HasValue)
            {
                IReadOnlyList<int> unlockNos = content.UnlockNos;
                _stars.Play(content.Stars.Value, content.NewStars, () => _unlockReveal.Show(unlockNos, null));
            }
            else
            {
                _stars.Hide();
            }
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
