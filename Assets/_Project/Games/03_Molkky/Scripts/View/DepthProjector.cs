using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 地面座標（X＝左右／Z＝奥行き、＋高さH）→ 画面座標・表示サイズ・描画順への変換（§4.2）。
    /// ピン・棒・影・地面が同じ変換を共有するよう、ここ1箇所に集約している。
    /// 奥へ行くほど消失点へ縮む透視投影にして、「奥のピンへ投げる」見た目を作る。
    /// </summary>
    public class DepthProjector : MonoBehaviour
    {
        [Tooltip("投擲ライン（Z=0）で地面の1単位が画面上の何ワールド単位になるか")]
        [SerializeField] private float _nearScale = 1.5f;

        [Tooltip("視点から投擲ラインまでの距離。小さいほど奥行きが強調される")]
        [SerializeField] private float _cameraDistance = 6f;

        [Tooltip("投擲ラインの画面上のY座標")]
        [SerializeField] private float _nearBaseY = -3.5f;

        [Tooltip("消失点（地平線）の画面上のY座標")]
        [SerializeField] private float _horizonY = 6.5f;

        [Tooltip("投擲ライン上の物の描画順。奥ほど小さくなる")]
        [SerializeField] private int _nearSortingOrder = 10000;

        [Tooltip("奥行き1単位あたり描画順をいくつ下げるか")]
        [SerializeField] private float _sortingStepsPerUnit = 100f;

        // 1つの物が本体・文字・影で描画順を3つ使うため、奥行きの段ごとに間を空ける
        public const int SortingSlotsPerStep = 4;

        // 視点より手前（Z が -cameraDistance 付近）で割り算が発散しないための下限
        private const float MinDepthRatio = 0.1f;

        /// <summary>奥行きによる縮小率（投擲ラインで1、奥ほど0へ近づく）</summary>
        public float DepthFactor(float z)
        {
            float distance = Mathf.Max(_cameraDistance + z, _cameraDistance * MinDepthRatio);
            return _cameraDistance / distance;
        }

        /// <summary>その奥行きで地面の1単位が画面上の何ワールド単位になるか</summary>
        public float ScaleAt(float z)
        {
            return _nearScale * DepthFactor(z);
        }

        /// <summary>地面座標（ground.y が奥行きZ）と高さ → 画面座標</summary>
        public Vector2 Project(Vector2 ground, float height = 0f)
        {
            float factor = DepthFactor(ground.y);
            float scale = _nearScale * factor;
            float baseY = _horizonY - (_horizonY - _nearBaseY) * factor;
            return new Vector2(ground.x * scale, baseY + height * scale);
        }

        public int SortingOrderAt(float z)
        {
            return _nearSortingOrder - Mathf.RoundToInt(z * _sortingStepsPerUnit) * SortingSlotsPerStep;
        }
    }
}
