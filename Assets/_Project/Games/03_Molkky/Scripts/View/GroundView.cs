using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 芝生の地面（奥ほど狭い台形）と投擲ラインを、DepthProjector の変換でメッシュとして描く。
    /// 台形はスプライトでは作れないため、頂点カラー付きの小さなメッシュにしている。
    /// 芝は奥行き方向に刈り目の縞を入れる。縞の間隔が奥ほど詰まって見えるので、遠近感が強く出る。
    /// （テクスチャを台形に貼ると歪むため、縞は頂点カラーで作っている）
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GroundView : MonoBehaviour
    {
        private const int VerticesPerQuad = 4;
        private const int IndicesPerQuad = 6;

        [SerializeField] private DepthProjector _projector;
        [SerializeField] private MolkkyPhysicsSettings _settings;

        [Header("Ground (ground units)")]
        [Tooltip("画面の左右端より外まで広げて、地面の切れ目が見えないようにする")]
        [SerializeField] private float _groundHalfWidth = 12f;
        [SerializeField] private float _groundNearZ = -4f;
        [SerializeField] private float _groundFarZ = 60f;
        [SerializeField] private Color _grassNearColor = new Color(0.36f, 0.66f, 0.3f);
        [SerializeField] private Color _grassFarColor = new Color(0.52f, 0.76f, 0.42f);

        [Header("Mowing Stripes")]
        [Tooltip("刈り目の縞1本の奥行き（地面単位）")]
        [SerializeField] private float _stripeLength = 1f;
        [Tooltip("1本おきに明るくする量")]
        [Range(0f, 0.3f)] [SerializeField] private float _stripeContrast = 0.08f;

        [Header("Throw Line")]
        [SerializeField] private float _lineThickness = 0.06f;
        [SerializeField] private Color _lineColor = Color.white;

        // 地面は常に全ての物より奥に描く
        [SerializeField] private int _sortingOrder = -30000;

        private void Start()
        {
            int stripeCount = Mathf.CeilToInt((_groundFarZ - _groundNearZ) / _stripeLength);
            int quadCount = stripeCount + 1; // 縞＋投擲ライン

            var vertices = new Vector3[quadCount * VerticesPerQuad];
            var colors = new Color[quadCount * VerticesPerQuad];
            var triangles = new int[quadCount * IndicesPerQuad];

            // 手前から奥へ順に並べ、最後の投擲ラインが芝より上に描かれるようにする
            for (int i = 0; i < stripeCount; i++)
            {
                float zNear = _groundNearZ + i * _stripeLength;
                float zFar = Mathf.Min(zNear + _stripeLength, _groundFarZ);
                Color color = StripeColor(i, zNear);
                SetQuad(vertices, colors, triangles, i, -_groundHalfWidth, _groundHalfWidth, zNear, zFar, color);
            }

            float lineHalfThickness = _lineThickness * 0.5f;
            SetQuad(vertices, colors, triangles, stripeCount, -_settings.ThrowLineHalfWidth, _settings.ThrowLineHalfWidth,
                -lineHalfThickness, lineHalfThickness, _lineColor);

            var mesh = new Mesh { name = "MolkkyGround", vertices = vertices, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sortingOrder = _sortingOrder;
        }

        private Color StripeColor(int index, float z)
        {
            float t = Mathf.InverseLerp(_groundNearZ, _groundFarZ, z);
            Color baseColor = Color.Lerp(_grassNearColor, _grassFarColor, t);
            return index % 2 == 0 ? baseColor : Color.Lerp(baseColor, Color.white, _stripeContrast);
        }

        /// <summary>地面上の矩形（手前2点・奥2点）を画面座標へ変換して、quadIndex 番目の四角形として入れる</summary>
        private void SetQuad(Vector3[] vertices, Color[] colors, int[] triangles, int quadIndex,
            float xMin, float xMax, float zNear, float zFar, Color color)
        {
            int v = quadIndex * VerticesPerQuad;
            vertices[v] = _projector.Project(new Vector2(xMin, zNear));
            vertices[v + 1] = _projector.Project(new Vector2(xMax, zNear));
            vertices[v + 2] = _projector.Project(new Vector2(xMax, zFar));
            vertices[v + 3] = _projector.Project(new Vector2(xMin, zFar));

            for (int i = 0; i < VerticesPerQuad; i++) colors[v + i] = color;

            int t = quadIndex * IndicesPerQuad;
            triangles[t] = v;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 3;
            triangles[t + 5] = v + 2;
        }
    }
}
