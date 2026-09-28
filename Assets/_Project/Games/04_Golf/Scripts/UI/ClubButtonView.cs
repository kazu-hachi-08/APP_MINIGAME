using UnityEngine;
using UnityEngine.UI;

namespace MiniGame.Golf
{
    /// <summary>
    /// クラブ名と最大飛距離を出し、タップでクラブを切り替えるボタン。
    /// </summary>
    public class ClubButtonView : MonoBehaviour
    {
        [SerializeField] private ShotInput _input;
        [SerializeField] private ClubSelector _clubs;
        [SerializeField] private GolfPhysicsSettings _settings;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        private void Awake()
        {
            _button.onClick.AddListener(OnClicked);
        }

        private void LateUpdate()
        {
            GolfClubData club = _clubs.Current;
            float yards = _clubs.MaxDistance(club, _input.Direction) * _settings.YardsPerUnit;
            _label.text = $"{club.DisplayName}\n{Mathf.RoundToInt(yards)}y";

            // グリーン上（パター固定）とゲージ操作中は変えられないことが分かるように薄くする
            _button.interactable = _input.CanAim && _clubs.CanChange;
        }

        private void OnClicked()
        {
            if (_input.CanAim) _clubs.Next();
        }
    }
}
