using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// 人数設定の後に、P1 → P2 → … の順で1人ずつキャラを選ぶパネル（§21.4）。
    /// モルックの CharacterSelectPanel と同じ流れ。1台を回して遊ぶ前提なので、画面は「今選んでいる1人」だけを大きく見せる。
    /// NPCはランダムで選んだ状態から始め、そのまま決定しても人間が代わりに変えてもよい。
    /// オンライン（§21.5）では自分の席の1人だけを選び、決定後は全員が揃うまで待機表示にする。
    /// </summary>
    public class GolfCharacterSelectPanel : MonoBehaviour
    {
        [SerializeField] private GolfCharacterCatalog _catalog;

        [SerializeField] private Text _titleText;
        [SerializeField] private Image _portrait;
        [SerializeField] private Text _nameText;
        [SerializeField] private GolfStatBarView _distanceBar;
        [SerializeField] private GolfStatBarView _straightnessBar;

        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _backButton;

        private IReadOnlyList<GolfPlayerType> _types;
        private int[] _selected;
        private int _seat;
        private Action<IReadOnlyList<int>> _onConfirmed;
        private Action _onBack;
        private Action<int> _onOnlineConfirmed;

        private bool IsOnline => _onOnlineConfirmed != null;

        private void Awake()
        {
            _prevButton.onClick.AddListener(() => Cycle(-1));
            _nextButton.onClick.AddListener(() => Cycle(1));
            _confirmButton.onClick.AddListener(Confirm);
            _backButton.onClick.AddListener(Back);
        }

        /// <summary>全員決まったら席順のキャラ番号一覧を返す。P1で「戻る」を押したら onBack を呼ぶ</summary>
        public void Show(IReadOnlyList<GolfPlayerType> types, Action<IReadOnlyList<int>> onConfirmed, Action onBack)
        {
            _types = types;
            _onConfirmed = onConfirmed;
            _onBack = onBack;
            _onOnlineConfirmed = null;
            _selected = CreateInitialSelection(types);
            _seat = 0;

            Open();
        }

        /// <summary>
        /// オンライン用：自分の席のキャラだけを選ぶ。決定したらキャラ番号を返し、Hide されるまで待機表示を続ける。
        /// 「戻る」は出さない。人数設定が無いので戻り先が無いため
        /// </summary>
        public void ShowOnline(int seat, int playerCount, Action<int> onConfirmed)
        {
            _types = null;
            _onConfirmed = null;
            _onBack = null;
            _onOnlineConfirmed = onConfirmed;
            _selected = new int[playerCount];
            _seat = seat;

            Open();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Open()
        {
            gameObject.SetActive(true);
            _backButton.gameObject.SetActive(!IsOnline);
            SetSelecting(true);
            Refresh();
        }

        /// <summary>決定後の待機中に◀▶や決定を押されて、送った後にキャラが変わらないよう操作を止める</summary>
        private void SetSelecting(bool selecting)
        {
            _prevButton.interactable = selecting;
            _nextButton.interactable = selecting;
            _confirmButton.interactable = selecting;
        }

        private int[] CreateInitialSelection(IReadOnlyList<GolfPlayerType> types)
        {
            var selected = new int[types.Count];
            for (int i = 0; i < types.Count; i++)
            {
                // 人間は基準のバランス型から、NPCは毎回違う相手になるようランダムから始める
                selected[i] = types[i] == GolfPlayerType.Human ? 0 : UnityEngine.Random.Range(0, _catalog.Count);
            }

            return selected;
        }

        private void Cycle(int step)
        {
            _selected[_seat] = (_selected[_seat] + step + _catalog.Count) % _catalog.Count;
            Refresh();
        }

        private void Confirm()
        {
            if (IsOnline)
            {
                ConfirmOnline();
                return;
            }

            _seat++;
            if (_seat < _selected.Length)
            {
                Refresh();
                return;
            }

            Hide();
            _onConfirmed?.Invoke(_selected);
        }

        /// <summary>待機表示にしてから通知する。最後の1人だった場合は通知の後すぐ Hide されるため、この順にする</summary>
        private void ConfirmOnline()
        {
            SetSelecting(false);
            _titleText.text = "他のプレイヤーを待っています";
            _onOnlineConfirmed(_selected[_seat]);
        }

        private void Back()
        {
            _seat--;
            if (_seat >= 0)
            {
                Refresh();
                return;
            }

            Hide();
            _onBack?.Invoke();
        }

        private void Refresh()
        {
            _titleText.text = IsOnline ? "あなたのキャラを選んでね" : $"{SeatLabel()} のキャラを選んでね";
            // 端末を回したときに誰の番か一目で分かるよう、席の色で出す
            _titleText.color = GolfPlayerColors.Get(_seat);

            GolfCharacterData character = _catalog.Get(_selected[_seat]);
            _portrait.sprite = character.FrontSprite;
            _nameText.text = character.DisplayName;
            _distanceBar.Show(character.DistanceMultiplier);
            _straightnessBar.Show(character.StraightnessMultiplier);
        }

        private string SeatLabel()
        {
            string name = GolfPlayerColors.Name(_seat);
            return _types[_seat] == GolfPlayerType.Human ? name : $"{name}（NPC）";
        }
    }
}
