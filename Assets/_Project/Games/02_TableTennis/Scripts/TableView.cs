using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 卓球台・センターライン・ネット・台の側面と脚を TableLayout の変換から実行時に描画する。
    /// 台の形は投影パラメータ次第で変わるため、素材を焼き込まずコードで組み立てている
    /// （Sceneに大きなオブジェクトを持たせない = Git コンフリクト回避にもなる）。
    /// 質感の素材（天板・ネット）は差し込めるようにし、未設定なら単色で描く。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TableView : MonoBehaviour
    {
        [SerializeField] private TableLayout _table;
        [SerializeField] private Material _material;

        [Header("Sprites (未設定なら単色で描く)")]
        [SerializeField] private Sprite _surfaceSprite;
        [SerializeField] private Sprite _netSprite;

        [Header("Colors")]
        [SerializeField] private Color _surfaceColor = new Color(0.55f, 0.75f, 0.95f);
        [SerializeField] private Color _lineColor = new Color(0.95f, 0.96f, 0.98f);
        [SerializeField] private Color _netColor = new Color(0.85f, 0.88f, 0.95f, 0.9f);
        [SerializeField] private Color _skirtColor = new Color(0.05f, 0.18f, 0.3f);
        [SerializeField] private Color _legColor = new Color(0.12f, 0.13f, 0.17f);

        [Header("Line Width (world unit at near edge)")]
        [SerializeField] private float _lineWidth = 0.06f;

        [Header("Table Body (world unit)")]
        [Tooltip("手前から見える天板の厚み")]
        [SerializeField] private float _skirtHeight = 0.35f;

        [SerializeField] private float _legHeight = 1.4f;
        [SerializeField] private float _legWidth = 0.28f;

        [Header("Texture Tiling")]
        [SerializeField] private Vector2 _surfaceTiling = new Vector2(3f, 4f);
        [SerializeField] private Vector2 _netTiling = new Vector2(12f, 1.5f);

        [Header("Sorting")]
        [SerializeField] private int _legSortingOrder = -130;
        [SerializeField] private int _skirtSortingOrder = -110;
        [SerializeField] private int _surfaceSortingOrder = -100;
        [SerializeField] private int _lineSortingOrder = -90;
        [SerializeField] private int _netSortingOrder = -50;

        private void Start()
        {
            BuildLegs();
            BuildSkirt();
            BuildSurface();
            BuildOutline();
            BuildCenterLine();
            BuildNet();
        }

        /// <summary>台の面（手前が広く奥が狭い台形）を4頂点のメッシュで塗る</summary>
        private void BuildSurface()
        {
            float halfWidth = _table.HalfWidth;
            Vector3 nearLeft = ToLocal(-halfWidth, _table.PlayerEndZ);
            Vector3 nearRight = ToLocal(halfWidth, _table.PlayerEndZ);
            Vector3 farLeft = ToLocal(-halfWidth, _table.OpponentEndZ);
            Vector3 farRight = ToLocal(halfWidth, _table.OpponentEndZ);

            var mesh = BuildQuadMesh("TableSurface", nearLeft, farLeft, farRight, nearRight,
                _surfaceColor, _surfaceTiling);

            GetComponent<MeshFilter>().mesh = mesh;
            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CreateMaterial(_surfaceSprite);
            meshRenderer.sortingOrder = _surfaceSortingOrder;
        }

        /// <summary>手前側の天板の厚み。ここがあるだけで台が板に見える</summary>
        private void BuildSkirt()
        {
            float halfWidth = _table.HalfWidth;
            Vector3 topLeft = ToLocal(-halfWidth, _table.PlayerEndZ);
            Vector3 topRight = ToLocal(halfWidth, _table.PlayerEndZ);
            Vector3 bottomLeft = topLeft + Vector3.down * _skirtHeight;
            Vector3 bottomRight = topRight + Vector3.down * _skirtHeight;

            CreateQuad("TableSkirt", bottomLeft, topLeft, topRight, bottomRight,
                _skirtColor, null, Vector2.one, _skirtSortingOrder);
        }

        /// <summary>手前側の脚。奥の脚は天板に隠れるため描かない</summary>
        private void BuildLegs()
        {
            float legX = _table.HalfWidth * 0.72f;
            CreateLeg("TableLegLeft", -legX);
            CreateLeg("TableLegRight", legX);
        }

        private void CreateLeg(string name, float x)
        {
            Vector3 top = ToLocal(x, _table.PlayerEndZ) + Vector3.down * _skirtHeight;
            float half = _legWidth * 0.5f;

            CreateQuad(name,
                top + new Vector3(-half, -_legHeight, 0f),
                top + new Vector3(-half, 0f, 0f),
                top + new Vector3(half, 0f, 0f),
                top + new Vector3(half, -_legHeight, 0f),
                _legColor, null, Vector2.one, _legSortingOrder);
        }

        private void BuildOutline()
        {
            float halfWidth = _table.HalfWidth;
            var line = CreateLine("Outline", _lineColor, _lineSortingOrder, loop: true, positionCount: 4);
            line.SetPosition(0, ToLocal(-halfWidth, _table.PlayerEndZ));
            line.SetPosition(1, ToLocal(halfWidth, _table.PlayerEndZ));
            line.SetPosition(2, ToLocal(halfWidth, _table.OpponentEndZ));
            line.SetPosition(3, ToLocal(-halfWidth, _table.OpponentEndZ));
        }

        private void BuildCenterLine()
        {
            var line = CreateLine("CenterLine", _lineColor, _lineSortingOrder, loop: false, positionCount: 2);
            line.SetPosition(0, ToLocal(0f, _table.PlayerEndZ));
            line.SetPosition(1, ToLocal(0f, _table.OpponentEndZ));
        }

        /// <summary>ネットは台の中央（z=0）に立てた面として描き、上端と支柱を線で締める</summary>
        private void BuildNet()
        {
            float halfWidth = _table.NetHalfWidth;
            float netHeight = _table.NetHeight;

            CreateQuad("NetMesh",
                ToLocal(-halfWidth, 0f, 0f),
                ToLocal(-halfWidth, 0f, netHeight),
                ToLocal(halfWidth, 0f, netHeight),
                ToLocal(halfWidth, 0f, 0f),
                _netColor, _netSprite, _netTiling, _netSortingOrder);

            var top = CreateLine("NetTop", _netColor, _netSortingOrder, loop: false, positionCount: 2);
            top.SetPosition(0, ToLocal(-halfWidth, 0f, netHeight));
            top.SetPosition(1, ToLocal(halfWidth, 0f, netHeight));

            var leftPost = CreateLine("NetPostLeft", _netColor, _netSortingOrder, loop: false, positionCount: 2);
            leftPost.SetPosition(0, ToLocal(-halfWidth, 0f, 0f));
            leftPost.SetPosition(1, ToLocal(-halfWidth, 0f, netHeight));

            var rightPost = CreateLine("NetPostRight", _netColor, _netSortingOrder, loop: false, positionCount: 2);
            rightPost.SetPosition(0, ToLocal(halfWidth, 0f, 0f));
            rightPost.SetPosition(1, ToLocal(halfWidth, 0f, netHeight));
        }

        // ------------------------------------------------------------------
        // 生成ヘルパー
        // ------------------------------------------------------------------

        /// <summary>頂点は左下→左上→右上→右下の順で渡す</summary>
        private void CreateQuad(string name, Vector3 bottomLeft, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight,
            Color color, Sprite sprite, Vector2 tiling, int sortingOrder)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);

            obj.AddComponent<MeshFilter>().mesh =
                BuildQuadMesh(name, bottomLeft, topLeft, topRight, bottomRight, color, tiling);

            var meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = CreateMaterial(sprite);
            meshRenderer.sortingOrder = sortingOrder;
        }

        private static Mesh BuildQuadMesh(string name, Vector3 bottomLeft, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight,
            Color color, Vector2 tiling)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { bottomLeft, topLeft, topRight, bottomRight };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.colors = new[] { color, color, color, color };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, tiling.y),
                new Vector2(tiling.x, tiling.y),
                new Vector2(tiling.x, 0f)
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>素材ごとにテクスチャが異なるため、共有マテリアルから複製して使う</summary>
        private Material CreateMaterial(Sprite sprite)
        {
            var material = new Material(_material);
            if (sprite != null)
            {
                material.mainTexture = sprite.texture;
            }
            return material;
        }

        private LineRenderer CreateLine(string name, Color color, int sortingOrder, bool loop, int positionCount)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);

            var line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = positionCount;
            line.widthMultiplier = _lineWidth;
            line.numCapVertices = 0;
            line.sharedMaterial = _material;
            line.startColor = color;
            line.endColor = color;
            line.sortingOrder = sortingOrder;
            return line;
        }

        private Vector3 ToLocal(float x, float z, float height = 0f)
        {
            Vector2 projected = _table.Project(new Vector3(x, height, z));
            return new Vector3(projected.x, projected.y, 0f) - transform.position;
        }
    }
}
