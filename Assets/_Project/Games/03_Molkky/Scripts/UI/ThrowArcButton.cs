using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 低め／山なりの切り替えボタン。今の軌道を表示し、押すと ThrowInput に切り替えを頼む。
    /// 表示・非表示は GameManager が人間の構え中だけ出すよう制御する。
    /// </summary>
    public class ThrowArcButton : MonoBehaviour
    {
        [SerializeField] private ThrowInput _input;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        [SerializeField] private string _lowLabel = "低め";
        [SerializeField] private string _highLabel = "山なり";

        private void Awake()
        {
            _button.onClick.AddListener(_input.ToggleArc);
            _input.ArcChanged += Refresh;
            Refresh(_input.Arc);
        }

        private void OnDestroy()
        {
            if (_input != null) _input.ArcChanged -= Refresh;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        private void Refresh(ThrowArc arc)
        {
            _label.text = arc == ThrowArc.High ? _highLabel : _lowLabel;
        }
    }
}
