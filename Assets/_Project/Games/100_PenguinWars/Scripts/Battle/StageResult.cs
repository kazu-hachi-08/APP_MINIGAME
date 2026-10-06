namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ステージ1回分の結果。★の判定（StarRule）とリザルト表示の両方がこれだけを見る</summary>
    public readonly struct StageResult
    {
        public bool Cleared { get; }
        public float ElapsedSeconds { get; }
        /// <summary>自分の城の残りHP（0〜1）</summary>
        public float PlayerCastleHpRatio { get; }
        public int KillCount { get; }

        public StageResult(bool cleared, float elapsedSeconds, float playerCastleHpRatio, int killCount)
        {
            Cleared = cleared;
            ElapsedSeconds = elapsedSeconds;
            PlayerCastleHpRatio = playerCastleHpRatio;
            KillCount = killCount;
        }

        /// <summary>一人用は自分が常に Left なので、敵（Right）の城が落ちていればクリア</summary>
        public static StageResult From(BattleWorld world)
        {
            CastleState castle = world.GetCastle(Side.Left);
            float hpRatio = castle.MaxHp > 0 ? (float)castle.Hp / castle.MaxHp : 0f;
            bool cleared = world.IsFinished && world.Loser == Side.Right;
            return new StageResult(cleared, world.ElapsedTime, hpRatio, world.GetKillCount(Side.Left));
        }
    }
}
