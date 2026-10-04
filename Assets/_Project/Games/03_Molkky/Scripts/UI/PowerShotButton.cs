using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// パワーショットの ON/OFF ボタン。今の状態を表示し、押すと ThrowInput に切り替えを頼む。
    /// ON は色も変えて、うっかり強振のまま投げないよう一目で分かるようにする。
    /// 表示・非表示は GameManager が人間の構え中だけ出すよう制御する。
    /// </summary>
    public class PowerShotButton : MonoBehaviour
    {
        [SerializeField] private ThrowInput _input;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        [SerializeField] private string _offLabel = "ふつう";
        [SerializeField] private string _onLabel = "パワー!";
        [SerializeField] private Color _onColor = new Color(0.85f, 0.3f, 0.2f);

        private Color _offColor;

        private void Awake()
        {
            _offColor = _button.image.color;
            _button.onClick.AddListener(_input.TogglePowerShot);
            _input.PowerShotChanged += Refresh;
            Refresh(_input.IsPowerShot);
        }

        private void OnDestroy()
        {
            if (_input != null) _input.PowerShotChanged -= Refresh;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void Refresh(bool isPowerShot)
        {
            _label.text = isPowerShot ? _onLabel : _offLabel;
            _button.image.color = isPowerShot ? _onColor : _offColor;
        }
    }
}
