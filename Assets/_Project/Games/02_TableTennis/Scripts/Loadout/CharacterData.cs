using UnityEngine;

namespace MiniGame.TableTennis
{
    /// <summary>
    /// 選手1人分のデータ。選手は「体」（動きの速さ・届く範囲・振りの猶予）を決める。
    /// 1人1アセットに分け、2人開発で数値調整がコンフリクトしにくくしている。
    /// 倍率は全て スタンダード=1 を基準にする。
    /// </summary>
    [CreateAssetMenu(menuName = "MiniGame/TableTennis/Character Data", fileName = "Character_")]
    public class CharacterData : ScriptableObject
    {
        public string DisplayName = "KAZUKI";
        public PlayStyle Style = PlayStyle.Standard;

        [Tooltip("手前（プレイヤー側）に立つときの背中向きスプライト")]
        public Sprite BackSprite;

        [Tooltip("奥（相手側）に立つときの正面向きスプライト")]
        public Sprite FrontSprite;

        [Header("能力倍率")]
        [Tooltip("ラケットが指へ追いつく速さ / NPCの移動速度")]
        public float MoveSpeedMultiplier = 1f;

        [Tooltip("当たる範囲 / NPCの届く範囲")]
        public float ReachMultiplier = 1f;

        [Tooltip("フリック後にボールを待てる時間（NPCには適用しない）")]
        public float SwingDurationMultiplier = 1f;
    }
}
