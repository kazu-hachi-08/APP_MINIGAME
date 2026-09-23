using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// ラケット1本分のデータ。ラケットは「打球」（速さ・回転・ミスのしにくさ）を決める。
    /// 倍率は全て スタンダード=1 を基準にする。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGame/TableTennis/Racket Data", fileName = "Racket_")]
    public class RacketData : ScriptableObject
    {
        public PlayStyle Style = PlayStyle.Standard;

        [Tooltip("ラバーの色で見分けられるよう、タイプごとに色違いのスプライトを使う")]
        public Sprite Sprite;

        [Header("能力倍率")]
        [Tooltip("打球の前方速度。速いほど弧が低くなる")]
        public float SpeedMultiplier = 1f;

        public float SpinMultiplier = 1f;

        [Tooltip("タイミングが悪いときに狙い点がズレる量（NPCには適用しない）")]
        public float ErrorMultiplier = 1f;
    }
}
