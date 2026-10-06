namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// 定義表で使うキャラ No の名前と、敵の出方を1行で書くヘルパー。
    /// 名前は UnitDefinitions と同じ（No だけだと定義表を読んでも何が出てくるか分からないため）
    /// </summary>
    public static partial class StageDefinitions
    {
        // 壁
        private const int Penguin = 1;
        private const int WallPenguin = 2;
        private const int SnowballPenguin = 3;
        private const int HelmetPenguin = 4;
        private const int IcePenguin = 5;
        private const int ChickPenguin = 6;
        private const int CardboardPenguin = 7;
        private const int RoundPenguin = 8;
        private const int FutonPenguin = 9;
        private const int IglooPenguin = 10;
        // アタッカー
        private const int AxePenguin = 11;
        private const int FishSwordPenguin = 12;
        private const int BoxerPenguin = 13;
        private const int SumoPenguin = 14;
        private const int LongLegPenguin = 15;
        private const int HammerPenguin = 16;
        private const int NinjaPenguin = 17;
        private const int SamuraiPenguin = 18;
        private const int BikePenguin = 19;
        private const int DrillPenguin = 20;
        private const int HeroPenguin = 21;
        private const int MusclePenguin = 22;
        // 遠距離
        private const int BowPenguin = 23;
        private const int SnowThrowPenguin = 24;
        private const int FishingPenguin = 25;
        private const int TallPenguin = 26;
        private const int CannonPenguin = 27;
        private const int WizardPenguin = 28;
        private const int SniperPenguin = 29;
        private const int BoomerangPenguin = 30;
        private const int RocketPenguin = 31;
        private const int TowerPenguin = 32;
        // 妨害
        private const int BlizzardPenguin = 33;
        private const int FreezePenguin = 34;
        private const int FanPenguin = 35;
        private const int SleepPenguin = 36;
        private const int StickyPenguin = 37;
        private const int HarisenPenguin = 38;
        private const int IdolPenguin = 39;
        private const int OctopusPenguin = 40;
        private const int BalloonPenguin = 41;
        private const int IceQueenPenguin = 42;
        // 大型
        private const int GiantPenguin = 43;
        private const int RobotPenguin = 44;
        private const int TankPenguin = 45;
        private const int IcebergPenguin = 46;
        private const int WhaleRiderPenguin = 47;
        private const int KingPenguin = 48;
        private const int AuroraPenguin = 49;
        private const int GodPenguin = 50;

        /// <summary>最初から出続ける（start 秒後に1体目、その後 interval 秒ごと）</summary>
        private static EnemySpawnEntry Stream(int no, float start, float interval, float mult = 1f) =>
            new EnemySpawnEntry { UnitNo = no, StartTime = start, Interval = interval, StatMultiplier = mult };

        /// <summary>敵城HPが castleRatio 以下になってから出る（城を叩くと敵が増える）。count が 0 なら出し続ける</summary>
        private static EnemySpawnEntry OnCastle(int no, float castleRatio, float interval, float mult = 1f, int count = 0) =>
            new EnemySpawnEntry { UnitNo = no, TriggerCastleHpRatio = castleRatio, Interval = interval, StatMultiplier = mult, Count = count };

        /// <summary>敵城HPが castleRatio 以下になったら1体だけ出るボス</summary>
        private static EnemySpawnEntry Boss(int no, float castleRatio, float mult = 1f) =>
            new EnemySpawnEntry { UnitNo = no, Count = 1, TriggerCastleHpRatio = castleRatio, IsBoss = true, StatMultiplier = mult };
    }
}
