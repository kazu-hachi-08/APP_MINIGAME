using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle.Tests
{
    /// <summary>
    /// 戦闘テストで共通の進め方・探し方。各テストは using static で取り込み、呼び出しを短く保つ。
    /// 進め方が違うと同じ秒数でも結果がずれるので、特別な進め方が要るテストだけ自分で持つ
    /// </summary>
    internal static class BattleTestUtil
    {
        /// <summary>BattleRunner と同じ固定ステップ</summary>
        public const float StepTime = 1f / 30f;

        /// <summary>seconds 秒に一番近いステップ数だけ進める（1/30 の割り算の誤差で1ステップ足りなくならないように丸める）</summary>
        public static void Run(BattleWorld world, float seconds)
        {
            int steps = (int)System.Math.Round(seconds / StepTime);
            for (int i = 0; i < steps; i++) world.Step(StepTime);
        }

        /// <summary>Run して、その間に出たイベントをすべて返す</summary>
        public static List<BattleEvent> RunAndDrain(BattleWorld world, float seconds)
        {
            Run(world, seconds);
            return Drain(world);
        }

        public static List<BattleEvent> Drain(BattleWorld world)
        {
            var events = new List<BattleEvent>();
            world.DrainEvents(events);
            return events;
        }

        public static int Count(List<BattleEvent> events, BattleEventType type)
        {
            return events.FindAll(e => e.Type == type).Count;
        }

        /// <summary>side のユニットのうち最初の1体（死んでいても返す）。いなければ null</summary>
        public static UnitState FindFirst(BattleWorld world, Side side)
        {
            foreach (UnitState unit in world.Units)
            {
                if (unit.Side == side) return unit;
            }
            return null;
        }
    }
}
