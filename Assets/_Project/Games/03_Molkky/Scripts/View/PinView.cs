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

        private bool _wasFallen;
        private float _standUpStartTime = float.NegativeInfinity;

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

            // 倒れていたピンが立て直された瞬間から、起き上がるアニメーションを始める
            if (_wasFallen && !_pin.IsFallen) _standUpStartTime = Time.time;
            _wasFallen = _pin.IsFallen;

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
            // 起き上がり中は背の低い状態から伸ばし、少し行き過ぎてから戻して「ピョコッ」と立たせる
            float rise = EaseOutBack(Mathf.Clamp01((Time.time - _standUpStartTime) / _style.StandUpDuration));
            float height = Mathf.LerpUnclamped(width, _style.PinHeight, rise);

            _bodyRenderer.sprite = _style.StandingSprite;
            _bodyRenderer.flipX = false;
            _body.localPosition = new Vector3(0f, height * 0.5f, 0f);
            SetSize(width, height);
            _bodyRenderer.color = _style.StandingColor;
            _label.localPosition = new Vector3(0f, height * StandingLabelHeightRatio, 0f);
        }

        private void ShowFallen()
        {
            float width = _style.PinWidth;
            float length = _style.PinHeight;

            // 左右どちらに倒れたかは見た目の手がかりにしかならないので、X方向のずれだけで表す
            float side = _pin.FallDirection.x >= 0f ? 1f : -1f;
            _bodyRenderer.sprite = _style.FallenSprite;
            // スプライトは頭（斜めに切った側）が右向きなので、左に倒れたら反転する
            _bodyRenderer.flipX = side < 0f;
            _body.localPosition = new Vector3(side * length * 0.25f, width * 0.5f, 0f);
            SetSize(length, width);
            _bodyRenderer.color = _style.FallenColor;
            _label.localPosition = new Vector3(side * length * 0.25f, width * 0.5f, 0f);
        }

        /// <summary>スプライトの元の大きさに関係なく、地面単位の幅・高さで表示する</summary>
        private void SetSize(float width, float height)
        {
            Vector2 spriteSize = _bodyRenderer.sprite.bounds.size;
            _body.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.70158f;
            float u = t - 1f;
            return 1f + (overshoot + 1f) * u * u * u + overshoot * u * u;
        }
    }
}
