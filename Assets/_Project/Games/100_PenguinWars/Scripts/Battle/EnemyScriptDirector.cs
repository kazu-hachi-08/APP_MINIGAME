using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>
    /// ステージの敵の出方（StageDefinition.Entries）に従って、出すキャラを決める。実際の出撃は BattleWorld が行う。
    /// BattleWorld を持たないのは、敵城HPの割合だけ渡せばテストで単体で動かせるようにするため
    /// </summary>
    public class EnemyScriptDirector
    {
        private class Row
        {
            public EnemySpawnEntry Entry;
            public UnitStats Stats;
            public bool IsTriggered;
            public float Timer;
            public int Spawned;
        }

        private readonly List<Row> _rows = new List<Row>();

        /// <param name="statsByNo">キャラNo → 数値。見つからない No の行は出さない（定義表のテストで弾く前提）</param>
        public EnemyScriptDirector(IReadOnlyList<EnemySpawnEntry> entries, IReadOnlyDictionary<int, UnitStats> statsByNo)
        {
            foreach (EnemySpawnEntry entry in entries)
            {
                if (!statsByNo.TryGetValue(entry.UnitNo, out UnitStats stats)) continue;

                // 倍率は最初に1回だけかける（出すたびにコピーを作らないように）
                _rows.Add(new Row { Entry = entry, Stats = stats.Scaled(entry.StatMultiplier) });
            }
        }

        /// <summary>
        /// このステップで出すキャラを spawns に入れる。ボスの行が初めて出たらそのキャラNoを返す（なければ 0）
        /// </summary>
        public int Tick(float deltaTime, float enemyCastleHpRatio, List<EnemySpawn> spawns)
        {
            spawns.Clear();
            int bossNo = 0;
            foreach (Row row in _rows)
            {
                if (!TickRow(row, deltaTime, enemyCastleHpRatio)) continue;

                spawns.Add(new EnemySpawn(row.Stats, row.Entry.IsBoss));
                if (row.Entry.IsBoss && row.Spawned == 1) bossNo = row.Entry.UnitNo;
            }
            return bossNo;
        }

        /// <summary>このステップで1体出すなら true。1ステップに出すのは1行1体まで（間隔 0 の行で無限に湧かないように）</summary>
        private static bool TickRow(Row row, float deltaTime, float enemyCastleHpRatio)
        {
            if (IsDone(row)) return false;
            if (!row.IsTriggered)
            {
                // 一度始まったら戻さない（城は回復しないが、念のため一方通行にする）
                if (enemyCastleHpRatio > row.Entry.TriggerCastleHpRatio) return false;

                row.IsTriggered = true;
                row.Timer = row.Entry.StartTime;
            }

            row.Timer -= deltaTime;
            if (row.Timer > 0f) return false;

            row.Timer += row.Entry.Interval;
            row.Spawned++;
            return true;
        }

        private static bool IsDone(Row row)
        {
            return row.Entry.Count > 0 && row.Spawned >= row.Entry.Count;
        }
    }

    /// <summary>Director が決めた出撃1体分</summary>
    public readonly struct EnemySpawn
    {
        public UnitStats Stats { get; }
        /// <summary>ボスは場の上限で捨てない（山場が来なくなるため）</summary>
        public bool IsBoss { get; }

        public EnemySpawn(UnitStats stats, bool isBoss)
        {
            Stats = stats;
            IsBoss = isBoss;
        }
    }
}
