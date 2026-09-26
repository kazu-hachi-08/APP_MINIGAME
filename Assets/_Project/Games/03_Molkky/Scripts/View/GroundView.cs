using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 芝生の地面（奥ほど狭い台形）と投擲ラインを、DepthProjector の変換でメッシュとして描く。
    /// 台形はスプライトでは作れないため、頂点カラー付きの小さなメッシュにしている。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GroundView : MonoBehaviour
    {
        [SerializeField] private DepthProjector _projector;
        [SerializeField] private MolkkyPhysicsSettings _settings;

        [Header("Ground (ground units)")]
        [Tooltip("画面の左右端より外まで広げて、地面の切れ目が見えないようにする")]
        [SerializeField] private float _groundHalfWidth = 12f;
        [SerializeField] private float _groundNearZ = -4f;
        [SerializeField] private float _groundFarZ = 60f;
        [SerializeField] private Color _grassNearColor = new Color(0.36f, 0.66f, 0.3f);
        [SerializeField] private Color _grassFarColor = new Color(0.52f, 0.76f, 0.42f);

        [Header("Throw Line")]
        [SerializeField] private float _lineThickness = 0.06f;
        [SerializeField] private Color _lineColor = Color.white;

        // 地面は常に全ての物より奥に描く
        [SerializeField] private int _sortingOrder = -30000;

        private void Start()
        {
            var mesh = new Mesh { name = "MolkkyGround" };

            float lineHalfWidth = _settings.ThrowLineHalfWidth;
            float lineHalfThickness = _lineThickness * 0.5f;

            var vertices = new Vector3[8];
            var colors = new Color[8];
            SetQuad(vertices, colors, 0, -_groundHalfWidth, _groundHalfWidth, _groundNearZ, _groundFarZ,
                _grassNearColor, _grassFarColor);
            SetQuad(vertices, colors, 4, -lineHalfWidth, lineHalfWidth, -lineHalfThickness, lineHalfThickness,
                _lineColor, _lineColor);

            mesh.vertices = vertices;
            mesh.colors = colors;
            // 芝生 → 投擲ラインの順に並べ、同じメッシュ内でラインを上に描く
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6 };
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sortingOrder = _sortingOrder;
        }

        /// <summary>地面上の矩形（手前2点・奥2点）を画面座標へ変換して頂点に入れる</summary>
        private void SetQuad(Vector3[] vertices, Color[] colors, int start,
            float xMin, float xMax, float zNear, float zFar, Color nearColor, Color farColor)
        {
            vertices[start] = _projector.Project(new Vector2(xMin, zNear));
            vertices[start + 1] = _projector.Project(new Vector2(xMax, zNear));
            vertices[start + 2] = _projector.Project(new Vector2(xMax, zFar));
            vertices[start + 3] = _projector.Project(new Vector2(xMin, zFar));

            colors[start] = nearColor;
            colors[start + 1] = nearColor;
            colors[start + 2] = farColor;
            colors[start + 3] = farColor;
        }
    }
}
