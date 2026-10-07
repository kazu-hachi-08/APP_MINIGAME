using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// オンライン対戦の編成確認（仕様書 §2.2）。ドラフト中は隠していたお互いの10体を、上に自分・下に相手で並べ、抽選されたステージも発表する。
    /// 表示時間は GameManager が決める
    /// </summary>
    public class DeckRevealPanel : MonoBehaviour
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [SerializeField] private Text _myNameLabel;
        [SerializeField] private Text _opponentNameLabel;
        [SerializeField] private Text _stageLabel;
        [Tooltip("並びがスロット順")]
        [SerializeField] private Image[] _myIcons;
        [SerializeField] private Text[] _myNames;
        [SerializeField] private Image[] _opponentIcons;
        [SerializeField] private Text[] _opponentNames;
        [Tooltip("じぶんペンギンのマスに出す「じぶん」の印。並びはスロット順")]
        [SerializeField] private GameObject[] _myCustomMarks;
        [SerializeField] private GameObject[] _opponentCustomMarks;

        /// <summary>どちらの端末でも自分 = Left（ゲストは反転済み）。絵も試合と同じく自分は青・相手は赤</summary>
        public void Show(IReadOnlyList<UnitStats> myDeck, IReadOnlyList<UnitStats> opponentDeck, string myName, string opponentName,
            string stageName)
        {
            _stageLabel.text = string.IsNullOrEmpty(stageName) ? string.Empty : $"ステージ：{stageName}";
            _myNameLabel.text = myName;
            _opponentNameLabel.text = opponentName;
            ShowRow(_myIcons, _myNames, _myCustomMarks, myDeck, Side.Left);
            ShowRow(_opponentIcons, _opponentNames, _opponentCustomMarks, opponentDeck, Side.Right);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowRow(Image[] icons, Text[] names, GameObject[] customMarks, IReadOnlyList<UnitStats> deck, Side side)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                bool hasUnit = i < deck.Count;
                icons[i].gameObject.SetActive(hasUnit);
                names[i].gameObject.SetActive(hasUnit);
                // じぶんペンギンは名前が自由なので、既存キャラと見分けられるよう印を付ける
                customMarks[i].SetActive(hasUnit && CustomUnitRules.IsCustomNo(deck[i].UnitNo));
                if (!hasUnit) continue;

                PenguinUnitData data = _catalog.Get(deck[i].UnitNo);
                UnitLabels.SetIcon(icons[i], data, side);
                names[i].text = UnitLabels.Name(data, deck[i].UnitNo);
            }
        }
    }
}
