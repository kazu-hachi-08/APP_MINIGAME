using System;
using System.Collections.Generic;

namespace MiniGame.PenguinWars.Battle
{
    /// <summary>ステージ1つ分の定義（StageDefinitions の1要素）。セーブは番号ではなく Id で記録する（並び替えで壊れないように）</summary>
    public class StageDefinition
    {
        /// <summary>各章の最後（この番号）がボスステージ</summary>
        public const int StagesPerChapter = 6;
        private const float DefaultSafeHpRatio = 0.5f;

        /// <summary>"1-3" のような「章-番号」</summary>
        public string Id { get; set; }
        public int Chapter { get; set; }
        /// <summary>章の中で何番目か（1 始まり）</summary>
        public int Index { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        /// <summary>地面の絵は 30 ＋余白までしか敷いていないので、それより長くしない</summary>
        public float FieldLength { get; set; } = 30f;
        public int PlayerCastleHp { get; set; } = 3000;
        public int EnemyCastleHp { get; set; } = 3000;
        public IReadOnlyList<EnemySpawnEntry> Entries { get; set; } = Array.Empty<EnemySpawnEntry>();
        /// <summary>★3 の目標タイム（秒）</summary>
        public float TargetSeconds { get; set; }
        /// <summary>★2 に必要な自城HPの割合。ステージごとに変えたくなったときのために定義に持たせる</summary>
        public float SafeHpRatio { get; set; } = DefaultSafeHpRatio;
        /// <summary>初めてクリアしたときに仲間になるキャラの No</summary>
        public IReadOnlyList<int> UnlockNos { get; set; } = Array.Empty<int>();

        public bool IsBossStage => Index == StagesPerChapter;
    }
}
