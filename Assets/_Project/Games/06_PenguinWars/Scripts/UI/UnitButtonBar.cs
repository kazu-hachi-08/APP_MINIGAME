using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 出撃ボタン 5個×2ページ（仕様書 §7.1）。毎フレーム BattleWorld を読んで表示するだけで、お金を直接いじらない
    /// （Phase 10 でゲスト画面にも同じ UI を使うため）
    /// </summary>
    public class UnitButtonBar : MonoBehaviour
    {
        public const int SlotsPerPage = 5;
        private const int PageCount = 2;

        [SerializeField] private BattleRunner _battleRunner;
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private UnitButton[] _buttons;
        [SerializeField] private Button _pageButton;
        [SerializeField] private Text _pageLabel;

        private int _page;
        // ページが変わったときだけ名前・コストを書き直す（毎フレーム文字列を作らないため）
        private int _shownPage = -1;

        private void Awake()
        {
            for (int i = 0; i < _buttons.Length; i++)
            {
                int visibleIndex = i;
                _buttons[i].Bind(() => SpawnVisibleSlot(visibleIndex));
            }
            _pageButton.onClick.AddListener(TogglePage);
        }

        public void TogglePage()
        {
            _page = (_page + 1) % PageCount;
        }

        /// <summary>表示中のページの visibleIndex 番目（0〜4）を出撃させる。キー 1〜5 もここを通す</summary>
        public void SpawnVisibleSlot(int visibleIndex)
        {
            _battleRunner.Enqueue(BattleCommand.Spawn(Side.Left, ToSlot(visibleIndex)));
        }

        private void Update()
        {
            BattleWorld world = _battleRunner.World;
            if (world == null) return;

            if (_page != _shownPage) ApplyPage(world);
            RefreshButtons(world);
        }

        private int ToSlot(int visibleIndex)
        {
            return _page * SlotsPerPage + visibleIndex;
        }

        private void ApplyPage(BattleWorld world)
        {
            _shownPage = _page;
            _pageLabel.text = $"切替\n{_page + 1}/{PageCount}";

            IReadOnlyList<UnitStats> deck = world.GetDeck(Side.Left);
            for (int i = 0; i < _buttons.Length; i++)
            {
                int slot = ToSlot(i);
                if (slot >= deck.Count)
                {
                    _buttons[i].SetEmpty();
                    continue;
                }

                UnitStats stats = deck[slot];
                PenguinUnitData data = _catalog.Get(stats.UnitNo);
                string displayName = data != null ? data.DisplayName : $"No.{stats.UnitNo}";
                _buttons[i].SetUnit(stats.UnitNo, displayName, stats.Cost);
            }
        }

        private void RefreshButtons(BattleWorld world)
        {
            int deckCount = world.GetDeck(Side.Left).Count;
            for (int i = 0; i < _buttons.Length; i++)
            {
                int slot = ToSlot(i);
                if (slot >= deckCount) continue;

                _buttons[i].Refresh(world.CanSpawn(Side.Left, slot), world.GetSlot(Side.Left, slot).RemainingRatio);
            }
        }
    }
}
