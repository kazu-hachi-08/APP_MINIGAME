using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.LifeGame
{
    /// <summary>止まったマスの結果などの文面を出し、タップで閉じる（仕様書 §3.3）</summary>
    public class EventPopupView : MonoBehaviour
    {
        [SerializeField] private Text _bodyText;
        [SerializeField] private Button _tapArea;

        private bool _tapped;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
        }

        public IEnumerator Play(string body)
        {
            _bodyText.text = body;
            _tapped = false;
            gameObject.SetActive(true);

            yield return new WaitUntil(() => _tapped);

            gameObject.SetActive(false);
        }
    }
}
