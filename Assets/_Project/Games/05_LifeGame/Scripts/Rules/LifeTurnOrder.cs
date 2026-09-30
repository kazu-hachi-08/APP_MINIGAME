using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 手番の計算。席順に回し、ゴール済みの人は飛ばす。
    /// オンラインでも全端末がこの計算で手番を進めるので、手番そのものは送らない。
    /// </summary>
    public static class LifeTurnOrder
    {
        public const int None = -1;

        /// <summary>current の次に回す席。全員ゴール済みなら None</summary>
        public static int NextSeat(IReadOnlyList<LifePlayerState> players, int current)
        {
            int count = players.Count;
            // i == count で current 自身も候補に入る（残りが1人ならその人が続けて回す）
            for (int i = 1; i <= count; i++)
            {
                int seat = (current + i) % count;
                if (!players[seat].HasGoaled) return seat;
            }

            return None;
        }
    }
}
