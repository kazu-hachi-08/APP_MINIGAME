using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 卓球台の寸法と、コート座標から画面座標への擬似3D変換をまとめて担当する。
    /// コート座標は x:左右 / y:台からの高さ / z:奥行き（-が自分側・+が相手側）の3成分。
    /// ボール・ラケット・台の表示が同じ変換を共有できるよう、変換をここ1箇所に集約している。
    /// </summary>
    public class TableLayout : MonoBehaviour
    {
        [Header("Table Size (m)")]
        [SerializeField] private float _halfWidth = 0.7625f;
        [SerializeField] private float _halfLength = 1.37f;
        [SerializeField] private float _netHeight = 0.1525f;

        [Header("Pseudo 3D Projection")]
        [Tooltip("手前端で 1m が画面上の何ワールド単位になるか")]
        [SerializeField] private float _nearScale = 3.0f;
        [Tooltip("奥端で 1m が画面上の何ワールド単位になるか。小さいほど奥行きが強調される")]
        [SerializeField] private float _farScale = 1.4f;
        [SerializeField] private float _nearBaseY = -3.0f;
        [SerializeField] private float _farBaseY = 1.0f;

        public float HalfWidth => _halfWidth;
        public float HalfLength => _halfLength;
        public float NetHeight => _netHeight;
        public float NearScale => _nearScale;

        /// <summary>自分側の台の端</summary>
        public float PlayerEndZ => -_halfLength;

        /// <summary>相手側の台の端</summary>
        public float OpponentEndZ => _halfLength;

        /// <summary>ネットは台より少し広い（実際の卓球と同じく台の外へはみ出す）</summary>
        public float NetHalfWidth => _halfWidth * 1.2f;

        /// <summary>
        /// 奥行きの割合（0:手前端 / 1:奥端）。台の外側も扱えるようクランプしない。
        /// </summary>
        public float DepthRatio(float z)
        {
            return (z + _halfLength) / (_halfLength * 2f);
        }

        /// <summary>奥ほど小さく見せるための表示倍率</summary>
        public float ScaleAt(float z)
        {
            return Mathf.LerpUnclamped(_nearScale, _farScale, DepthRatio(z));
        }

        /// <summary>その奥行きにおける台表面（高さ0）の画面上のY座標</summary>
        public float BaseYAt(float z)
        {
            return Mathf.LerpUnclamped(_nearBaseY, _farBaseY, DepthRatio(z));
        }

        /// <summary>コート座標 → 画面（ワールド2D）座標</summary>
        public Vector2 Project(Vector3 court)
        {
            float scale = ScaleAt(court.z);
            return new Vector2(court.x * scale, BaseYAt(court.z) + court.y * scale);
        }

        /// <summary>
        /// 画面（ワールド2D）座標 → コート座標。
        /// 奥行きは一意に決まらないため、対象の z を指定して逆変換する。
        /// </summary>
        public Vector3 Unproject(Vector2 world, float z)
        {
            float scale = ScaleAt(z);
            return new Vector3(world.x / scale, (world.y - BaseYAt(z)) / scale, z);
        }

        /// <summary>台の上（有効範囲内）かどうか</summary>
        public bool IsOnTable(float x, float z)
        {
            return Mathf.Abs(x) <= _halfWidth && Mathf.Abs(z) <= _halfLength;
        }
    }
}
