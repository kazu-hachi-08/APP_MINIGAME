using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// ドラフト画面（仕様書 §6）。上にラウンドと残り秒、中央に候補カード、下に取ったキャラ一覧。
    /// 抽選・時間切れの判定はホストの DraftSession が持つので、ここは見せて押されたことを伝えるだけ。
    /// 残り秒はラウンドが届いた時から自分で減らす（ゲストへ毎秒送らないため。少し遅れて見えるだけで判定には使わない）
    /// </summary>
    public class DraftPanel : MonoBehaviour
    {
        private const string ChooseMessage = "1体えらんでください";
        private const string WaitingMessage = "相手を待っています...";

        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private Text _roundLabel;
        [SerializeField] private Text _timeLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private DraftCard[] _cards;
        [Tooltip("取ったキャラ一覧。並びが編成の枠の順")]
        [SerializeField] private Image[] _pickedIcons;

        private float _remaining;
        private bool _hasPicked;

        /// <summary>候補の何番目が押されたか。このラウンドで1回だけ</summary>
        public event Action<int> Picked;

        private void Awake()
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                int offerIndex = i;
                _cards[i].Bind(() => HandleCardClicked(offerIndex));
            }
        }

        /// <param name="round">0始まり</param>
        public void ShowRound(int round, int totalRounds, IReadOnlyList<int> offer, IReadOnlyList<int> picks, float pickTime)
        {
            _roundLabel.text = $"ラウンド {round + 1} / {totalRounds}";
            _remaining = pickTime;
            _hasPicked = false;
            _statusLabel.text = ChooseMessage;
            ShowOffer(offer);
            ShowPicks(picks);
            RefreshTime();
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            _remaining = Mathf.Max(0f, _remaining - Time.deltaTime);
            RefreshTime();
        }

        private void HandleCardClicked(int offerIndex)
        {
            if (_hasPicked) return;

            _hasPicked = true;
            for (int i = 0; i < _cards.Length; i++) _cards[i].SetState(false, i == offerIndex);
            _statusLabel.text = WaitingMessage;
            Picked?.Invoke(offerIndex);
        }

        private void ShowOffer(IReadOnlyList<int> offer)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                bool hasUnit = i < offer.Count;
                _cards[i].gameObject.SetActive(hasUnit);
                if (hasUnit) _cards[i].Show(offer[i], _catalog.Get(offer[i]));
            }
        }

        private void ShowPicks(IReadOnlyList<int> picks)
        {
            for (int i = 0; i < _pickedIcons.Length; i++)
            {
                PenguinUnitData data = i < picks.Count ? _catalog.Get(picks[i]) : null;
                UnitLabels.SetIcon(_pickedIcons[i], data, Side.Left);
            }
        }

        /// <summary>切り上げにして、0.5秒残っているのに 0 と出ないようにする</summary>
        private void RefreshTime()
        {
            _timeLabel.text = $"のこり {Mathf.CeilToInt(_remaining)}";
        }
    }
}
