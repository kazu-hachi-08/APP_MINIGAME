using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ステージ用の BattleWorld を作る。実機（BattleRunner）と検証（StageSimulator）で作り方がずれると
    /// シミュレーション結果が実機と合わなくなるため、ここ1か所にまとめる
    /// </summary>
    public static class StageWorldBuilder
    {
        /// <param name="baseSettings">ステージで上書きする前の設定。この中身を書き換えるので、使い回さないこと</param>
        /// <param name="deckNos">左陣営の編成。statsByNo に無い No は外す</param>
        /// <param name="statsByNo">キャラの数値。味方・敵の両方に使う</param>
        public static BattleWorld Create(BattleSettings baseSettings, StageDefinition stage, IReadOnlyList<int> deckNos, IReadOnlyDictionary<int, UnitStats> statsByNo)
        {
            // ステージの敵はお金を持たず定義表どおりに湧く・時間切れなし
            baseSettings.RightSpawnsFree = true;
            baseSettings.TimeLimit = 0f;
            stage.ApplyTo(baseSettings);

            var world = new BattleWorld(baseSettings);
            world.SetDeck(Side.Left, ToSortedDeck(deckNos, statsByNo));
            world.SetEnemyScript(new EnemyScriptDirector(stage.Entries, statsByNo));
            return world;
        }

        private static List<UnitStats> ToSortedDeck(IReadOnlyList<int> deckNos, IReadOnlyDictionary<int, UnitStats> statsByNo)
        {
            var deck = new List<UnitStats>();
            foreach (int no in deckNos)
            {
                if (statsByNo.TryGetValue(no, out UnitStats stats)) deck.Add(stats);
            }
            DeckRandomizer.SortByCost(deck);
            return deck;
        }
    }
}
