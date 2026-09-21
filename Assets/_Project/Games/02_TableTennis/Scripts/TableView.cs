using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 卓球台・センターライン・ネットを TableLayout の変換から実行時に描画する。
    /// 台の形は投影パラメータ次第で変わるため、素材を焼き込まずコードで組み立てている
    /// （Sceneに大きなオブジェクトを持たせない = Git コンフリクト回避にもなる）。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TableView : MonoBehaviour
    {
        [SerializeField] private TableLayout _table;
        [SerializeField] private Material _material;

        [Header("Colors")]
        [SerializeField] private Color _surfaceColor = new Color(0.08f, 0.32f, 0.5f);
        [SerializeField] private Color _lineColor = new Color(0.95f, 0.96f, 0.98f);
        [SerializeField] private Color _netColor = new Color(0.85f, 0.88f, 0.95f, 0.85f);

        [Header("Line Width (world unit at near edge)")]
        [SerializeField] private float _lineWidth = 0.06f;

        [Header("Sorting")]
        [SerializeField] private int _surfaceSortingOrder = -100;
        [SerializeField] private int _lineSortingOrder = -90;
        [SerializeField] private int _netSortingOrder = -50;

        private void Start()
        {
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

            var mesh = new Mesh { name = "TableSurface" };
            mesh.vertices = new[] { nearLeft, farLeft, farRight, nearRight };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.colors = new[] { _surfaceColor, _surfaceColor, _surfaceColor, _surfaceColor };
            mesh.RecalculateBounds();

            GetComponent<MeshFilter>().mesh = mesh;
            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _material;
            meshRenderer.sortingOrder = _surfaceSortingOrder;
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

        /// <summary>ネットは台の中央（z=0）に立てた面として、上端と下端を結ぶ線で表現する</summary>
        private void BuildNet()
        {
            float halfWidth = _table.NetHalfWidth;
            float netHeight = _table.NetHeight;

            var top = CreateLine("NetTop", _netColor, _netSortingOrder, loop: false, positionCount: 2);
            top.SetPosition(0, ToLocal(-halfWidth, 0f, netHeight));
            top.SetPosition(1, ToLocal(halfWidth, 0f, netHeight));

            var bottom = CreateLine("NetBottom", _netColor, _netSortingOrder, loop: false, positionCount: 2);
            bottom.SetPosition(0, ToLocal(-halfWidth, 0f, 0f));
            bottom.SetPosition(1, ToLocal(halfWidth, 0f, 0f));

            var leftPost = CreateLine("NetPostLeft", _netColor, _netSortingOrder, loop: false, positionCount: 2);
            leftPost.SetPosition(0, ToLocal(-halfWidth, 0f, 0f));
            leftPost.SetPosition(1, ToLocal(-halfWidth, 0f, netHeight));

            var rightPost = CreateLine("NetPostRight", _netColor, _netSortingOrder, loop: false, positionCount: 2);
            rightPost.SetPosition(0, ToLocal(halfWidth, 0f, 0f));
            rightPost.SetPosition(1, ToLocal(halfWidth, 0f, netHeight));
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
