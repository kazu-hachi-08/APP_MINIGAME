using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 試合前のテーマ選択（仕様書 §2.1）。ボタンを押したらすぐ決定する（選び直しはプレイヤー設定の「戻る」でできるため）。
    /// </summary>
    public class ThemeSelectPanel : MonoBehaviour
    {
        [Tooltip("テーマの並びと同じ順")]
        [SerializeField] private Button[] _themeButtons;

        private Action<int> _onSelected;

        private void Awake()
        {
            for (int i = 0; i < _themeButtons.Length; i++)
            {
                int index = i;
                _themeButtons[i].onClick.AddListener(() => Select(index));
            }
        }

        /// <summary>選んだテーマの番号を返す（フェーズ5のオンラインで番号だけ送れば済むように）</summary>
        public void Show(IReadOnlyList<LifeThemeData> themes, Action<int> onSelected)
        {
            _onSelected = onSelected;
            for (int i = 0; i < _themeButtons.Length; i++)
            {
                bool exists = i < themes.Count;
                _themeButtons[i].gameObject.SetActive(exists);
                if (exists) _themeButtons[i].GetComponentInChildren<Text>().text = themes[i].DisplayName;
            }

            gameObject.SetActive(true);
        }

        private void Select(int index)
        {
            gameObject.SetActive(false);
            _onSelected?.Invoke(index);
        }
    }
}
