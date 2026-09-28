using UnityEngine;

namespace MiniGame.Molkky
{
    /// <summary>
    /// 演出で使う補間カーブ。ピンの起き上がりと得点ポップアップで同じ「弾み方」にそろえるため1箇所にまとめる。
    /// </summary>
    public static class MolkkyEasing
    {
        // 一般的な EaseOutBack の行き過ぎ量（約10%）。これより大きいと弾みすぎて見える
        private const float BackOvershoot = 1.70158f;

        /// <summary>少し行き過ぎてから戻る補間。「ポン」「ピョコッ」と弾む見た目にする</summary>
        public static float OutBack(float t)
        {
            float u = t - 1f;
            return 1f + (BackOvershoot + 1f) * u * u * u + BackOvershoot * u * u;
        }

        /// <summary>最後に減速して止まる補間。勢いよく飛び出してきた感じを出す</summary>
        public static float OutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
    }
}
