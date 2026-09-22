using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 試合開始前に5段階の難易度を選ばせるパネル。選択したら閉じてコールバックへ通知する。
    /// </summary>
    public class DifficultySelectPanel : MonoBehaviour
    {
        [SerializeField] private Button[] _levelButtons;

        private Action<int> _onSelected;

        private void Awake()
        {
            for (int i = 0; i < _levelButtons.Length; i++)
            {
                int level = i + 1;
                _levelButtons[i].onClick.AddListener(() => Select(level));
            }
        }

        public void Show(Action<int> onSelected)
        {
            _onSelected = onSelected;
            gameObject.SetActive(true);
        }

        private void Select(int level)
        {
            gameObject.SetActive(false);
            _onSelected?.Invoke(level);
            _onSelected = null;
        }
    }
}
