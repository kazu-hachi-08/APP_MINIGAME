using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// ピン1本の見た目。ロジック用の Pin の地面座標を DepthProjector で擬似3Dへ変換して表示する。
    /// 倒れたら横倒しにするが、数字は常に正立させて読めるようにする（§5.2）。
    /// </summary>
    public class PinView : MonoBehaviour
    {
        private const string LabelFontName = "LegacyRuntime.ttf";

        // 数字を立っているピンの上寄りに置く。前の列のピンに下半分が隠れても読めるようにするため
        private const float StandingLabelHeightRatio = 0.72f;

        private Pin _pin;
        private DepthProjector _projector;
        private PinRackView _style;

        private Transform _body;
        private SpriteRenderer _bodyRenderer;
        private Transform _label;
        private MeshRenderer _labelRenderer;

        public void Bind(Pin pin, DepthProjector projector, PinRackView style)
        {
            _pin = pin;
            _projector = projector;
            _style = style;

            CreateBody();
            CreateLabel();
            LateUpdate();
        }

        private void CreateBody()
        {
            var bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(transform, false);
            _body = bodyObj.transform;
            _bodyRenderer = bodyObj.AddComponent<SpriteRenderer>();
            _bodyRenderer.sprite = ShapeSprites.Square;
        }

        private void CreateLabel()
        {
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(transform, false);
            _label = labelObj.transform;

            Font font = Resources.GetBuiltinResource<Font>(LabelFontName);
            var text = labelObj.AddComponent<TextMesh>();
            text.font = font;
            text.text = _pin.Number.ToString();
            text.fontSize = _style.LabelFontSize;
            text.characterSize = _style.LabelCharacterSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.color = _style.LabelColor;

            _labelRenderer = labelObj.GetComponent<MeshRenderer>();
            _labelRenderer.sharedMaterial = font.material;
        }

        private void LateUpdate()
        {
            if (_pin == null) return;

            Vector2 ground = _pin.GroundPosition;
            transform.position = _projector.Project(ground);
            transform.localScale = Vector3.one * _projector.ScaleAt(ground.y);

            int order = _projector.SortingOrderAt(ground.y);
            _bodyRenderer.sortingOrder = order;
            _labelRenderer.sortingOrder = order + 1;

            if (_pin.IsFallen)
            {
                ShowFallen();
            }
            else
            {
                ShowStanding();
            }
        }

        private void ShowStanding()
        {
            float width = _style.PinWidth;
            float height = _style.PinHeight;

            _body.localPosition = new Vector3(0f, height * 0.5f, 0f);
            _body.localScale = new Vector3(width, height, 1f);
            _bodyRenderer.color = _style.StandingColor;
            _label.localPosition = new Vector3(0f, height * StandingLabelHeightRatio, 0f);
        }

        private void ShowFallen()
        {
            float width = _style.PinWidth;
            float length = _style.PinHeight;

            // 左右どちらに倒れたかは見た目の手がかりにしかならないので、X方向のずれだけで表す
            float side = _pin.FallDirection.x >= 0f ? 1f : -1f;
            _body.localPosition = new Vector3(side * length * 0.25f, width * 0.5f, 0f);
            _body.localScale = new Vector3(length, width, 1f);
            _bodyRenderer.color = _style.FallenColor;
            _label.localPosition = new Vector3(side * length * 0.25f, width * 0.5f, 0f);
        }
    }
}
