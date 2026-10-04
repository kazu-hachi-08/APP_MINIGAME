using System;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 一定間隔で右陣営の編成からランダムに出撃させる仮の湧き。Phase 4 で敵レベル付きの湧きに置き換える
    /// </summary>
    public class SimpleEnemySpawner
    {
        private readonly float _interval;
        private readonly int _slotCount;
        private readonly Random _random;
        private float _timer;

        public SimpleEnemySpawner(float interval, int slotCount, int seed)
        {
            _interval = interval;
            _slotCount = slotCount;
            _random = new Random(seed);
            // 開始直後に湧くと、味方を出す前に押し込まれるので1間隔待つ
            _timer = interval;
        }

        public void Tick(BattleWorld world, float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer > 0f || _slotCount <= 0) return;

            _timer += _interval;
            world.Enqueue(BattleCommand.Spawn(Side.Right, _random.Next(_slotCount)));
        }
    }
}
