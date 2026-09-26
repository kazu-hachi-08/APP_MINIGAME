using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 縦投げ／横投げの切り替えボタン（§7.5）。今の投げ方を表示し、押すと ThrowInput に切り替えを頼む。
    /// 表示・非表示は GameManager が人間の構え中だけ出すよう制御する。
    /// </summary>
    public class ThrowStyleButton : MonoBehaviour
    {
        [SerializeField] private ThrowInput _input;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        [SerializeField] private string _verticalLabel = "縦投げ";
        [SerializeField] private string _horizontalLabel = "横投げ";

        private void Awake()
        {
            _button.onClick.AddListener(_input.ToggleStyle);
            _input.StyleChanged += Refresh;
            Refresh(_input.Style);
        }

        private void OnDestroy()
        {
            if (_input != null) _input.StyleChanged -= Refresh;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void Refresh(ThrowStyle style)
        {
            _label.text = style == ThrowStyle.Vertical ? _verticalLabel : _horizontalLabel;
        }
    }
}
