namespace MiniGame.PenguinWars.Battle
{
    /// <summary>オンライン対戦の制限時間と時間切れの判定（仕様書 §2.2）</summary>
    public partial class BattleWorld
    {
        /// <summary>対戦の残り秒数。制限時間なし（ステージ）なら 0 のまま</summary>
        public float RemainingTime { get; internal set; }
        public bool HasTimeLimit => _settings.TimeLimit > 0f;
        /// <summary>時間切れで城の残りHP割合が同じだった。true のとき Loser は意味を持たない</summary>
        public bool IsDraw { get; private set; }

        private void TickTimeLimit(float deltaTime)
        {
            if (IsFinished || !HasTimeLimit) return;

            RemainingTime = System.Math.Max(0f, RemainingTime - deltaTime);
            if (RemainingTime <= 0f) FinishByTimeUp();
        }

        /// <summary>城の残りHP「割合」で比べる（城HPを左右で変えても公平になるように）</summary>
        private void FinishByTimeUp()
        {
            // 割合を float で比べると誤差で同点を取りこぼすので、分母を払った整数で比べる
            long left = (long)_leftCastle.Hp * _rightCastle.MaxHp;
            long right = (long)_rightCastle.Hp * _leftCastle.MaxHp;

            IsFinished = true;
            IsDraw = left == right;
            Loser = left < right ? Side.Left : Side.Right;
            _events.Add(new BattleEvent(BattleEventType.TimeUp, Loser, BattleEvent.CastleId, 0f, IsDraw ? 1 : 0));
        }
    }
}
