using System.Collections.Generic;

namespace MiniGame.LifeGame
{
    /// <summary>
    /// 手番の計算。席順に回し、ゴール済みの人と休みの人は飛ばす。
    /// オンラインでも全端末がこの計算で手番を進めるので、手番そのものは送らない。
    /// </summary>
    public static class LifeTurnOrder
    {
        public const int None = -1;

        /// <summary>
        /// current の次に回す席。全員ゴール済みなら None。
        /// 休みの人は休みを1減らして飛ばし、rested に席を足す（飛ばした人を演出で見せるため）
        /// </summary>
        public static int NextSeat(IReadOnlyList<LifePlayerState> players, int current, List<int> rested = null)
        {
            int count = players.Count;
            // 全員休みでも、飛ばすたびに休みが減るので何周かすれば必ず誰かの番になる
            while (true)
            {
                bool anyPlaying = false;
                // i == count で current 自身も候補に入る（残りが1人ならその人が続けて回す）
                for (int i = 1; i <= count; i++)
                {
                    int seat = (current + i) % count;
                    LifePlayerState player = players[seat];
                    if (player.HasGoaled) continue;

                    anyPlaying = true;
                    if (player.RestTurns == 0) return seat;

                    player.RestTurns--;
                    rested?.Add(seat);
                }

                if (!anyPlaying) return None;
            }
        }
    }
}
