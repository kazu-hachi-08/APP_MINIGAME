using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 画面なしでステージを1回自動プレイする（SimpleBot が左陣営を操作）。
    /// 18ステージを手で遊ばずに難しさの並びを確かめるため。BattleRunner.InitializeStage と同じ順で World を作るので、ギミック込みで再現できる
    /// </summary>
    public class StageSimulator
    {
        /// <summary>BattleRunner と同じ固定ステップ（違う値だと実機と結果がずれるため）</summary>
        public const float StepTime = 1f / 30f;

        private readonly Func<BattleSettings> _createBaseSettings;
        private readonly IReadOnlyDictionary<int, UnitStats> _statsByNo;
        // イベントは使わないが、溜め続けるとメモリを食うので毎ステップここに移して捨てる
        private readonly List<BattleEvent> _discardedEvents = new List<BattleEvent>();

        /// <param name="createBaseSettings">ステージで上書きする前の設定（Balance 相当）。呼ぶたびに新しいものを返すこと</param>
        /// <param name="statsByNo">キャラの数値。味方・敵の両方に使う</param>
        public StageSimulator(Func<BattleSettings> createBaseSettings, IReadOnlyDictionary<int, UnitStats> statsByNo)
        {
            _createBaseSettings = createBaseSettings;
            _statsByNo = statsByNo;
        }

        /// <summary>BattleSettings の既定値と定義表の数値で回す（テスト用。エディタのメニューは Balance の値を渡す）</summary>
        public static StageSimulator CreateDefault()
        {
            return new StageSimulator(() => new BattleSettings(), DefinitionStats());
        }

        /// <summary>定義表から計算したキャラの数値（カタログのアセットも Rebuild でこれと同じ値になる）</summary>
        public static Dictionary<int, UnitStats> DefinitionStats()
        {
            var statsByNo = new Dictionary<int, UnitStats>();
            foreach (UnitDefinition unit in UnitDefinitions.All) statsByNo[unit.No] = UnitStatFormula.Calculate(unit);
            return statsByNo;
        }

        /// <summary>決着が付かないまま maxSeconds を過ぎたら、その時点の結果（未クリア・自城HPは残っている）を返す</summary>
        public StageResult Run(StageDefinition stage, IReadOnlyList<int> deckNos, BotSkill skill, int seed, float maxSeconds)
        {
            BattleWorld world = CreateWorld(stage, deckNos, seed);
            var bot = new SimpleBot(skill, seed);
            while (!world.IsFinished && world.ElapsedTime < maxSeconds)
            {
                bot.Tick(world, StepTime);
                world.Step(StepTime);
                world.DrainEvents(_discardedEvents);
                _discardedEvents.Clear();
            }
            return StageResult.From(world);
        }

        private BattleWorld CreateWorld(StageDefinition stage, IReadOnlyList<int> deckNos, int seed)
        {
            BattleSettings settings = _createBaseSettings();
            // ステージの敵はお金を持たず定義表どおりに湧く・時間切れなし（BattleRunner のステージと同じ）
            settings.RightSpawnsFree = true;
            settings.TimeLimit = 0f;
            settings.RandomSeed = seed;
            stage.ApplyTo(settings);

            var world = new BattleWorld(settings);
            world.SetDeck(Side.Left, ToSortedDeck(deckNos));
            world.SetEnemyScript(new EnemyScriptDirector(stage.Entries, _statsByNo));
            return world;
        }

        private List<UnitStats> ToSortedDeck(IReadOnlyList<int> deckNos)
        {
            var deck = new List<UnitStats>();
            foreach (int no in deckNos)
            {
                if (_statsByNo.TryGetValue(no, out UnitStats stats)) deck.Add(stats);
            }
            DeckRandomizer.SortByCost(deck);
            return deck;
        }
    }
}
