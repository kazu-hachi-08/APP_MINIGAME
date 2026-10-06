using System;
using System.Collections.Generic;
using MiniGame.PenguinWars.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.PenguinWars
{
    /// <summary>
    /// 試合前の編成発表（仕様書 §2.1）。保存した編成10体とステージ名・特別ルールを見せるだけで、表示時間は GameManager が決める。
    /// 編成制限で出せないキャラは暗くして、出撃ボタンが暗い理由を先に知らせる
    /// </summary>
    public class DeckIntroPanel : MonoBehaviour
    {
        [SerializeField] private PenguinUnitCatalog _catalog;
        [Tooltip("枠の並び順がスロット順（ボタンの並び）と同じ")]
        [SerializeField] private Image[] _icons;
        [SerializeField] private Text[] _names;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _rulesLabel;
        [SerializeField] private Color _bannedTint = new Color(0.25f, 0.25f, 0.25f, 0.8f);
        [SerializeField] private Color _bannedNameColor = new Color(0.5f, 0.5f, 0.5f);

        /// <param name="isAllowed">スロット番号 → 出せるか（BattleWorld.IsAllowedByRules）</param>
        public void Show(IReadOnlyList<UnitStats> deck, string title, string rules, Func<int, bool> isAllowed)
        {
            _titleLabel.text = title;
            _rulesLabel.text = rules;
            for (int i = 0; i < _icons.Length; i++)
            {
                bool hasUnit = i < deck.Count;
                _icons[i].gameObject.SetActive(hasUnit);
                _names[i].gameObject.SetActive(hasUnit);
                if (hasUnit) ShowUnit(i, deck[i].UnitNo, isAllowed(i));
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ShowUnit(int index, int unitNo, bool allowed)
        {
            PenguinUnitData data = _catalog.Get(unitNo);
            // 自分の編成なので、出撃ボタンと同じ左陣営（青）の立ち姿を見せる
            Sprite icon = data != null ? data.GetSprites(Side.Left).Icon : null;
            _icons[index].sprite = icon;
            _icons[index].enabled = icon != null;
            _names[index].text = data != null ? data.DisplayName : $"No.{unitNo}";
            _icons[index].color = allowed ? Color.white : _bannedTint;
            _names[index].color = allowed ? Color.white : _bannedNameColor;
        }
    }
}
