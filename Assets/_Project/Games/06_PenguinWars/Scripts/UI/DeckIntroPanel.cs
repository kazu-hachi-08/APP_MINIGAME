using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 試合前の編成発表（仕様書 §2.1）。ランダムに決まった10体を並べて見せるだけで、表示時間は GameManager が決める
    /// </summary>
    public class DeckIntroPanel : MonoBehaviour
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("枠の並び順がスロット順（ボタンの並び）と同じ")]
        [SerializeField] private Image[] _icons;
        [SerializeField] private Text[] _names;

        public void Show(IReadOnlyList<UnitStats> deck)
        {
            for (int i = 0; i < _icons.Length; i++)
            {
                bool hasUnit = i < deck.Count;
                _icons[i].gameObject.SetActive(hasUnit);
                _names[i].gameObject.SetActive(hasUnit);
                if (hasUnit) ShowUnit(i, deck[i].UnitNo);
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowUnit(int index, int unitNo)
        {
            PenguinUnitData data = _catalog.Get(unitNo);
            // 自分の編成なので、出撃ボタンと同じ左陣営（青）の立ち姿を見せる
            Sprite icon = data != null ? data.GetSprites(Side.Left).Icon : null;
            _icons[index].sprite = icon;
            _icons[index].enabled = icon != null;
            _names[index].text = data != null ? data.DisplayName : $"No.{unitNo}";
        }
    }
}
