using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// リザルトの上に「なかまになった！」のカードを1体ずつ出す（ステージ計画 Phase 7）。ペンギンは歩きコマで足踏みし、画面を押すと次のカードへ。
    /// 全員出し終えたら閉じて、下のリザルト（右側に仲間の一覧がある）に戻る
    /// </summary>
    public class UnlockRevealPanel : MonoBehaviour
    {
        private static readonly PenguinFrame[] StepFrames = { PenguinFrame.Walk0, PenguinFrame.Walk1 };

        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private PenguinWarsAudio _audio;
        [Tooltip("画面全体のボタン。どこを押しても次へ進める")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private RectTransform _card;
        [SerializeField] private UnitSpriteAnimator _icon;
        [SerializeField] private Text _nameLabel;
        [Tooltip("「1/3」のように、あと何体いるかを出す")]
        [SerializeField] private Text _countLabel;
        [SerializeField] private float _popDuration = 0.25f;
        [SerializeField] private float _popStartScale = 0.5f;
        [Tooltip("カードが出てすぐの連打で読み飛ばさないよう、この秒数は押しても進まない")]
        [SerializeField] private float _inputDelay = 0.3f;

        private IReadOnlyList<int> _unitNos;
        private int _index;
        private float _shownTime;
        private Action _onFinished;

        private void Awake()
        {
            _nextButton.onClick.AddListener(Next);
        }

        /// <param name="unitNos">空なら何も出さずにすぐ onFinished を呼ぶ</param>
        public void Show(IReadOnlyList<int> unitNos, Action onFinished)
        {
            _unitNos = unitNos;
            _onFinished = onFinished;
            _index = 0;
            if (unitNos.Count == 0)
            {
                Finish();
                return;
            }

            gameObject.SetActive(true);
            ShowCard();
        }

        private void ShowCard()
        {
            int unitNo = _unitNos[_index];
            PenguinUnitData data = _catalog.Get(unitNo);
            if (data != null) _icon.Play(data.GetSprites(Side.Left), StepFrames);
            _nameLabel.text = data != null ? data.DisplayName : $"No.{unitNo}";
            _countLabel.text = $"{_index + 1}/{_unitNos.Count}";
            _shownTime = Time.unscaledTime;
            _card.localScale = Vector3.one * _popStartScale;
            _audio.PlayFanfare();
        }

        private void Update()
        {
            float rate = Mathf.Clamp01((Time.unscaledTime - _shownTime) / _popDuration);
            // 少し行き過ぎてから戻る（EaseOutBack）と「ポン」と出てきた感じになる
            const float overshoot = 1.7f;
            float eased = 1f + (overshoot + 1f) * Mathf.Pow(rate - 1f, 3f) + overshoot * Mathf.Pow(rate - 1f, 2f);
            _card.localScale = Vector3.one * Mathf.LerpUnclamped(_popStartScale, 1f, eased);
        }

        private void Next()
        {
            if (Time.unscaledTime - _shownTime < _inputDelay) return;

            _index++;
            if (_index < _unitNos.Count) ShowCard();
            else Finish();
        }

        private void Finish()
        {
            gameObject.SetActive(false);
            Action onFinished = _onFinished;
            _onFinished = null;
            onFinished?.Invoke();
        }
    }
}
