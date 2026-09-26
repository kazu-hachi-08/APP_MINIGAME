using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 「○○の番」の全画面表示（§10.2）。1台を回して遊ぶとき、前の人の指がそのまま投擲にならないよう
    /// 人間の番はタップするまで待つ。NPCの番は短く出して自動で閉じる（§9.5）。
    /// </summary>
    public class TurnBannerView : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _hintText;
        [SerializeField] private Button _tapArea;

        private bool _tapped;

        private void Awake()
        {
            _tapArea.onClick.AddListener(() => _tapped = true);
        }

        public IEnumerator Play(string title, Color color, bool waitForTap, float autoCloseSeconds)
        {
            _titleText.text = title;
            _titleText.color = color;
            _hintText.gameObject.SetActive(waitForTap);
            _tapArea.interactable = waitForTap;
            _tapped = false;
            gameObject.SetActive(true);

            if (waitForTap)
            {
                yield return new WaitUntil(() => _tapped);
            }
            else
            {
                yield return new WaitForSeconds(autoCloseSeconds);
            }

            gameObject.SetActive(false);
        }
    }
}
