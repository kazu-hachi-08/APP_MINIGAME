using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// P1 → P2 → … の順で1人ずつキャラを選ぶ（仕様書 §9.3）。1台を回して遊ぶので「今選んでいる1人」だけを見せる。
    /// NPCはランダムで選んだ状態から始め、そのまま決定しても人間が代わりに変えてもよい。
    /// オンラインでは自分の席だけを選び、決定後とテーマ待ちの間は待機の文面だけを出す（仕様書 §10.3）。
    /// </summary>
    public class CharacterSelectPanel : MonoBehaviour
    {
        [SerializeField] private LifeCharacterCatalog _catalog;

        [SerializeField] private Text _titleText;
        [SerializeField] private Image _portraitImage;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _abilityText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _backButton;

        private IReadOnlyList<LifePlayerKind> _kinds;
        private int[] _selected;
        private int _seat;
        private Action<IReadOnlyList<int>> _onConfirmed;
        private Action _onBack;
        private Action<int> _onOnlineConfirmed;

        private void Awake()
        {
            _prevButton.onClick.AddListener(() => Cycle(-1));
            _nextButton.onClick.AddListener(() => Cycle(1));
            _confirmButton.onClick.AddListener(Confirm);
            _backButton.onClick.AddListener(Back);
        }

        /// <summary>全員決まったら席順のキャラ番号を返す。P1で「戻る」を押したら onBack を呼ぶ</summary>
        public void Show(IReadOnlyList<LifePlayerKind> kinds, Action<IReadOnlyList<int>> onConfirmed, Action onBack)
        {
            _kinds = kinds;
            _onConfirmed = onConfirmed;
            _onBack = onBack;
            _onOnlineConfirmed = null;
            _selected = new int[kinds.Count];
            for (int i = 0; i < kinds.Count; i++)
            {
                // 人間は先頭のキャラから、NPCは毎回違う相手になるようランダムから始める
                _selected[i] = kinds[i] == LifePlayerKind.Human ? 0 : UnityEngine.Random.Range(0, _catalog.Count);
            }

            _seat = 0;
            SetSelecting(true, true);
            Refresh();
        }

        /// <summary>オンライン：自分の席のキャラだけ選ぶ。「戻る」は出さない（部屋の設定をやり直す手段がないため）</summary>
        public void ShowOnline(int localSeat, int playerCount, Action<int> onConfirmed)
        {
            _onOnlineConfirmed = onConfirmed;
            _onConfirmed = null;
            _onBack = null;
            // オンラインは全員人間（既定値の Human）
            _kinds = new LifePlayerKind[playerCount];
            _selected = new int[playerCount];
            _seat = localSeat;
            SetSelecting(true, false);
            Refresh();
        }

        /// <summary>ボタンを隠して文面だけ出す（ホストのテーマ選択待ち・他のプレイヤーのキャラ選択待ち）</summary>
        public void ShowWaiting(string message)
        {
            SetSelecting(false, false);
            _titleText.text = message;
            _titleText.color = Color.white;
            _nameText.text = "";
            _abilityText.text = "";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SetSelecting(bool selecting, bool canGoBack)
        {
            _portraitImage.gameObject.SetActive(selecting);
            _prevButton.gameObject.SetActive(selecting);
            _nextButton.gameObject.SetActive(selecting);
            _confirmButton.gameObject.SetActive(selecting);
            _backButton.gameObject.SetActive(canGoBack);
            gameObject.SetActive(true);
        }

        private void Cycle(int step)
        {
            _selected[_seat] = (_selected[_seat] + step + _catalog.Count) % _catalog.Count;
            Refresh();
        }

        private void Confirm()
        {
            if (_onOnlineConfirmed != null)
            {
                _onOnlineConfirmed(_selected[_seat]);
                return;
            }

            _seat++;
            if (_seat < _selected.Length)
            {
                Refresh();
                return;
            }

            gameObject.SetActive(false);
            _onConfirmed?.Invoke(_selected);
        }

        private void Back()
        {
            _seat--;
            if (_seat >= 0)
            {
                Refresh();
                return;
            }

            gameObject.SetActive(false);
            _onBack?.Invoke();
        }

        private void Refresh()
        {
            string npc = _kinds[_seat] == LifePlayerKind.Npc ? "（NPC）" : "";
            _titleText.text = $"{LifeTexts.PlayerName(_seat)}{npc} のキャラを選んでね";
            // 端末を回したときに誰の番か一目で分かるよう、席の色で出す
            _titleText.color = LifeColors.Seat(_seat);

            LifeCharacterData character = _catalog.Get(_selected[_seat]);
            _portraitImage.sprite = character.Portrait;
            _nameText.text = character.DisplayName;
            _abilityText.text = character.AbilityText;
        }
    }
}
