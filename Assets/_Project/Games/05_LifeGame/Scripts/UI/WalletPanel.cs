using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 財布：持ち物の一覧と約束手形の返済（仕様書 §3.3・§7.6）。
    /// 返済してよいかはルール（LifeRules.IsValid）が決めるので、ここは表示とボタンだけを持つ。
    /// </summary>
    public class WalletPanel : MonoBehaviour
    {
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _repayButton;
        [SerializeField] private Button _closeButton;

        /// <summary>返済ボタン（1枚）が押された</summary>
        public event Action RepayRequested;

        private void Awake()
        {
            _repayButton.onClick.AddListener(() => RepayRequested?.Invoke());
            _closeButton.onClick.AddListener(Hide);
        }

        public bool IsOpen => gameObject.activeSelf;

        public void Show(string body, bool canRepay)
        {
            gameObject.SetActive(true);
            Refresh(body, canRepay);
        }

        public void Refresh(string body, bool canRepay)
        {
            _bodyText.text = body;
            _repayButton.interactable = canRepay;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
