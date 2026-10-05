using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ステージ検証用に左陣営を操作する簡単なCPU（StageSimulator が使う）。
    /// 判断の順: 砲（範囲に敵がたまっていれば撃つ）→ 働きペンギン → 壁が足りなければ一番安い壁 → 壁以外で出せる中で一番高いキャラ。
    /// 判断の間隔だけシードで揺らす（人間の操作の揺れの代わり。揺らさないと何回回しても同じ結果になり、勝率の意味がないため）
    /// </summary>
    public class SimpleBot
    {
        private const Side BotSide = Side.Left;
        /// <summary>生きている壁がこの数より少なければ足す（前線で倒れても次の壁が向かっている状態を保つ）</summary>
        private const int WallsToKeep = 3;
        /// <summary>砲の範囲にこの数以上の敵がいたら撃つ（1〜2体に撃つのはもったいないため）</summary>
        private const int CannonMinTargets = 3;
        /// <summary>判断の間隔をこの割合だけ前後に揺らす</summary>
        private const float IntervalJitter = 0.25f;
        private const int NoSlot = -1;

        private readonly BotSkill _skill;
        private readonly Random _random;
        private float _timer;

        public SimpleBot(BotSkill skill, int seed)
        {
            _skill = skill;
            _random = new Random(seed);
            _timer = NextInterval();
        }

        /// <summary>BattleWorld.Step の前に毎ステップ呼ぶ。判断は DecisionInterval ごとだけ</summary>
        public void Tick(BattleWorld world, float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f) return;

            _timer += NextInterval();
            Decide(world);
        }

        private float NextInterval()
        {
            float jitter = 1f + IntervalJitter * (float)(_random.NextDouble() * 2.0 - 1.0);
            return _skill.DecisionInterval * jitter;
        }

        private void Decide(BattleWorld world)
        {
            if (ShouldFireCannon(world)) world.Enqueue(BattleCommand.FireCannon(BotSide));
            if (ShouldLevelUp(world))
            {
                world.Enqueue(BattleCommand.LevelUpWallet(BotSide));
                return;
            }

            int slot = NeedsWall(world) ? FindCheapestWall(world) : NoSlot;
            if (slot == NoSlot) slot = FindPriciestNonWall(world);
            if (slot != NoSlot) world.Enqueue(BattleCommand.Spawn(BotSide, slot));
        }

        private bool ShouldLevelUp(BattleWorld world)
        {
            WalletState wallet = world.GetWallet(BotSide);
            return wallet.Level < _skill.WalletTargetLevel && wallet.CanLevelUp;
        }

        private static bool ShouldFireCannon(BattleWorld world)
        {
            if (!world.GetCannon(BotSide).IsReady) return false;

            float reach = world.CannonReach(BotSide);
            float originX = world.GetCastle(BotSide).X;
            int targets = 0;
            foreach (UnitState unit in world.Units)
            {
                if (unit.Side != BotSide && !unit.IsDead && unit.X - originX <= reach) targets++;
            }
            return targets >= CannonMinTargets;
        }

        private static bool NeedsWall(BattleWorld world)
        {
            int walls = 0;
            foreach (UnitState unit in world.Units)
            {
                if (unit.Side == BotSide && !unit.IsDead && unit.Stats.Role == UnitRole.Wall) walls++;
            }
            return walls < WallsToKeep;
        }

        private static int FindCheapestWall(BattleWorld world)
        {
            IReadOnlyList<UnitStats> deck = world.GetDeck(BotSide);
            int best = NoSlot;
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Role != UnitRole.Wall || !world.CanSpawn(BotSide, i)) continue;
                if (best == NoSlot || deck[i].Cost < deck[best].Cost) best = i;
            }
            return best;
        }

        /// <summary>壁以外で今出せる一番高いキャラ（壁は NeedsWall で足す。ここで壁も選ぶと、安い壁ばかり出てさかなが残らないため）</summary>
        private static int FindPriciestNonWall(BattleWorld world)
        {
            IReadOnlyList<UnitStats> deck = world.GetDeck(BotSide);
            int best = NoSlot;
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i].Role == UnitRole.Wall || !world.CanSpawn(BotSide, i)) continue;
                if (best == NoSlot || deck[i].Cost > deck[best].Cost) best = i;
            }
            return best;
        }
    }
}
