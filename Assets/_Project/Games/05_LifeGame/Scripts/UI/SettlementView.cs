using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 1人ずつの精算の内訳（仕様書 §8）。家の売却ルーレットが見えるよう、画面の上寄りに出して下のルーレットは隠さない。
    /// 何をいつ出すかは LifeGameManager が決め、ここは行を足して見せるだけにする。
    /// </summary>
    public class SettlementView : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;
        [SerializeField] private Text _moneyText;
        [SerializeField] private Text _hintText;
        [SerializeField] private Button _tapArea;

        private bool _tapped;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
        }

        public void Begin(string title, Color color, string money)
        {
            _titleText.text = title;
            _titleText.color = color;
            _bodyText.text = "";
            _moneyText.text = money;
            _hintText.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void AddLine(string line, string money)
        {
            _bodyText.text = _bodyText.text.Length == 0 ? line : $"{_bodyText.text}\n{line}";
            SetMoney(money);
        }

        public void SetMoney(string money)
        {
            _moneyText.text = money;
        }

        /// <summary>内訳を読み終えたら次の人へ進めるよう、タップを待つ</summary>
        public IEnumerator WaitForTap()
        {
            _hintText.gameObject.SetActive(true);
            _tapped = false;
            yield return new WaitUntil(() => _tapped);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
