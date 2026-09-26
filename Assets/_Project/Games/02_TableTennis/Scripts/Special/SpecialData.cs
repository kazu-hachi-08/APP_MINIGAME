using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 必殺技1つ分のデータ。技ごとに専用クラスを作らず「既存の打球値への倍率」と「相手の足止め」の組み合わせで表す。
    /// 倍率を1、足止めを0にすると効果なしになる。1技1アセットに分け、2人開発で数値調整がコンフリクトしにくくしている。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGame/TableTennis/Special Data", fileName = "Special_")]
    public class SpecialData : ScriptableObject
    {
        public string DisplayName = "ファイアショット";

        [Tooltip("必殺技の球だと一目で分かるよう、飛んでいる間ボールをこの色にする")]
        public Color BallColor = new Color(1f, 0.35f, 0.15f);

        [Header("打球")]
        [Tooltip("打球速度の倍率。上げすぎると弧が低くなりネットに掛かりやすい")]
        public float SpeedMultiplier = 1f;

        [Tooltip("横回転の倍率")]
        public float SideSpinMultiplier = 1f;

        [Tooltip("横回転の最低量。横フリックしなかったときにも曲がるようにする")]
        public float MinSideSpin;

        [Header("相手の足止め")]
        [Tooltip("打った瞬間から相手の動きを鈍らせる時間（秒）。0で足止めなし")]
        public float StunDuration;

        [Tooltip("足止め中の相手の移動速度の倍率")]
        [Range(0f, 1f)]
        public float StunMoveMultiplier = 1f;

        [Tooltip("NPCがこの球を返せる確率の倍率。バウンドが低い・見えないなど、NPCの判断に表れない効果の分をここで表す")]
        [Range(0f, 1f)]
        public float OpponentReturnRateMultiplier = 1f;

        [Header("球筋")]
        [Tooltip("ネットを越えてから相手コートでバウンドするまで、ボールを見えなくする（影だけ残す）")]
        public bool Invisible;

        [Tooltip("相手コートでのバウンドの跳ね上がりの倍率。小さいほど低く滑る")]
        [Range(0f, 1f)]
        public float BounceHeightMultiplier = 1f;

        [Header("打つ側の強化")]
        [Tooltip("1球だけでなく、そのラリーが終わるまで効果を続ける")]
        public bool LastsForRally;

        [Tooltip("接触範囲の倍率（NPCは届く範囲）")]
        public float ReachMultiplier = 1f;

        [Tooltip("当たれば常にジャストのタイミングとして扱う（NPCは必ず返球する）")]
        public bool PerfectTiming;

        /// <summary>横回転だけを強める。縦回転はショット種別の個性なので変えない</summary>
        public Vector2 ApplySpin(Vector2 spin)
        {
            float side = spin.x * SideSpinMultiplier;
            if (Mathf.Abs(side) < MinSideSpin)
            {
                side = Mathf.Sign(spin.x) * MinSideSpin;
            }

            return new Vector2(side, spin.y);
        }
    }
}
