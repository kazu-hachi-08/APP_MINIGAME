using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// オンライン対戦の編成確認（仕様書 §2.2）。ドラフト中は隠していたお互いの10体を、上に自分・下に相手で並べる。
    /// 表示時間は GameManager が決める
    /// </summary>
    public class DeckRevealPanel : MonoBehaviour
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private Text _myNameLabel;
        [SerializeField] private Text _opponentNameLabel;
        [Tooltip("並びがスロット順")]
        [SerializeField] private Image[] _myIcons;
        [SerializeField] private Text[] _myNames;
        [SerializeField] private Image[] _opponentIcons;
        [SerializeField] private Text[] _opponentNames;

        /// <summary>どちらの端末でも自分 = Left（ゲストは反転済み）。絵も試合と同じく自分は青・相手は赤</summary>
        public void Show(IReadOnlyList<UnitStats> myDeck, IReadOnlyList<UnitStats> opponentDeck, string myName, string opponentName)
        {
            _myNameLabel.text = myName;
            _opponentNameLabel.text = opponentName;
            ShowRow(_myIcons, _myNames, myDeck, Side.Left);
            ShowRow(_opponentIcons, _opponentNames, opponentDeck, Side.Right);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowRow(Image[] icons, Text[] names, IReadOnlyList<UnitStats> deck, Side side)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                bool hasUnit = i < deck.Count;
                icons[i].gameObject.SetActive(hasUnit);
                names[i].gameObject.SetActive(hasUnit);
                if (!hasUnit) continue;

                PenguinUnitData data = _catalog.Get(deck[i].UnitNo);
                Sprite icon = data != null ? data.GetSprites(side).Icon : null;
                icons[i].sprite = icon;
                icons[i].enabled = icon != null;
                names[i].text = data != null ? data.DisplayName : $"No.{deck[i].UnitNo}";
            }
        }
    }
}
