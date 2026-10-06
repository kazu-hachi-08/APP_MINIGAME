namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// キャラの役割（仕様書 §5.5）。数値の計算式・ランダム編成の制約・大型の確定出現で使う。
    /// 戦闘ロジック（DeckRandomizer など）が見るので Data ではなく Battle に置く
    /// </summary>
    public enum UnitRole
    {
        /// <summary>壁: 安い・体力が多め・攻撃が弱い</summary>
        Wall,
        /// <summary>アタッカー: 近距離で攻撃が高い</summary>
        Attacker,
        /// <summary>遠距離: 射程が長く、壁の後ろから攻撃</summary>
        Ranged,
        /// <summary>妨害: ふっとばす / 止める / 遅くする 持ち</summary>
        Disruptor,
        /// <summary>大型: 高コスト・高性能。再生産が長い</summary>
        Large,
    }
}
